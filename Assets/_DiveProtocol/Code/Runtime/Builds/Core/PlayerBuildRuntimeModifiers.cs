using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Unified numeric modifier facade used by existing runtime systems.
    /// </summary>
    public sealed class PlayerBuildRuntimeModifiers
    {
        private const float RedMarrowLowHealthGunMultiplier = 1.20f;
        private const float RedMarrowLowHealthMoveMultiplier = 1.12f;
        private const float CoagulationIncomingMultiplier = 0.70f;
        private const float AdrenalineReloadMultiplier = 1.30f;
        private const float AdrenalineInteractionMultiplier = 1.25f;
        private const float CalmShotMultiplier = 1.20f;
        private const float SafeDistance = 6f;
        private const float SafeDistanceCritChance = 0.10f;
        private const float SafeDistanceCritMultiplier = 1.50f;

        private readonly PlayerBuildController _controller;

        public PlayerBuildRuntimeModifiers(PlayerBuildController controller)
        {
            _controller = controller;
        }

        public float GetOutgoingGunDamageMultiplier(IDamageable target, DamageInfo info)
        {
            if (_controller == null)
            {
                return 1f;
            }

            float multiplier = 1f;

            if (_controller.BloodDebt != null && _controller.BloodDebt.IsLowHealthActive)
            {
                multiplier *= RedMarrowLowHealthGunMultiplier;
            }

            if (_controller.OpticNerve != null)
            {
                multiplier *= _controller.OpticNerve.GetMarkedDamageMultiplier(target);
            }

            if (_controller.HasUpgrade(BuildUpgradeId.OpticNerve_CalmShot) &&
                _controller.IsPlayerLowSpeed())
            {
                multiplier *= CalmShotMultiplier;
            }

            if (_controller.HasUpgrade(BuildUpgradeId.OpticNerve_SafeDistance) &&
                target is Component targetComponent)
            {
                float distance = Vector3.Distance(_controller.transform.position, targetComponent.transform.position);
                if (distance > SafeDistance && Random.value <= SafeDistanceCritChance)
                {
                    multiplier *= SafeDistanceCritMultiplier;
                }
            }

            return multiplier;
        }

        public float GetMoveSpeedMultiplier()
        {
            if (_controller != null &&
                _controller.BloodDebt != null &&
                _controller.BloodDebt.IsLowHealthActive)
            {
                return RedMarrowLowHealthMoveMultiplier;
            }

            return 1f;
        }

        public float GetIncomingDamageMultiplier(DamageInfo info)
        {
            if (_controller == null)
            {
                return 1f;
            }

            if (info.DamageType == DamageType.BloodCost)
            {
                return 1f;
            }

            float multiplier = 1f;

            if (_controller.BloodDebt != null && _controller.BloodDebt.IsCoagulationActive)
            {
                multiplier *= CoagulationIncomingMultiplier;
            }

            if (_controller.Symbiosis != null && info.DamageType == DamageType.Environmental)
            {
                multiplier *= _controller.Symbiosis.GetEnvironmentalDamageMultiplier();
            }

            return multiplier;
        }

        public float GetHealingMultiplier()
        {
            if (_controller != null && _controller.BloodDebt != null)
            {
                return _controller.BloodDebt.ConsumeHealingMultiplier();
            }

            return 1f;
        }

        public float GetInteractionSpeedMultiplier()
        {
            return _controller != null &&
                   _controller.BloodDebt != null &&
                   _controller.BloodDebt.IsAdrenalineActive
                ? AdrenalineInteractionMultiplier
                : 1f;
        }

        public float GetReloadSpeedMultiplier()
        {
            return _controller != null &&
                   _controller.BloodDebt != null &&
                   _controller.BloodDebt.IsAdrenalineActive
                ? AdrenalineReloadMultiplier
                : 1f;
        }

        public float GetWeaponSpreadMultiplier()
        {
            float multiplier = 1f;
            if (_controller != null && _controller.Symbiosis != null)
            {
                multiplier *= _controller.Symbiosis.GetWeaponSpreadMultiplier();
            }

            return multiplier;
        }
    }
}
