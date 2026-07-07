using System.Collections;
using TMPro;
using UnityEngine;

namespace DiveProtocol.UI
{
    /// <summary>
    /// Displays the top-screen survival unlock countdown and its completion message.
    /// </summary>
    public sealed class SurvivalCountdownUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Root object that should be shown while the countdown or completion message is visible.")]
        [SerializeField]
        private GameObject root;

        [Tooltip("Text used for the countdown title or completion message.")]
        [SerializeField]
        private TMP_Text titleText;

        [Tooltip("Text used for the remaining MM:SS countdown.")]
        [SerializeField]
        private TMP_Text timeText;

        [Header("Text")]
        [Tooltip("Default title shown when no custom title is provided.")]
        [SerializeField]
        private string defaultTitle = "DOOR LOCK SYSTEM UPDATE";

        [Tooltip("Default completion message shown when the unlock finishes.")]
        [SerializeField]
        private string completionMessage = "DOOR UNLOCKED";

        [Tooltip("How long the completion message remains visible before hiding.")]
        [SerializeField, Min(0f)]
        private float completionDisplaySeconds = 3f;

        private Coroutine _hideCoroutine;
        private int _lastDisplayedSeconds = int.MinValue;

        private void Awake()
        {
            Hide();
        }

        private void OnDisable()
        {
            StopHideCoroutine();
            SetRootActive(false);
        }

        /// <summary>
        /// Shows the countdown using a custom title and remaining time.
        /// </summary>
        public void ShowCountdown(string title, float remainingSeconds)
        {
            StopHideCoroutine();
            SetRootActive(true);

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(title)
                    ? defaultTitle
                    : title;
            }

            SetRemainingTime(remainingSeconds);
        }

        /// <summary>
        /// Updates the remaining countdown time in MM:SS format.
        /// </summary>
        public void SetRemainingTime(float remainingSeconds)
        {
            if (timeText == null)
            {
                return;
            }

            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            if (_lastDisplayedSeconds == totalSeconds)
            {
                return;
            }

            _lastDisplayedSeconds = totalSeconds;
            timeText.text = FormatRemainingTime(totalSeconds);
        }

        /// <summary>
        /// Shows the completion message and hides it after the requested duration.
        /// </summary>
        public void ShowCompleted(string message, float displaySeconds)
        {
            StopHideCoroutine();
            SetRootActive(true);

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(message)
                    ? completionMessage
                    : message;
            }

            if (timeText != null)
            {
                timeText.text = string.Empty;
            }

            _lastDisplayedSeconds = int.MinValue;

            float duration = displaySeconds > 0f
                ? displaySeconds
                : completionDisplaySeconds;

            if (duration > 0f && isActiveAndEnabled)
            {
                _hideCoroutine = StartCoroutine(HideAfterSeconds(duration));
            }
        }

        /// <summary>
        /// Hides the countdown UI immediately.
        /// </summary>
        public void Hide()
        {
            StopHideCoroutine();
            _lastDisplayedSeconds = int.MinValue;
            SetRootActive(false);
        }

        private IEnumerator HideAfterSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _hideCoroutine = null;
            SetRootActive(false);
        }

        private void StopHideCoroutine()
        {
            if (_hideCoroutine == null)
            {
                return;
            }

            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        private void SetRootActive(bool active)
        {
            GameObject targetRoot = root != null
                ? root
                : gameObject;

            if (targetRoot.activeSelf != active)
            {
                targetRoot.SetActive(active);
            }
        }

        private static string FormatRemainingTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            completionDisplaySeconds = Mathf.Max(0f, completionDisplaySeconds);
        }
#endif
    }
}
