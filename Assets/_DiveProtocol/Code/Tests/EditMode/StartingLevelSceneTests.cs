using DiveProtocol.Editor;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class StartingLevelSceneTests
    {
        [Test]
        public void GameConfigDefaultsToLevel01Drainage()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                Assert.That(config.StartingLevelSceneName, Is.EqualTo(SceneNames.Level01Drainage));
                Assert.That(config.StartingLevelSceneName, Is.Not.EqualTo(SceneNames.DemoLevel));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void BuildSceneCompositionKeepsCoreScenesFirstAndDoesNotDuplicate()
        {
            var existing = new[]
            {
                "Assets/_DiveProtocol/Scenes/Levels/SCN_DemoLevel.unity",
                "Assets/_DiveProtocol/Scenes/Levels/SCN_L01_Drainage.unity",
                "Assets/SomeOtherScene.unity"
            };

            var composed = BuildSceneConfiguration.ComposeBuildScenes(BuildSceneConfiguration.CoreBuildScenePaths, existing);

            Assert.That(BuildSceneConfiguration.StartsWithCoreScenes(composed, BuildSceneConfiguration.CoreBuildScenePaths), Is.True);
            Assert.That(composed, Has.Length.EqualTo(composed.Distinct().Count()));
            Assert.That(composed[0], Does.EndWith($"{SceneNames.Bootstrap}.unity"));
            Assert.That(composed[2], Does.EndWith($"{SceneNames.Level01Drainage}.unity"));
            Assert.That(composed[2], Does.Not.EndWith($"{SceneNames.DemoLevel}.unity"));
            Assert.That(composed, Does.Contain("Assets/SomeOtherScene.unity"));
        }

        [Test]
        public void BuildSceneOrderValidationRejectsMissingLevel01()
        {
            var configuredWithoutLevel01 = new[]
            {
                "Assets/_DiveProtocol/Scenes/System/SCN_Bootstrap.unity",
                "Assets/_DiveProtocol/Scenes/System/SCN_MainMenu.unity",
                "Assets/_DiveProtocol/Scenes/Levels/SCN_DemoLevel.unity",
                "Assets/_DiveProtocol/Scenes/System/SCN_Results.unity"
            };

            Assert.That(BuildSceneConfiguration.StartsWithCoreScenes(configuredWithoutLevel01, BuildSceneConfiguration.CoreBuildScenePaths), Is.False);
        }
    }
}
