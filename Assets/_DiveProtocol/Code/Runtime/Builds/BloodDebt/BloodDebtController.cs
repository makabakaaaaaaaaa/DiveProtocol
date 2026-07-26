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
        private const int CompressedAmmoBonus = 2;
        private const float DoorHealthCost = 8f;
        private const float LowHealthThreshold = 0.35f;
        private const float AdrenalineThreshold = 0.30f;
        private const float OrganCollateralThreshold = 0.20f;
        private const float MinimumRemainingHealth = 10f;
        private const float AmmoCooldownSeconds = 6f;
        private const float CoagulationDurationSeconds = 3f;

        private PlayerBuildController _buildController;
        private HealthComponent _health;
        private PlayerHitscanWeapon _weapon;
        private float _nextAmmoSpendTime;
        private float _coagulationUntilTime;
        private bool _nextHealingPenalty;
        private bool _organCollateralTriggeredThisLevel;

        public event Action<BloodDebtSpendType> HealthSpent;

        public bool IsLowHealthActive =>
            HasCore &&
            _health != null &&
            _health.MaxHealth > 0f &&
            _health.CurrentHealth / _health.MaxHealth <= LowHealthThreshold;

        public bool IsAdrenalineActive =>
            _buildController != null &&
            _buildController.HasUpgrade(BuildUpgradeId.RedMarrow_ExcessAdrenaline) &&
            _health != null &&
            _health.MaxHealth > 0f &&
            _health.CurrentHealth / _health.MaxHealth <= AdrenalineThreshold;

        public bool IsCoagulationActive => Time.time < _coagulationUntilTime;
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
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.HealthChanged -= HandleHealthChanged;
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

            if (!_health.TrySpendHealth(AmmoHealthCost, MinimumRemainingHealth, gameObject))
            {
                return false;
            }

            int ammoAmount = AmmoGain;
            if (_buildController.HasUpgrade(BuildUpgradeId.RedMarrow_BloodBulletCompression))
            {
                ammoAmount += CompressedAmmoBonus;
                _nextHealingPenalty = true;
            }

            _weapon.TryAddAmmo(ammoAmount);
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

            float cost = Mathf.Max(0f, bypass.HpCost > 0 ? bypass.HpCost : DoorHealthCost);
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

        /// <summary>
        /// Resets per-level Red Marrow state.
        /// </summary>
        public void ResetLevelState()
        {
            _organCollateralTriggeredThisLevel = false;
        }

        private void TriggerSpend(BloodDebtSpendType spendType)
        {
            if (_buildController != null &&
                _buildController.HasUpgrade(BuildUpgradeId.RedMarrow_CoagulationReflex))
            {
                _coagulationUntilTime = Time.time + CoagulationDurationSeconds;
            }

            HealthSpent?.Invoke(spendType);
        }

        private void HandleHealthChanged(HealthComponent health, float currentHealth, float maxHealth)
        {
            if (_organCollateralTriggeredThisLevel ||
                _buildController == null ||
                !_buildController.HasUpgrade(BuildUpgradeId.RedMarrow_OrganCollateral) ||
                maxHealth <= 0f ||
                currentHealth / maxHealth > OrganCollateralThreshold)
            {
                return;
            }

            _organCollateralTriggeredThisLevel = true;
            _health.ModifyMaxHealth(-10f, 30f);
            _health.Heal(10f);
        }
    }
}
