using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Unified numeric modifier facade used by existing runtime systems.
    /// </summary>
    public sealed class PlayerBuildRuntimeModifiers
    {
        private const float RedMarrowLowHealthGunMultiplier = 1.20f;
        private const float RedMarrowLowHealthMoveMultiplier = 1.15f;
        private const float BloodCompressionGunMultiplier = 1.15f;
        private const float LowHealthAmplifierGunMultiplier = 1.15f;
        private const float LowHealthAmplifierMoveMultiplier = 1.10f;
        private const float SacrificeGunMultiplier = 1.50f;
        private const float CalmShotMultiplier = 1.25f;
        private const float SafeDistance = 6f;
        private const float SafeDistanceMultiplier = 1.20f;
        private const float WeakPointCritChance = 0.25f;
        private const float WeakPointCritMultiplier = 1.50f;
        private const float PerfectPredictionCritMultiplier = 2f;

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
                if (_controller.HasUpgrade(BuildUpgradeId.RedMarrow_LowHealthAmplifier))
                {
                    multiplier *= LowHealthAmplifierGunMultiplier;
                }
            }

            if (_controller.HasUpgrade(BuildUpgradeId.RedMarrow_BloodBulletCompression))
            {
                multiplier *= BloodCompressionGunMultiplier;
            }

            if (_controller.BloodDebt != null && _controller.BloodDebt.IsSacrificeActive)
            {
                multiplier *= SacrificeGunMultiplier;
            }

            if (_controller.OpticNerve != null)
            {
                multiplier *= _controller.OpticNerve.GetMarkedDamageMultiplier(target);
                if (_controller.OpticNerve.IsMarked(target) &&
                    _controller.HasUpgrade(BuildUpgradeId.OpticNerve_JointRupture) &&
                    Random.value <= WeakPointCritChance)
                {
                    multiplier *= WeakPointCritMultiplier;
                }

                if (_controller.OpticNerve.TryConsumePerfectPrediction(target))
                {
                    multiplier *= PerfectPredictionCritMultiplier;
                }
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
                if (distance > SafeDistance)
                {
                    multiplier *= SafeDistanceMultiplier;
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
                float multiplier = RedMarrowLowHealthMoveMultiplier;
                if (_controller.HasUpgrade(BuildUpgradeId.RedMarrow_LowHealthAmplifier))
                {
                    multiplier *= LowHealthAmplifierMoveMultiplier;
                }

                return multiplier;
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

            if (_controller.Symbiosis != null)
            {
                multiplier *= _controller.Symbiosis.GetIncomingDamageMultiplier(info);
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
            return 1f;
        }

        public float GetReloadSpeedMultiplier()
        {
            return _controller != null && _controller.HasUpgrade(BuildUpgradeId.OpticNerve_CalmShot)
                ? CalmShotMultiplier
                : 1f;
        }

        public float GetWeaponSpreadMultiplier()
        {
            return 1f;
        }
    }
}
