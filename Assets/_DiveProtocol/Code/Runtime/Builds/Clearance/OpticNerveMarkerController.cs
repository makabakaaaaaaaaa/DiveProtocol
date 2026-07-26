using System;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Handles Optic Nerve target marking and hit-triggered secondary effects.
    /// </summary>
    public sealed class OpticNerveMarkerController : MonoBehaviour
    {
        private const float LockSeconds = 0.8f;
        private const float MarkDurationSeconds = 5f;
        private const float MarkedDamageMultiplier = 1.15f;
        private const float MarkAmmoRefundChance = 0.20f;
        private const float SlowMultiplier = 0.65f;
        private const float SlowDurationSeconds = 3f;
        private const float VisionBoostSeconds = 2f;

        [SerializeField] private LayerMask markMask = ~0;
        [SerializeField, Min(0.1f)] private float markRange = 20f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private PlayerBuildController _buildController;
        private PlayerHitscanWeapon _weapon;
        private HealthComponent _candidateTarget;
        private float _candidateStartTime;
        private MarkedTarget _currentMarkedTarget;
        private readonly JointRuptureTracker _jointRuptureTracker = new();

        public event Action<float> VisionBoostTriggered;

        public MarkedTarget CurrentMarkedTarget => _currentMarkedTarget;

        private bool HasCore =>
            _buildController != null &&
            _buildController.HasUpgrade(BuildUpgradeId.OpticNerve_Calibration);

        private void Awake()
        {
            _buildController = GetComponent<PlayerBuildController>();
            _weapon = GetComponent<PlayerHitscanWeapon>();
        }

        private void OnEnable()
        {
            if (_weapon != null)
            {
                _weapon.HitConfirmed += HandleWeaponHit;
            }
        }

        private void OnDisable()
        {
            if (_weapon != null)
            {
                _weapon.HitConfirmed -= HandleWeaponHit;
            }
        }

        private void Update()
        {
            if (!HasCore)
            {
                return;
            }

            UpdateMarkCandidate();
        }

        public bool IsMarked(IDamageable target)
        {
            if (target is not Component component)
            {
                return false;
            }

            MarkedTarget marker = component.GetComponent<MarkedTarget>();
            return marker != null && marker.IsMarkedBy(gameObject);
        }

        public float GetMarkedDamageMultiplier(IDamageable target)
        {
            return IsMarked(target) ? MarkedDamageMultiplier : 1f;
        }

        private void UpdateMarkCandidate()
        {
            if (!TryFindAimedHealth(out HealthComponent aimedTarget))
            {
                _candidateTarget = null;
                _candidateStartTime = 0f;
                return;
            }

            if (_candidateTarget != aimedTarget)
            {
                _candidateTarget = aimedTarget;
                _candidateStartTime = Time.time;
                return;
            }

            if (Time.time - _candidateStartTime >= LockSeconds)
            {
                ApplyMark(aimedTarget);
            }
        }

        private bool TryFindAimedHealth(out HealthComponent target)
        {
            target = null;
            Ray ray;
            if (_weapon != null && _weapon.TryGetAimRay(out Ray weaponRay))
            {
                ray = weaponRay;
            }
            else if (Camera.main != null)
            {
                ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            }
            else
            {
                return false;
            }

            if (!Physics.Raycast(ray, out RaycastHit hit, markRange, markMask, triggerInteraction))
            {
                return false;
            }

            target = hit.collider.GetComponentInParent<HealthComponent>();
            return target != null && target.IsAlive && target.transform.root != transform.root;
        }

        private void ApplyMark(HealthComponent target)
        {
            if (_currentMarkedTarget != null && _currentMarkedTarget.gameObject != target.gameObject)
            {
                _currentMarkedTarget.Clear();
            }

            MarkedTarget marker = target.GetComponent<MarkedTarget>();
            if (marker == null)
            {
                marker = target.gameObject.AddComponent<MarkedTarget>();
            }

            marker.Mark(gameObject, MarkDurationSeconds);
            _currentMarkedTarget = marker;
        }

        private void HandleWeaponHit(WeaponHitInfo hitInfo)
        {
            if (_buildController == null || hitInfo.Target == null)
            {
                return;
            }

            if (HasCore && IsMarked(hitInfo.Target) && UnityEngine.Random.value <= MarkAmmoRefundChance)
            {
                _weapon.TryAddAmmo(1);
            }

            if (_buildController.HasUpgrade(BuildUpgradeId.OpticNerve_JointRupture) &&
                _jointRuptureTracker.RegisterHit(hitInfo.Target) &&
                hitInfo.Target is Component targetComponent)
            {
                TemporaryEnemySlow.Apply(targetComponent.gameObject, SlowMultiplier, SlowDurationSeconds);
            }

            if (_buildController.HasUpgrade(BuildUpgradeId.OpticNerve_MarkRecycle) &&
                hitInfo.TargetDied &&
                IsMarked(hitInfo.Target))
            {
                _weapon.TryAddAmmo(1);
                VisionBoostTriggered?.Invoke(VisionBoostSeconds);
            }
        }
    }
}
