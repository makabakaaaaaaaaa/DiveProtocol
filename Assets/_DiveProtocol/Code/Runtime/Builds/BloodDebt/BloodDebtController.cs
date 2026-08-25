using System;
using DiveProtocol.Doors;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Runtime implementation for Red Marrow blood-debt spend effects.
    /// </summary>
    public sealed class BloodDebtController : MonoBehaviour
    {
        private const float AmmoHealthCost = 6f;
        private const int AmmoGain = 4;
        private const float DoorHealthCost = 8f;
        private const float LowHealthThreshold = 0.50f;
        private const float SacrificeThreshold = 0.20f;
        private const float MinimumRemainingHealth = 10f;
        private const float AmmoCooldownSeconds = 6f;
        private const float LethalSaveCooldownSeconds = 60f;
        private const float KillRecoveryHealth = 5f;
        private const float SacrificeDurationSeconds = 10f;

        private PlayerBuildController _buildController;
        private HealthComponent _health;
        private PlayerHitscanWeapon _weapon;
        private float _nextAmmoSpendTime;
        private float _nextLethalSaveTime;
        private float _sacrificeUntilTime;
        private bool _nextHealingPenalty;
        private bool _bloodCompressionApplied;
        private bool _wasBelowSacrificeThreshold;

        public event Action<BloodDebtSpendType> HealthSpent;

        public bool IsLowHealthActive =>
            HasCore &&
            _health != null &&
            _health.MaxHealth > 0f &&
            _health.CurrentHealth / _health.MaxHealth <= LowHealthThreshold;

        public bool IsSacrificeActive => Time.time < _sacrificeUntilTime;
        public bool HasPendingHealingPenalty => _nextHealingPenalty;

        private bool HasCore =>
            _buildController != null &&
            _buildController.HasUpgrade(BuildUpgradeId.RedMarrow_Overdraft);

        private void Awake()
        {
            _buildController = GetComponent<PlayerBuildController>();
            _health = GetComponent<HealthComponent>();
            _weapon = GetComponent<PlayerHitscanWeapon>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.HealthChanged += HandleHealthChanged;
            }

            if (_weapon != null)
            {
                _weapon.EnemyKilled += HandleEnemyKilled;
            }

            if (_buildController != null)
            {
                _buildController.State.UpgradeGranted += HandleUpgradeGranted;
                ApplyPermanentUpgrades();
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.HealthChanged -= HandleHealthChanged;
            }

            if (_weapon != null)
            {
                _weapon.EnemyKilled -= HandleEnemyKilled;
            }

            if (_buildController != null)
            {
                _buildController.State.UpgradeGranted -= HandleUpgradeGranted;
            }
        }

        /// <summary>
        /// Spends HP to create pistol ammo if Red Marrow core is owned.
        /// </summary>
        public bool TrySpendHealthForAmmo()
        {
            if (!HasCore || _health == null || _weapon == null || Time.time < _nextAmmoSpendTime)
            {
                return false;
            }

            if (!_health.TrySpendHealth(GetHealthSpendCost(AmmoHealthCost), MinimumRemainingHealth, gameObject))
            {
                return false;
            }

            _weapon.TryAddAmmo(AmmoGain);
            _nextAmmoSpendTime = Time.time + AmmoCooldownSeconds;
            TriggerSpend(BloodDebtSpendType.Ammo);
            return true;
        }

        /// <summary>
        /// Spends HP to permanently unlock and toggle an opted-in door.
        /// </summary>
        public bool TrySpendHealthForDoor(DoorInteractable door)
        {
            if (!HasCore || _health == null || door == null)
            {
                return false;
            }

            BloodDebtDoorBypass bypass = door.GetComponent<BloodDebtDoorBypass>() ??
                                         door.GetComponentInParent<BloodDebtDoorBypass>();
            if (bypass == null || !bypass.AllowBloodBypass)
            {
                return false;
            }

            float cost = GetHealthSpendCost(Mathf.Max(0f, bypass.HpCost > 0 ? bypass.HpCost : DoorHealthCost));
            if (!_health.TrySpendHealth(cost, MinimumRemainingHealth, gameObject))
            {
                return false;
            }

            door.UnlockPermanently();
            door.Interact(gameObject);
            TriggerSpend(BloodDebtSpendType.Door);
            return true;
        }

        public float ConsumeHealingMultiplier()
        {
            if (!_nextHealingPenalty)
            {
                return 1f;
            }

            _nextHealingPenalty = false;
            return 0.5f;
        }

        /// <summary>Resets transient low-health activation state for a new scene.</summary>
        public void ResetLevelState()
        {
            _wasBelowSacrificeThreshold = false;
            _sacrificeUntilTime = 0f;
        }

        /// <summary>Consumes the Coagulation Reflex lethal save when its cooldown is ready.</summary>
        public bool TryPreventLethalDamage()
        {
            if (_buildController == null ||
                !_buildController.HasUpgrade(BuildUpgradeId.RedMarrow_CoagulationReflex) ||
                Time.time < _nextLethalSaveTime)
            {
                return false;
            }

            _nextLethalSaveTime = Time.time + LethalSaveCooldownSeconds;
            _health?.SetInvulnerableFor(0.15f);
            return true;
        }

        private void TriggerSpend(BloodDebtSpendType spendType)
        {
            HealthSpent?.Invoke(spendType);
        }

        private void HandleHealthChanged(HealthComponent health, float currentHealth, float maxHealth)
        {
            if (_buildController == null || maxHealth <= 0f)
            {
                return;
            }

            bool belowSacrificeThreshold = currentHealth / maxHealth <= SacrificeThreshold;
            if (_buildController.HasUpgrade(BuildUpgradeId.RedMarrow_SacrificeProtocol) &&
                belowSacrificeThreshold &&
                !_wasBelowSacrificeThreshold)
            {
                _sacrificeUntilTime = Time.time + SacrificeDurationSeconds;
            }

            _wasBelowSacrificeThreshold = belowSacrificeThreshold;
        }

        private void HandleEnemyKilled(GameObject enemy)
        {
            if (_buildController != null &&
                _buildController.HasUpgrade(BuildUpgradeId.RedMarrow_ExcessAdrenaline))
            {
                _health?.Heal(KillRecoveryHealth);
            }
        }

        private void HandleUpgradeGranted(BuildUpgradeId id)
        {
            if (id == BuildUpgradeId.RedMarrow_BloodBulletCompression)
            {
                ApplyPermanentUpgrades();
            }
        }

        private void ApplyPermanentUpgrades()
        {
            if (_bloodCompressionApplied || _buildController == null || _health == null ||
                !_buildController.HasUpgrade(BuildUpgradeId.RedMarrow_BloodBulletCompression))
            {
                return;
            }

            _bloodCompressionApplied = true;
            _health.ModifyMaxHealth(-10f, 10f);
        }

        private float GetHealthSpendCost(float baseCost)
        {
            float multiplier = 1f;
            if (_buildController != null && _buildController.HasUpgrade(BuildUpgradeId.RedMarrow_OrganCollateral))
            {
                multiplier *= 0.5f;
            }

            if (_buildController != null && _buildController.HasUpgrade(BuildUpgradeId.RedMarrow_BloodEconomy))
            {
                multiplier *= 0.75f;
            }

            return baseCost * multiplier;
        }
    }
}
