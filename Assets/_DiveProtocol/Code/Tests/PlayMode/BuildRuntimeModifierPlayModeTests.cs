using System.Collections;
using DiveProtocol.Builds;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class BuildRuntimeModifierPlayModeTests
    {
        [UnityTest]
        public IEnumerator RedMarrowCoreAppliesLowHealthMovementAndGunDamageModifiers()
        {
            var player = new GameObject("Red Marrow Player");
            try
            {
                HealthComponent health = player.AddComponent<HealthComponent>();
                PlayerBuildController controller = player.AddComponent<PlayerBuildController>();
                yield return null;

                controller.GrantUpgrade(BuildUpgradeId.RedMarrow_Overdraft);
                health.TakeDamage(new DamageInfo(70f, null, Vector3.zero, Vector3.zero));

                Assert.That(controller.Modifiers.GetMoveSpeedMultiplier(), Is.EqualTo(1.15f).Within(0.001f));
                Assert.That(
                    controller.Modifiers.GetOutgoingGunDamageMultiplier(null, default),
                    Is.EqualTo(1.20f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(player);
            }
        }

        [UnityTest]
        public IEnumerator OpticNerveCoreAppliesItsMarkedTargetDamageMultiplier()
        {
            var player = new GameObject("Optic Nerve Player");
            var enemy = new GameObject("Marked Enemy");
            try
            {
                player.AddComponent<HealthComponent>();
                PlayerBuildController controller = player.AddComponent<PlayerBuildController>();
                HealthComponent target = enemy.AddComponent<HealthComponent>();
                MarkedTarget marker = enemy.AddComponent<MarkedTarget>();
                yield return null;

                controller.GrantUpgrade(BuildUpgradeId.OpticNerve_Calibration);
                marker.Mark(player, 5f);

                Assert.That(
                    controller.Modifiers.GetOutgoingGunDamageMultiplier(target, default),
                    Is.EqualTo(1.15f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(enemy);
            }
        }
    }
}
