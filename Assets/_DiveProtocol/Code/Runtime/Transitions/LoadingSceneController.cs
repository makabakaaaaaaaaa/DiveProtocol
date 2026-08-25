using System.Collections;
using DiveProtocol.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiveProtocol
{
    /// <summary>
    /// Drives the unified loading scene presentation and delayed activation of the target scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadingSceneController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text descriptionText;

        [SerializeField]
        private Slider progressSlider;

        [SerializeField]
        private TMP_Text progressText;

        [Header("Presentation")]
        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private GameObject placeholderRoot;

        [Header("Direct Open Fallback")]
        [SerializeField]
        private string directOpenFallbackSceneName;

        private bool _isLoading;

        private void Start()
        {
            GameplayPauseController.RestoreGlobalPauseSideEffects();

            if (!SceneTransitionContext.HasPendingTransition)
            {
                HandleMissingPendingProfile();
                return;
            }

            StartCoroutine(LoadTargetRoutine(SceneTransitionContext.PendingProfile));
        }

        private IEnumerator LoadTargetRoutine(SceneTransitionProfile profile)
        {
            if (_isLoading)
            {
                yield break;
            }

            _isLoading = true;
            LoadingScreenOverlayService.Show(
                profile.TransitionTitle,
                profile.TransitionDescription);
            ApplyText(profile);
            PreparePresentation(profile);
            SetProgress(0f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(profile.TargetSceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"[Transition] Failed to start async load for scene '{profile.TargetSceneName}'.");
                SceneTransitionContext.Clear();
                SceneTransitionService.ResetRequestGuard();
                LoadingScreenOverlayService.Hide();
                _isLoading = false;
                yield break;
            }

            operation.allowSceneActivation = false;
            float startedAt = Time.unscaledTime;
            float presentationDuration = profile.OptionalPresentationPrefab != null
                ? profile.OptionalPresentationDurationSeconds
                : 0f;

            while (!CanActivate(operation, profile, startedAt, presentationDuration))
            {
                SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
                yield return null;
            }

            SetProgress(1f);
            SceneTransitionContext.Clear();
            SceneTransitionService.ResetRequestGuard();
            LoadingScreenOverlayService.HideAfterNextSceneActivation();
            operation.allowSceneActivation = true;
        }

        private static bool CanActivate(
            AsyncOperation operation,
            SceneTransitionProfile profile,
            float startedAt,
            float presentationDuration)
        {
            if (operation.progress < 0.9f)
            {
                return false;
            }

            float elapsed = Time.unscaledTime - startedAt;
            return elapsed >= profile.MinimumDisplaySeconds &&
                   elapsed >= presentationDuration;
        }

        private void ApplyText(SceneTransitionProfile profile)
        {
            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(profile.TransitionTitle)
                    ? "LOADING"
                    : profile.TransitionTitle;
            }

            if (descriptionText != null)
            {
                descriptionText.text = profile.TransitionDescription ?? string.Empty;
            }
        }

        private void PreparePresentation(SceneTransitionProfile profile)
        {
            if (profile.OptionalPresentationPrefab != null)
            {
                if (placeholderRoot != null)
                {
                    placeholderRoot.SetActive(false);
                }

                Transform parent = presentationRoot != null
                    ? presentationRoot
                    : transform;

                Instantiate(profile.OptionalPresentationPrefab, parent);
                return;
            }

            if (placeholderRoot != null)
            {
                placeholderRoot.SetActive(true);
            }
        }

        private void SetProgress(float normalizedProgress)
        {
            float clampedProgress = Mathf.Clamp01(normalizedProgress);

            if (progressSlider != null)
            {
                progressSlider.value = clampedProgress;
            }

            if (progressText != null)
            {
                int percent = Mathf.RoundToInt(clampedProgress * 100f);
                progressText.text = $"{percent}%";
            }

            LoadingScreenOverlayService.SetProgress(clampedProgress);
        }

        private void HandleMissingPendingProfile()
        {
            Debug.LogWarning("[Transition] Loading scene opened without a pending SceneTransitionProfile.");

            if (!string.IsNullOrWhiteSpace(directOpenFallbackSceneName) &&
                Application.CanStreamedLevelBeLoaded(directOpenFallbackSceneName))
            {
                SceneManager.LoadScene(directOpenFallbackSceneName, LoadSceneMode.Single);
                return;
            }

            SetProgress(0f);
        }
    }
}
