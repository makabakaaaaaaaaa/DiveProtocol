using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class AbortRunFlowPlayModeTests
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
        public IEnumerator AbortClearsRunWithoutResultsRewardsOrPersistence()
        {
            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);
            var run = AppRoot.Instance.RunManager.CurrentRun;
            run.Player.TakeDamage(40);
            run.Player.TryConsumeLoadedAmmo();
            run.EnterLevel("L_ABORT_TEST", 7);
            run.Inventory.AddKeyItem("AbortKey");
            run.Score.AddBonusScore(500);
            var metaBefore = AppRoot.Instance.SaveManager.CurrentMeta.Clone();

            var commands = new RunDebugCommands(AppRoot.Instance);
            Assert.That(commands.AbortCurrentRun(), Is.True);
            yield return SceneTestUtility.WaitForScene(SceneNames.MainMenu);

            Assert.That(AppRoot.Instance.RunManager.CurrentRun, Is.Null);
            Assert.That(AppRoot.Instance.RunManager.LastResult, Is.Null);
            Assert.That(AppRoot.Instance.SaveManager.CurrentMeta.TotalCurrency, Is.EqualTo(metaBefore.TotalCurrency));
            Assert.That(AppRoot.Instance.SaveManager.CurrentMeta.TotalRunsSettled, Is.EqualTo(metaBefore.TotalRunsSettled));
            Assert.That(AppRoot.Instance.SaveManager.CurrentMeta.RecentlyProcessedRunIds.Count, Is.EqualTo(metaBefore.RecentlyProcessedRunIds.Count));
            Assert.That(File.ReadAllText(AppRoot.Instance.SaveManager.SaveFilePath), Does.Not.Contain("L_ABORT_TEST"));

            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);
            var newRun = AppRoot.Instance.RunManager.CurrentRun;
            Assert.That(newRun.Player.CurrentHealth, Is.EqualTo(newRun.Player.MaxHealth));
            Assert.That(newRun.Inventory.HasKeyItem("AbortKey"), Is.False);
            Assert.That(newRun.Score.TotalScore, Is.Zero);
            Assert.That(newRun.CurrentLevelId, Is.EqualTo(RunFactory.InitialLevelId));
        }
    }
}
