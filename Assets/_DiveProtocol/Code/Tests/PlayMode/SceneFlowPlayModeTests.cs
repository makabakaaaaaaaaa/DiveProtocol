using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class SceneFlowPlayModeTests
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
        public IEnumerator BootstrapFacilityMenuNewDescentResultsAndMainMenuFlowSettlesOnce()
        {
            Button newDescentButton = GameObject.Find("NEW DESCENT")?.GetComponent<Button>();
            Assert.That(newDescentButton, Is.Not.Null, "MainMenu_Facility must expose its NEW DESCENT Button.");
            Assert.That(newDescentButton.interactable, Is.True);

            newDescentButton.onClick.Invoke();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);
            Assert.That(AppRoot.Instance.RunManager.CurrentRun.IsActive, Is.True);

            Object.FindFirstObjectByType<DemoLevelController>().CompleteDemo();
            yield return SceneTestUtility.WaitForScene(SceneNames.Results);
            yield return null;
            Assert.That(AppRoot.Instance.SaveManager.CurrentMeta.TotalRunsSettled, Is.EqualTo(1));

            Object.FindFirstObjectByType<ResultsController>().ReturnToMainMenu();
            yield return SceneTestUtility.WaitForScene(SceneNames.MainMenu);
            Assert.That(AppRoot.Instance.RunManager.CurrentRun, Is.Null);
        }

        [UnityTest]
        public IEnumerator RetryCreatesNewRunIdAndSeed()
        {
            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);
            var oldRunId = AppRoot.Instance.RunManager.CurrentRun.RunId;
            var oldSeed = AppRoot.Instance.RunManager.CurrentRun.Seed;
            Object.FindFirstObjectByType<DemoLevelController>().CompleteDemo();
            yield return SceneTestUtility.WaitForScene(SceneNames.Results);
            Object.FindFirstObjectByType<ResultsController>().Retry();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);

            Assert.That(AppRoot.Instance.RunManager.CurrentRun.RunId, Is.Not.EqualTo(oldRunId));
            Assert.That(AppRoot.Instance.RunManager.CurrentRun.Seed, Is.Not.EqualTo(oldSeed));
        }
    }
}
