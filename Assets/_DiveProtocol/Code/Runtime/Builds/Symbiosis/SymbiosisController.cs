using System;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Runtime implementation for Humus Symbiosis stacks, aura damage, and abnormal zones.
    /// </summary>
    public sealed class SymbiosisController : MonoBehaviour
    {
        private const int MaxStacks = 5;
        private const float SourceDetectionRadius = 2.5f;
        private const float AuraRadius = 2.2f;
        private const float StackGainIntervalSeconds = 1f;
        private const float StackDecayIntervalSeconds = 4f;
        private const float AuraTickIntervalSeconds = 1f;
        private const float PollutionCoatChance = 0.30f;
        private const float CadaverConfusionSeconds = 4f;

        [SerializeField] private LayerMask sourceMask = ~0;
        [SerializeField] private LayerMask auraTargetMask = ~0;

        private readonly Collider[] _sourceHits = new Collider[24];
        private PlayerBuildController _buildController;
        private HealthComponent _health;
        private SymbiosisAuraDamage _auraDamage;
        private int _sympathyStacks;
        private float _nextStackGainTime;
        private float _nextStackDecayTime;
        private int _abnormalZoneDepth;
        private float _abnormalHealAccumulator;
        private float _activeAbnormalHealPerSecond = 1f;
        private float _activeAbnormalWeaponSpreadMultiplier = 1.25f;

        public event Action<int> SympathyStacksChanged;
        public event Action<GameObject> PollutionCoatTriggered;
        public event Action<GameObject> CorpseReanimated;

        public int SympathyStacks => _sympathyStacks;
        public bool IsInAbnormalZone => _abnormalZoneDepth > 0;

        private bool HasCore =>
            _buildController != null &&
            _buildController.HasUpgrade(BuildUpgradeId.Humus_Sympathy);

        private void Awake()
        {
            _buildController = GetComponent<PlayerBuildController>();
            _health = GetComponent<HealthComponent>();
            _auraDamage = new SymbiosisAuraDamage(transform, auraTargetMask, AuraRadius);
        }

        private void Update()
        {
            if (!HasCore)
            {
                return;
            }

            bool nearSource = IsNearSymbiosisSource();
            if (nearSource)
            {
                TryGainStack();
            }
            else
            {
                TryDecayStack();
            }

            _auraDamage.Tick(_sympathyStacks, AuraTickIntervalSeconds);
            UpdateAbnormalHealing();
        }

        private void OnTriggerEnter(Collider other)
        {
            AbnormalZone zone = other != null ? other.GetComponentInParent<AbnormalZone>() : null;
            if (zone != null && zone.CountsAsAbnormalEnvironment)
            {
                _abnormalZoneDepth++;
                _activeAbnormalHealPerSecond = Mathf.Max(_activeAbnormalHealPerSecond, zone.HealPerSecond);
                _activeAbnormalWeaponSpreadMultiplier = Mathf.Max(
                    _activeAbnormalWeaponSpreadMultiplier,
                    zone.WeaponSpreadMultiplier);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            AbnormalZone zone = other != null ? other.GetComponentInParent<AbnormalZone>() : null;
            if (zone != null && zone.CountsAsAbnormalEnvironment)
            {
                _abnormalZoneDepth = Mathf.Max(0, _abnormalZoneDepth - 1);
                if (_abnormalZoneDepth == 0)
                {
                    _activeAbnormalHealPerSecond = 1f;
                    _activeAbnormalWeaponSpreadMultiplier = 1.25f;
                }
            }
        }

        public float GetEnvironmentalDamageMultiplier()
        {
            return Mathf.Clamp01(1f - _sympathyStacks * 0.05f);
        }

        public float GetWeaponSpreadMultiplier()
        {
            return _buildController != null &&
                   _buildController.HasUpgrade(BuildUpgradeId.Humus_AbnormalMetabolism) &&
                   IsInAbnormalZone
                ? _activeAbnormalWeaponSpreadMultiplier
                : 1f;
        }

        public bool TryTriggerPollutionCoat(GameObject attacker)
        {
            if (_buildController == null ||
                !_buildController.HasUpgrade(BuildUpgradeId.Humus_PollutionCoat) ||
                _sympathyStacks < 3 ||
                UnityEngine.Random.value > PollutionCoatChance)
            {
                return false;
            }

            PollutionCoatTriggered?.Invoke(attacker);
            return true;
        }

        public void OnCorpseReanimated(GameObject corpse)
        {
            CorpseReanimated?.Invoke(corpse);

            if (_buildController != null &&
                _buildController.HasUpgrade(BuildUpgradeId.Humus_CadaverDelay))
            {
                TemporaryConfusion.Apply(corpse, CadaverConfusionSeconds);
            }
        }

        private bool IsNearSymbiosisSource()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                SourceDetectionRadius,
                _sourceHits,
                sourceMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                SymbiosisSource source = _sourceHits[i] != null
                    ? _sourceHits[i].GetComponentInParent<SymbiosisSource>()
                    : null;

                if (source == null || !source.GrantsStacks)
                {
                    continue;
                }

                float radius = Mathf.Max(0.1f, source.Radius);
                if ((source.transform.position - transform.position).sqrMagnitude <= radius * radius)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryGainStack()
        {
            _nextStackDecayTime = Time.time + StackDecayIntervalSeconds;
            if (Time.time < _nextStackGainTime || _sympathyStacks >= MaxStacks)
            {
                return;
            }

            _nextStackGainTime = Time.time + StackGainIntervalSeconds;
            SetStacks(_sympathyStacks + 1);
        }

        private void TryDecayStack()
        {
            if (_sympathyStacks <= 0)
            {
                return;
            }

            if (_nextStackDecayTime <= 0f)
            {
                _nextStackDecayTime = Time.time + StackDecayIntervalSeconds;
                return;
            }

            if (Time.time >= _nextStackDecayTime)
            {
                _nextStackDecayTime = Time.time + StackDecayIntervalSeconds;
                SetStacks(_sympathyStacks - 1);
            }
        }

        private void SetStacks(int value)
        {
            int clamped = Mathf.Clamp(value, 0, MaxStacks);
            if (_sympathyStacks == clamped)
            {
                return;
            }

            _sympathyStacks = clamped;
            SympathyStacksChanged?.Invoke(_sympathyStacks);
        }

        private void UpdateAbnormalHealing()
        {
            if (_health == null ||
                _buildController == null ||
                !_buildController.HasUpgrade(BuildUpgradeId.Humus_AbnormalMetabolism) ||
                !IsInAbnormalZone)
            {
                _abnormalHealAccumulator = 0f;
                return;
            }

            _abnormalHealAccumulator += Time.deltaTime;
            if (_abnormalHealAccumulator < 1f)
            {
                return;
            }

            _abnormalHealAccumulator -= 1f;
            _health.Heal(_activeAbnormalHealPerSecond);
        }
    }
}
