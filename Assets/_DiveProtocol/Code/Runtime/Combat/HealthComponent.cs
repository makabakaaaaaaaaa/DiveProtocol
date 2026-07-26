using System;
using DiveProtocol.Builds;
using UnityEngine;
using UnityEngine.Events;

namespace DiveProtocol
{
    /// <summary>
    /// Generic runtime health component for players, regular enemies, and future bosses.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        private const float MinimumNonLethalHealth = 0.001f;

        [Header("Health")]
        [Tooltip("Maximum health for this object.")]
        [SerializeField, Min(1f)]
        private float maxHealth = 100f;

        [Tooltip("Temporary invulnerability duration applied by SetInvulnerableFor.")]
        [SerializeField, Min(0f)]
        private float invulnerabilitySeconds;

        [Tooltip("When enabled, health starts at Max Health in Awake.")]
        [SerializeField]
        private bool startAtFullHealth = true;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onDamaged;

        [SerializeField]
        private UnityEvent onHealed;

        [SerializeField]
        private UnityEvent onDied;

        private float _currentHealth;
        private float _invulnerableUntilTime;
        private bool _hasDied;

        /// <summary>
        /// Raised after non-lethal or lethal damage is applied.
        /// </summary>
        public event Action<HealthComponent, DamageInfo> Damaged;

        /// <summary>
        /// Raised after health is restored.
        /// </summary>
        public event Action<HealthComponent, float> Healed;

        /// <summary>
        /// Raised once when health reaches zero.
        /// </summary>
        public event Action<HealthComponent> Died;

        /// <summary>
        /// Raised whenever current or maximum health values change.
        /// </summary>
        public event Action<HealthComponent, float, float> HealthChanged;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => _currentHealth;
        public float NormalizedHealth => maxHealth > 0f ? _currentHealth / maxHealth : 0f;
        public bool IsAlive => !_hasDied && _currentHealth > 0f;
        public bool IsInvulnerable => Time.time < _invulnerableUntilTime;

        private void Awake()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            _currentHealth = startAtFullHealth
                ? maxHealth
                : Mathf.Clamp(_currentHealth, 0f, maxHealth);
            _hasDied = _currentHealth <= 0f;
        }

        /// <summary>
        /// Applies damage if this component is alive and not currently invulnerable.
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || IsInvulnerable || damageInfo.Amount <= 0f)
            {
                return;
            }

            ApplyDamage(damageInfo);
        }

        /// <summary>
        /// Restores health without exceeding MaxHealth.
        /// </summary>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return 0f;
            }

            PlayerBuildController buildController = GetComponent<PlayerBuildController>();
            if (buildController != null)
            {
                amount *= Mathf.Max(0f, buildController.Modifiers.GetHealingMultiplier());
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, maxHealth);
            float applied = _currentHealth - previousHealth;

            if (applied <= 0f)
            {
                return 0f;
            }

            Healed?.Invoke(this, applied);
            onHealed?.Invoke();
            HealthChanged?.Invoke(this, _currentHealth, maxHealth);
            return applied;
        }

        /// <summary>
        /// Restores this component to full health if it is alive.
        /// </summary>
        public void RestoreToFull()
        {
            Heal(maxHealth);
        }

        /// <summary>
        /// Immediately kills this component through the normal damage and event flow.
        /// </summary>
        public void Kill(GameObject source = null)
        {
            if (!IsAlive)
            {
                return;
            }

            ApplyDamage(new DamageInfo(_currentHealth, source, transform.position, Vector3.zero));
        }

        /// <summary>
        /// Makes this component ignore normal damage for a scaled-time duration.
        /// </summary>
        public void SetInvulnerableFor(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            float requestedEndTime = Time.time + seconds;
            if (requestedEndTime > _invulnerableUntilTime)
            {
                _invulnerableUntilTime = requestedEndTime;
            }
        }

        /// <summary>
        /// Returns whether health can be spent as a resource.
        /// </summary>
        public bool CanSpendHealth(float amount, bool allowLethal = false)
        {
            if (!IsAlive || amount <= 0f)
            {
                return false;
            }

            float minimumHealth = allowLethal ? 0f : MinimumNonLethalHealth;
            return _currentHealth - amount >= minimumHealth;
        }

        /// <summary>
        /// Spends health through the shared damage flow.
        /// </summary>
        public bool TrySpendHealth(
            float amount,
            GameObject source = null,
            bool allowLethal = false)
        {
            if (!CanSpendHealth(amount, allowLethal))
            {
                return false;
            }

            ApplyDamage(new DamageInfo(amount, source, transform.position, Vector3.zero, DamageType.BloodCost));
            return true;
        }

        /// <summary>
        /// Spends health while preserving a requested minimum remaining value.
        /// </summary>
        public bool TrySpendHealth(float amount, float minimumRemaining, GameObject source = null)
        {
            if (!IsAlive || amount <= 0f || _currentHealth - amount < minimumRemaining)
            {
                return false;
            }

            ApplyDamage(new DamageInfo(amount, source, transform.position, Vector3.zero, DamageType.BloodCost));
            return true;
        }

        /// <summary>
        /// Adjusts maximum health and clamps current health to the new maximum.
        /// </summary>
        public float ModifyMaxHealth(float delta, float minimumMaxHealth = 1f)
        {
            float previousMaxHealth = maxHealth;
            maxHealth = Mathf.Max(minimumMaxHealth, maxHealth + delta);
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);

            if (!Mathf.Approximately(previousMaxHealth, maxHealth))
            {
                HealthChanged?.Invoke(this, _currentHealth, maxHealth);
            }

            return maxHealth;
        }

        private void ApplyDamage(DamageInfo damageInfo)
        {
            PlayerBuildController buildController = GetComponent<PlayerBuildController>();
            if (buildController != null)
            {
                float multiplier = Mathf.Max(0f, buildController.Modifiers.GetIncomingDamageMultiplier(damageInfo));
                if (!Mathf.Approximately(multiplier, 1f))
                {
                    damageInfo = new DamageInfo(
                        damageInfo.Amount * multiplier,
                        damageInfo.Source,
                        damageInfo.HitPoint,
                        damageInfo.HitDirection,
                        damageInfo.DamageType);
                }
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth - damageInfo.Amount, 0f, maxHealth);

            if (Mathf.Approximately(previousHealth, _currentHealth))
            {
                return;
            }

            Damaged?.Invoke(this, damageInfo);
            onDamaged?.Invoke();
            HealthChanged?.Invoke(this, _currentHealth, maxHealth);

            if (_currentHealth <= 0f && !_hasDied)
            {
                _hasDied = true;
                Died?.Invoke(this);
                onDied?.Invoke();
            }

            if (invulnerabilitySeconds > 0f && IsAlive)
            {
                SetInvulnerableFor(invulnerabilitySeconds);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            invulnerabilitySeconds = Mathf.Max(0f, invulnerabilitySeconds);
        }
#endif
    }
}
