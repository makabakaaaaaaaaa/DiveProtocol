using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiveProtocol.Builds
{
    /// <summary>Implements Symbiosis as scene-depth regeneration without corpse or abnormal-zone dependencies.</summary>
    public sealed class SymbiosisController : MonoBehaviour
    {
        private const float RegenerationTickSeconds = 1f;
        private const float LivingAdaptationDamageMultiplier = 0.85f;
        private const float EnvironmentalToleranceMultiplier = 0.70f;
        private const float ParasiticResponseChance = 0.25f;
        private const float ParasiticResponseDamage = 10f;
        private const float ParasiticResponseRadius = 2.5f;
        private const float ExpandedVesselBonusHealth = 20f;

        private PlayerBuildController _buildController;
        private HealthComponent _health;
        private float _nextRegenerationTime;
        private bool _expandedVesselApplied;

        public event Action<GameObject> ParasiticResponseTriggered;

        private bool HasCore => _buildController != null && _buildController.HasUpgrade(BuildUpgradeId.Humus_Sympathy);

        /// <summary>Current passive regeneration rate after this scene's depth and owned upgrades are applied.</summary>
        public float RegenerationPerSecond => GetRegenerationPerSecond(SceneManager.GetActiveScene().name);

        private void Awake()
        {
            _buildController = GetComponent<PlayerBuildController>();
            _health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (_buildController != null)
            {
                _buildController.State.UpgradeGranted += HandleUpgradeGranted;
                ApplyPermanentUpgrades();
            }
        }

        private void OnDisable()
        {
            if (_buildController != null)
            {
                _buildController.State.UpgradeGranted -= HandleUpgradeGranted;
            }
        }

        private void Update()
        {
            if (!HasCore || _health == null)
            {
                return;
            }

            if (Time.time < _nextRegenerationTime)
            {
                return;
            }

            _nextRegenerationTime = Time.time + RegenerationTickSeconds;
            _health.Heal(RegenerationPerSecond);
        }

        /// <summary>Returns the incoming-damage multiplier supplied by the selected Symbiosis components.</summary>
        public float GetIncomingDamageMultiplier(DamageInfo damageInfo)
        {
            if (_buildController == null)
            {
                return 1f;
            }

            float multiplier = 1f;
            if (_buildController.HasUpgrade(BuildUpgradeId.Humus_PollutionCoat))
            {
                multiplier *= LivingAdaptationDamageMultiplier;
            }

            if (damageInfo.DamageType == DamageType.Environmental &&
                _buildController.HasUpgrade(BuildUpgradeId.Humus_EnvironmentalTolerance))
            {
                multiplier *= EnvironmentalToleranceMultiplier;
            }

            return multiplier;
        }

        /// <summary>Legacy entry point used by enemy contact attacks; it now triggers damage without cancelling the hit.</summary>
        public bool TryTriggerPollutionCoat(GameObject attacker)
        {
            if (_buildController == null ||
                !_buildController.HasUpgrade(BuildUpgradeId.Humus_CadaverDelay) ||
                attacker == null ||
                UnityEngine.Random.value > ParasiticResponseChance)
            {
                return false;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, ParasiticResponseRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                HealthComponent target = hit.GetComponentInParent<HealthComponent>();
                if (target != null && target.gameObject != gameObject && target.IsAlive)
                {
                    target.TakeDamage(new DamageInfo(
                        ParasiticResponseDamage,
                        gameObject,
                        hit.ClosestPoint(transform.position),
                        (target.transform.position - transform.position).normalized,
                        DamageType.Environmental));
                }
            }

            ParasiticResponseTriggered?.Invoke(attacker);
            return false;
        }

        public void OnCorpseReanimated(GameObject corpse)
        {
            // Retained as a compatibility hook; the new passive build has no corpse dependency.
        }

        private float GetRegenerationPerSecond(string sceneName)
        {
            if (!HasCore)
            {
                return 0f;
            }

            float baseRate = LevelBuildSelectionCatalog.GetSymbiosisRegenerationPerSecond(sceneName);
            if (_buildController.HasUpgrade(BuildUpgradeId.Humus_CompleteSymbiosis) &&
                sceneName == SceneNames.Level04FacilityCore)
            {
                return 4f;
            }

            if (_buildController.HasUpgrade(BuildUpgradeId.Humus_DeadMatterWhisper))
            {
                baseRate += 1f;
            }

            if (_buildController.HasUpgrade(BuildUpgradeId.Humus_AbnormalMetabolism))
            {
                baseRate += 1f;
            }

            return baseRate;
        }

        private void HandleUpgradeGranted(BuildUpgradeId id)
        {
            if (id == BuildUpgradeId.Humus_Sympathy)
            {
                _nextRegenerationTime = Time.time + RegenerationTickSeconds;
            }

            if (id == BuildUpgradeId.Humus_ExpandedVessel)
            {
                ApplyPermanentUpgrades();
            }
        }

        private void ApplyPermanentUpgrades()
        {
            if (_expandedVesselApplied || _buildController == null || _health == null ||
                !_buildController.HasUpgrade(BuildUpgradeId.Humus_ExpandedVessel))
            {
                return;
            }

            _expandedVesselApplied = true;
            _health.ModifyMaxHealth(ExpandedVesselBonusHealth);
        }
    }
}
