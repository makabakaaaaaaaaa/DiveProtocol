using System;
using DiveProtocol.Gameplay;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DiveProtocol
{
    /// <summary>
    /// Minimal whitebox hitscan weapon that damages IDamageable targets through a raycast.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHitscanWeapon : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        [Header("Input")]
        [Tooltip("Input System action used to fire this weapon.")]
        [SerializeField]
        private InputActionReference fireAction;
#endif

        [Header("Fire")]
        [Tooltip("Origin used for the hitscan ray. Defaults to this transform.")]
        [SerializeField]
        private Transform fireOrigin;

        [SerializeField, Min(0.01f)]
        private float damage = 20f;

        [SerializeField, Min(0.1f)]
        private float range = 20f;

        [SerializeField, Min(0.01f)]
        private float fireIntervalSeconds = 0.3f;

        [SerializeField]
        private LayerMask hitMask = ~0;

        [Header("Hit Detection")]
        [Tooltip("When enabled, uses a SphereCast to make whitebox aiming less pixel-perfect.")]
        [SerializeField]
        private bool useSphereCast = true;

        [Tooltip("Radius used by SphereCast hit detection.")]
        [SerializeField, Min(0f)]
        private float hitRadius = 0.3f;

        [Tooltip("Controls whether trigger colliders can be hit by this weapon.")]
        [SerializeField]
        private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Ammo")]
        [SerializeField, Min(0)]
        private int maxAmmo = 20;

        [FormerlySerializedAs("startingAmmo")]
        [SerializeField, Min(0)]
        private int currentAmmo = 20;

        [SerializeField]
        private bool infiniteAmmo;

        [SerializeField]
        private bool consumeAmmoOnFire = true;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onFired;

        [SerializeField]
        private UnityEvent onDryFire;

        [SerializeField]
        private UnityEvent onAmmoChanged;

        private float _nextAllowedFireTime;
        private bool _isFireEnabled = true;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

#if ENABLE_INPUT_SYSTEM
        private bool _isSubscribedToFire;
        private bool _enabledFireAction;
#endif

        /// <summary>
        /// Raised after current or maximum ammo changes.
        /// </summary>
        public event Action<int, int> AmmoChanged;

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public bool HasAmmo => infiniteAmmo || !consumeAmmoOnFire || currentAmmo > 0;
        public bool CanFire =>
            _isFireEnabled &&
            enabled &&
            isActiveAndEnabled &&
            !GameplayInputLock.IsLocked &&
            Time.time >= _nextAllowedFireTime &&
            HasAmmo;

        private void Awake()
        {
            maxAmmo = Mathf.Max(0, maxAmmo);
            currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);

            if (fireOrigin == null)
            {
                fireOrigin = transform;
            }
        }

        private void OnEnable()
        {
            SubscribeFireInput();
        }

        private void OnDisable()
        {
            UnsubscribeFireInput();
        }

        /// <summary>
        /// Attempts to fire once, consuming one ammo on valid shots.
        /// </summary>
        public bool TryFire()
        {
            if (!CanAttemptFire())
            {
                return false;
            }

            if (!HasAmmo)
            {
                _nextAllowedFireTime = Time.time + fireIntervalSeconds;
                onDryFire?.Invoke();
                return false;
            }

            if (consumeAmmoOnFire && !infiniteAmmo)
            {
                ConsumeAmmoForShot();
            }

            _nextAllowedFireTime = Time.time + fireIntervalSeconds;
            FireRaycast();
            onFired?.Invoke();
            return true;
        }

        /// <summary>
        /// Returns whether this weapon can attempt an input-driven shot before ammo is checked.
        /// </summary>
        private bool CanAttemptFire()
        {
            return _isFireEnabled &&
                   enabled &&
                   isActiveAndEnabled &&
                   !GameplayInputLock.IsLocked &&
                   Time.time >= _nextAllowedFireTime;
        }

        /// <summary>
        /// Adds ammo without exceeding MaxAmmo.
        /// </summary>
        public bool TryAddAmmo(int amount)
        {
            return AddAmmo(amount) > 0;
        }

        /// <summary>
        /// Adds ammo without exceeding MaxAmmo.
        /// </summary>
        public int AddAmmo(int amount)
        {
            if (amount <= 0 || currentAmmo >= maxAmmo)
            {
                return 0;
            }

            int previousAmmo = currentAmmo;
            currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
            int added = currentAmmo - previousAmmo;

            if (added > 0)
            {
                NotifyAmmoChanged();
            }

            return added;
        }

        /// <summary>
        /// Sets current ammo directly, clamped to the weapon capacity.
        /// </summary>
        public void SetAmmo(int amount)
        {
            int clampedAmount = Mathf.Clamp(amount, 0, maxAmmo);
            if (currentAmmo == clampedAmount)
            {
                return;
            }

            currentAmmo = clampedAmount;
            NotifyAmmoChanged();
        }

        /// <summary>
        /// Consumes ammo if enough is available.
        /// </summary>
        public bool TryConsumeAmmo(int amount)
        {
            if (amount <= 0 || currentAmmo < amount)
            {
                return false;
            }

            currentAmmo -= amount;
            NotifyAmmoChanged();
            return true;
        }

        /// <summary>
        /// Enables or disables firing without disabling the component.
        /// </summary>
        public void SetFireEnabled(bool enabled)
        {
            _isFireEnabled = enabled;
        }

        private void ConsumeAmmoForShot()
        {
            if (currentAmmo <= 0)
            {
                return;
            }

            currentAmmo--;
            NotifyAmmoChanged();
        }

        private void NotifyAmmoChanged()
        {
            AmmoChanged?.Invoke(currentAmmo, maxAmmo);
            onAmmoChanged?.Invoke();
        }

        private void FireRaycast()
        {
            Transform origin = fireOrigin != null ? fireOrigin : transform;

            Vector3 shotDirection = origin.forward.sqrMagnitude > 0f
                ? origin.forward.normalized
                : transform.forward.normalized;

            if (shotDirection.sqrMagnitude <= 0.0001f ||
                !TryGetFirstBlockingHit(origin.position, shotDirection, out RaycastHit hit))
            {
                return;
            }

            IDamageable damageable = ResolveDamageable(hit.collider);
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            if (damageable is Component damageableComponent &&
                damageableComponent.transform.root == transform.root)
            {
                return;
            }

            damageable.TakeDamage(new DamageInfo(
                damage,
                gameObject,
                hit.point,
                shotDirection));
        }

        private bool TryGetFirstBlockingHit(
            Vector3 origin,
            Vector3 direction,
            out RaycastHit hit)
        {
            int hitCount = useSphereCast && hitRadius > 0f
                ? Physics.SphereCastNonAlloc(
                    origin,
                    hitRadius,
                    direction,
                    _hitBuffer,
                    range,
                    hitMask,
                    triggerInteraction)
                : Physics.RaycastNonAlloc(
                    origin,
                    direction,
                    _hitBuffer,
                    range,
                    hitMask,
                    triggerInteraction);

            hit = default;

            float closestDistance = float.PositiveInfinity;
            bool foundHit = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = _hitBuffer[i];
                if (candidate.collider == null ||
                    candidate.transform.root == transform.root ||
                    candidate.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = candidate.distance;
                hit = candidate;
                foundHit = true;
            }

            return foundHit;
        }

        private static IDamageable ResolveDamageable(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return null;
            }

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                return damageable;
            }

            HealthComponent healthComponent = hitCollider.GetComponentInParent<HealthComponent>();
            return healthComponent;
        }

        private void SubscribeFireInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (_isSubscribedToFire || fireAction == null || fireAction.action == null)
            {
                return;
            }

            fireAction.action.performed += HandleFirePerformed;
            _isSubscribedToFire = true;

            if (!fireAction.action.enabled)
            {
                fireAction.action.Enable();
                _enabledFireAction = true;
            }
#endif
        }

        private void UnsubscribeFireInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (!_isSubscribedToFire || fireAction == null || fireAction.action == null)
            {
                return;
            }

            fireAction.action.performed -= HandleFirePerformed;
            _isSubscribedToFire = false;

            if (_enabledFireAction)
            {
                fireAction.action.Disable();
                _enabledFireAction = false;
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void HandleFirePerformed(InputAction.CallbackContext context)
        {
            TryFire();
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            damage = Mathf.Max(0.01f, damage);
            range = Mathf.Max(0.1f, range);
            fireIntervalSeconds = Mathf.Max(0.01f, fireIntervalSeconds);
            hitRadius = Mathf.Max(0f, hitRadius);
            maxAmmo = Mathf.Max(0, maxAmmo);
            currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
        }
#endif
    }
}
