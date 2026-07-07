using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiveProtocol.Tests.PlayMode
{
    internal static class SceneTestUtility
    {
        private static readonly string[] _requiredScenes =
        {
            SceneNames.Bootstrap, SceneNames.MainMenu, SceneNames.Level01Drainage, SceneNames.DemoLevel, SceneNames.Results
        };

        internal static string CreateTemporarySaveDirectory()
        {
            return Path.Combine(Path.GetTempPath(), $"DiveProtocolPlayMode_{Guid.NewGuid():N}");
        }

        internal static bool SystemScenesAreAvailable(out string reason)
        {
            foreach (var sceneName in _requiredScenes)
            {
                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    reason = $"{sceneName} is missing from Build Settings. Run Create/Configure System Scenes first.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        internal static IEnumerator LoadBootstrap(string saveDirectory)
        {
            AppRoot.SetSaveManagerFactoryForTests(() => new SaveManager(saveDirectory));
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            Assert.That(AppRoot.TryGetInstance(out var appRoot), Is.True);
            Assert.That(appRoot.RunManager.CurrentRun, Is.Null);
        }

        internal static IEnumerator WaitForScene(string sceneName, float timeoutSeconds = 10f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName), $"Timed out waiting for {sceneName}.");
        }

        internal static IEnumerator WaitForLoadingComplete(float timeoutSeconds = 10f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (AppRoot.Instance != null && AppRoot.Instance.SceneLoader.IsLoading && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(AppRoot.Instance.SceneLoader.IsLoading, Is.False, "Timed out waiting for scene loading to finish.");
        }

        internal static IEnumerator Cleanup(string saveDirectory)
        {
            if (AppRoot.Instance != null)
            {
                UnityEngine.Object.Destroy(AppRoot.Instance.gameObject);
                yield return null;
            }

            AppRoot.ResetTestOverrides();
            if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
        }
    }
}
