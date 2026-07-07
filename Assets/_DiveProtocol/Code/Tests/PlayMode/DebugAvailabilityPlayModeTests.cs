#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class DebugAvailabilityPlayModeTests
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
        public IEnumerator CommandsAreSafeAndRestartCreatesFreshRunIdentity()
        {
            Assert.That(AppRoot.Instance.GetComponent<DebugOverlay>(), Is.Not.Null);
            var commands = new RunDebugCommands(AppRoot.Instance);
            Assert.That(commands.HealFull(), Is.False);
            Assert.That(commands.AbortCurrentRun(), Is.False);
            Assert.That(commands.AddTestCurrency(), Is.True);
            Assert.That(AppRoot.Instance.SaveManager.CurrentMeta.TotalCurrency,
                Is.EqualTo(RunDebugCommands.DebugCurrencyAmount));

            Assert.That(AppRoot.Instance.RunManager.StartNewRun(555), Is.True);
            var firstId = AppRoot.Instance.RunManager.CurrentRun.RunId;
            AppRoot.Instance.RunManager.CurrentRun.Player.TakeDamage(50);
            Assert.That(commands.HealFull(), Is.True);
            Assert.That(AppRoot.Instance.RunManager.CurrentRun.Player.CurrentHealth,
                Is.EqualTo(AppRoot.Instance.RunManager.CurrentRun.Player.MaxHealth));
            Assert.That(commands.AddAmmo(), Is.True);

            Assert.That(commands.RestartSameSeed(), Is.True);
            var sameSeedId = AppRoot.Instance.RunManager.CurrentRun.RunId;
            Assert.That(AppRoot.Instance.RunManager.CurrentRun.Seed, Is.EqualTo(555));
            Assert.That(sameSeedId, Is.Not.EqualTo(firstId));
            yield return SceneTestUtility.WaitForLoadingComplete();

            Assert.That(commands.RestartNewSeed(), Is.True);
            Assert.That(AppRoot.Instance.RunManager.CurrentRun.Seed, Is.Not.EqualTo(555));
            Assert.That(AppRoot.Instance.RunManager.CurrentRun.RunId, Is.Not.EqualTo(sameSeedId));
            yield return SceneTestUtility.WaitForLoadingComplete();

            Assert.That(commands.ForceDemoComplete(), Is.True);
            Assert.That(commands.ForceDemoComplete(), Is.False);
            yield return SceneTestUtility.WaitForScene(SceneNames.Results);
            yield return null;
            Assert.That(AppRoot.Instance.SaveManager.CurrentMeta.TotalRunsSettled, Is.EqualTo(1));
        }
    }
}
#endif
