using DiveProtocol.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiveProtocol
{
    /// <summary>
    /// Begins loading-scene transitions requested by exits or future scripted events.
    /// </summary>
    public static class SceneTransitionService
    {
        private static bool _isTransitionStarting;
        private static int _lastRequestFrame = -1;

        /// <summary>
        /// Stores a transition profile and loads its configured loading scene.
        /// </summary>
        public static bool BeginTransition(SceneTransitionProfile profile)
        {
            if (_isTransitionStarting || _lastRequestFrame == Time.frameCount)
            {
                Debug.LogWarning("[Transition] Ignored duplicate scene transition request.");
                return false;
            }

            if (!ValidateProfile(profile))
            {
                return false;
            }

            _isTransitionStarting = true;
            _lastRequestFrame = Time.frameCount;
            SceneTransitionContext.SetPendingTransition(profile);
            GameplayPauseController.ForceResumeActivePause();

            SceneManager.LoadScene(profile.LoadingSceneName, LoadSceneMode.Single);
            return true;
        }

        /// <summary>
        /// Allows the loading scene to clear the duplicate-request guard before activating the target scene.
        /// </summary>
        public static void ResetRequestGuard()
        {
            _isTransitionStarting = false;
        }

        private static bool ValidateProfile(SceneTransitionProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("[Transition] Cannot begin transition because the profile is missing.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile.LoadingSceneName))
            {
                Debug.LogError("[Transition] Cannot begin transition because Loading Scene Name is empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile.TargetSceneName))
            {
                Debug.LogError("[Transition] Cannot begin transition because Target Scene Name is empty.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(profile.LoadingSceneName))
            {
                Debug.LogError(
                    $"[Transition] Loading scene '{profile.LoadingSceneName}' is not in Build Settings.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(profile.TargetSceneName))
            {
                Debug.LogError(
                    $"[Transition] Target scene '{profile.TargetSceneName}' is not in Build Settings.");
                return false;
            }

            return true;
        }
    }
}
