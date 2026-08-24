using DiveProtocol.Builds;
using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class BuildRunBridgeTests
    {
        [Test]
        public void SyncRestoresRunUpgradesToANewPlayerController()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var player = new GameObject("Build Bridge Player");
            try
            {
                RunState run = RunFactory.Create(123, config);
                Assert.That(run.BuildState.GrantUpgrade(BuildUpgradeId.OpticNerve_Calibration), Is.True);

                PlayerBuildController controller = BuildRunBridge.EnsureAndSync(player.transform, run);

                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.HasUpgrade(BuildUpgradeId.OpticNerve_Calibration), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(config);
            }
        }

    }
}
