using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class ResultsSettlementPlayModeTests
    {
        private string _saveDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!SceneTestUtility.SystemScenesAreAvailable(out var reason)) Assert.Ignore(reason);
            _saveDirectory = SceneTestUtility.CreateTemporarySaveDirectory();
            yield return SceneTestUtility.LoadBootstrap(_saveDirectory);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => SceneTestUtility.Cleanup(_saveDirectory);

        [UnityTest]
        public IEnumerator PlayerDeathSettlesCurrencyWithoutSuccessAndCannotResume()
        {
            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);
            AppRoot.Instance.RunManager.CurrentRun.Score.AddBonusScore(100);
            Object.FindFirstObjectByType<DemoLevelController>().SimulateDeath();
            yield return SceneTestUtility.WaitForScene(SceneNames.Results);
            yield return null;

            var meta = AppRoot.Instance.SaveManager.CurrentMeta;
            Assert.That(meta.TotalRunsSettled, Is.EqualTo(1));
            Assert.That(meta.SuccessfulRuns, Is.Zero);
            Assert.That(meta.TotalCurrency, Is.EqualTo(10));

            Object.FindFirstObjectByType<ResultsController>().ReturnToMainMenu();
            yield return SceneTestUtility.WaitForScene(SceneNames.MainMenu);
            Assert.That(AppRoot.Instance.RunManager.CurrentRun, Is.Null);
            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);
            Assert.That(AppRoot.Instance.RunManager.CurrentRun.CurrentLevelId, Is.EqualTo(RunFactory.InitialLevelId));
            Assert.That(AppRoot.Instance.RunManager.CurrentRun.Player.CurrentHealth,
                Is.EqualTo(AppRoot.Instance.RunManager.CurrentRun.Player.MaxHealth));
        }
    }
}
