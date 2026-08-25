using System.Collections;
using DiveProtocol.Builds;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class SymbiosisRegenerationPlayModeTests
    {
        [UnityTest]
        public IEnumerator CoreRegenerationUsesSceneDepthAndOneSecondTicks()
        {
            Scene drainage = SceneManager.CreateScene(SceneNames.Level01Drainage);
            SceneManager.SetActiveScene(drainage);
            var player = new GameObject("Symbiosis Test Player");
            try
            {
                HealthComponent health = player.AddComponent<HealthComponent>();
                PlayerBuildController controller = player.AddComponent<PlayerBuildController>();
                yield return null;

                controller.GrantUpgrade(BuildUpgradeId.Humus_Sympathy);
                health.TakeDamage(new DamageInfo(10f, null, Vector3.zero, Vector3.zero));
                yield return new WaitForSeconds(1.1f);
                Assert.That(health.CurrentHealth, Is.EqualTo(91f).Within(0.1f));

                Scene containment = SceneManager.CreateScene(SceneNames.Level02Containment);
                SceneManager.SetActiveScene(containment);
                health.TakeDamage(new DamageInfo(10f, null, Vector3.zero, Vector3.zero));
                yield return new WaitForSeconds(1.1f);
                Assert.That(health.CurrentHealth, Is.EqualTo(83f).Within(0.1f));
            }
            finally
            {
                Object.Destroy(player);
            }
        }
    }
}
