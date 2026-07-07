using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class AppRootPlayModeTests
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
        public IEnumerator AppRootIsUniquePersistentAndStartsWithoutRun()
        {
            var appRoot = AppRoot.Instance;
            Assert.That(appRoot.SceneLoader, Is.Not.Null);
            Assert.That(appRoot.RunManager, Is.Not.Null);
            Assert.That(appRoot.SaveManager, Is.Not.Null);
            Assert.That(appRoot.SaveManager.CurrentMeta, Is.Not.Null);
            Assert.That(appRoot.RunManager.CurrentRun, Is.Null);

            new GameObject("DuplicateAppRoot", typeof(GameStateMachine), typeof(SceneLoader), typeof(RunManager), typeof(AppRoot));
            yield return null;
            Assert.That(Object.FindObjectsByType<AppRoot>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level01Drainage);
            Assert.That(AppRoot.Instance, Is.SameAs(appRoot));
        }

        [UnityTest]
        public IEnumerator SimulatedProcessRestartDoesNotRestoreActiveRun()
        {
            AppRoot.Instance.RunManager.StartNewRun(9191);
            Assert.That(AppRoot.Instance.RunManager.CurrentRun, Is.Not.Null);
            Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;

            yield return SceneTestUtility.LoadBootstrap(_saveDirectory);
            Assert.That(AppRoot.Instance.RunManager.CurrentRun, Is.Null);
        }
    }
}
