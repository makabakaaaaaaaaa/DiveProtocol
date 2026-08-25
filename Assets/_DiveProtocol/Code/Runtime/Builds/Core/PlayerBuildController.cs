using System.Collections.Generic;
using UnityEngine;
using DiveProtocol.Doors;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Player-owned runtime coordinator for run build upgrades.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerBuildController : MonoBehaviour
    {
        [SerializeField] private PlayerBuildState state = new();

        private HealthComponent _health;
        private PlayerHitscanWeapon _weapon;
        private PlayerMovement _movement;
        private CharacterController _characterController;

        public PlayerBuildState State => state;
        public PlayerBuildRuntimeModifiers Modifiers { get; private set; }
        public BloodDebtController BloodDebt { get; private set; }
        public OpticNerveMarkerController OpticNerve { get; private set; }
        public SymbiosisController Symbiosis { get; private set; }

        private void Awake()
        {
            state ??= new PlayerBuildState();
            Modifiers = new PlayerBuildRuntimeModifiers(this);
            ResolveReferences();
            EnsureBranchControllers();
            state.BuildsReset += HandleBuildsReset;
        }

        private void OnDestroy()
        {
            if (state != null)
            {
                state.BuildsReset -= HandleBuildsReset;
            }
        }

        private void Update()
        {
            if (_movement != null && Modifiers != null)
            {
                _movement.ExternalSpeedMultiplier = Modifiers.GetMoveSpeedMultiplier();
            }
        }

        /// <summary>
        /// Grants a build upgrade to this player.
        /// </summary>
        public void GrantUpgrade(BuildUpgradeId id)
        {
            state.GrantUpgrade(id);
        }

        /// <summary>
        /// Replaces this transient player copy with the upgrades owned by the active run.
        /// </summary>
        public void ReplaceUpgrades(IReadOnlyCollection<BuildUpgradeId> upgrades)
        {
            state.ClearAllUpgrades();
            if (upgrades == null)
            {
                return;
            }

            foreach (BuildUpgradeId id in upgrades)
            {
                state.GrantUpgrade(id);
            }
        }

        public void ClearUpgrades()
        {
            state.ClearAllUpgrades();
        }

        public bool HasUpgrade(BuildUpgradeId id)
        {
            return state.HasUpgrade(id);
        }

        /// <summary>Lets branch effects intercept a lethal hit without coupling HealthComponent to a specific build.</summary>
        public bool TryPreventLethalDamage()
        {
            return BloodDebt != null && BloodDebt.TryPreventLethalDamage();
        }

        /// <summary>
        /// Attempts the Red Marrow HP-for-ammo action.
        /// </summary>
        public bool TrySpendHealthForAmmo()
        {
            return BloodDebt != null && BloodDebt.TrySpendHealthForAmmo();
        }

        /// <summary>
        /// Attempts to force an opted-in door open through Red Marrow blood debt.
        /// </summary>
        public bool TrySpendHealthForDoor(DoorInteractable door)
        {
            return BloodDebt != null && BloodDebt.TrySpendHealthForDoor(door);
        }

        public bool IsPlayerLowSpeed()
        {
            if (_movement != null)
            {
                return _movement.CurrentHorizontalSpeed <= 0.1f;
            }

            if (_characterController != null)
            {
                Vector3 velocity = _characterController.velocity;
                velocity.y = 0f;
                return velocity.sqrMagnitude <= 0.01f;
            }

            return false;
        }

        public HealthComponent Health => _health;
        public PlayerHitscanWeapon Weapon => _weapon;

        private void ResolveReferences()
        {
            _health = GetComponent<HealthComponent>();
            _weapon = GetComponent<PlayerHitscanWeapon>();
            _movement = GetComponent<PlayerMovement>();
            _characterController = GetComponent<CharacterController>();

            if (_health == null)
            {
                Debug.LogWarning("[Builds] PlayerBuildController could not find HealthComponent on the player.", this);
            }

            if (_weapon == null)
            {
                Debug.LogWarning("[Builds] PlayerBuildController could not find PlayerHitscanWeapon on the player.", this);
            }
        }

        private void EnsureBranchControllers()
        {
            BloodDebt = GetComponent<BloodDebtController>();
            if (BloodDebt == null)
            {
                BloodDebt = gameObject.AddComponent<BloodDebtController>();
            }

            OpticNerve = GetComponent<OpticNerveMarkerController>();
            if (OpticNerve == null)
            {
                OpticNerve = gameObject.AddComponent<OpticNerveMarkerController>();
            }

            Symbiosis = GetComponent<SymbiosisController>();
            if (Symbiosis == null)
            {
                Symbiosis = gameObject.AddComponent<SymbiosisController>();
            }
        }

        private void HandleBuildsReset()
        {
            if (BloodDebt != null)
            {
                BloodDebt.ResetLevelState();
            }

            if (_movement != null)
            {
                _movement.ExternalSpeedMultiplier = 1f;
            }
        }
    }
}
