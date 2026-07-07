using System;
using DiveProtocol.Interaction;
using DiveProtocol.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DiveProtocol.Doors
{
    /// <summary>
    /// Starts a timed survival unlock sequence before allowing a door to be toggled normally.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurvivalUnlockDoorInteractable : InteractableBase
    {
        [Header("Door")]
        [Tooltip("Door controller that remains locked until the survival timer completes.")]
        [SerializeField]
        private DoorController doorController;

        [Header("Survival Unlock")]
        [Tooltip("Duration of the survival sequence in scaled seconds.")]
        [SerializeField, Min(1f)]
        private float unlockDurationSeconds = 120f;

        [Header("Prompts")]
        [Tooltip("Prompt shown before the timed unlock has started.")]
        [SerializeField]
        private string startPrompt = "Start Door Unlock";

        [Tooltip("Prompt shown after completion while the door is closed.")]
        [SerializeField]
        private string openPrompt = "Open Door";

        [Tooltip("Prompt shown after completion while the door is open.")]
        [SerializeField]
        private string closePrompt = "Close Door";

        [Header("Countdown UI")]
        [Tooltip("Title shown at the top of the countdown UI.")]
        [SerializeField]
        private string countdownTitle = "DOOR LOCK SYSTEM UPDATE";

        [Tooltip("Message shown after the timed unlock completes.")]
        [SerializeField]
        private string completionMessage = "DOOR UNLOCKED";

        [Tooltip("How long the completion message remains visible.")]
        [SerializeField, Min(0f)]
        private float completionDisplaySeconds = 3f;

        [Header("Events")]
        [Tooltip("Invoked once when the survival unlock sequence starts.")]
        [SerializeField]
        private UnityEvent onSurvivalStarted;

        [Tooltip("Invoked once when the survival unlock sequence completes.")]
        [SerializeField]
        private UnityEvent onSurvivalCompleted;

        private SurvivalCountdownUI _countdownUI;
        private bool _hasLoggedMissingDoorController;
        private bool _hasLoggedMissingCountdownUI;

        /// <summary>
        /// Raised once when the survival unlock sequence starts.
        /// </summary>
        public event Action<SurvivalUnlockDoorInteractable> SurvivalStarted;

        /// <summary>
        /// Raised once when the survival unlock sequence completes.
        /// </summary>
        public event Action<SurvivalUnlockDoorInteractable> SurvivalCompleted;

        /// <summary>
        /// Gets whether the timed survival sequence is currently running.
        /// </summary>
        public bool IsSurvivalRunning { get; private set; }

        /// <summary>
        /// Gets whether the timed survival sequence has completed for this scene lifetime.
        /// </summary>
        public bool IsUnlockCompleted { get; private set; }

        /// <summary>
        /// Gets the remaining scaled seconds before the door unlock completes.
        /// </summary>
        public float RemainingSeconds { get; private set; }

        public override string InteractionPrompt
        {
            get
            {
                if (!IsUnlockCompleted)
                {
                    return GetSafePrompt(startPrompt, "Start Door Unlock");
                }

                return doorController != null && doorController.IsOpen
                    ? GetSafePrompt(closePrompt, "Close Door")
                    : GetSafePrompt(openPrompt, "Open Door");
            }
        }

        private void Awake()
        {
            ResolveDoorController();
            RemainingSeconds = unlockDurationSeconds;
        }

        private void Start()
        {
            if (doorController != null && !IsUnlockCompleted)
            {
                doorController.SetOpenImmediate(false);
            }
        }

        private void Update()
        {
            if (!IsSurvivalRunning)
            {
                return;
            }

            RemainingSeconds -= Time.deltaTime;

            if (RemainingSeconds > 0f)
            {
                if (_countdownUI != null)
                {
                    _countdownUI.SetRemainingTime(RemainingSeconds);
                }

                return;
            }

            CompleteSurvival();
        }

        private void OnDisable()
        {
            if (!IsSurvivalRunning)
            {
                return;
            }

            IsSurvivalRunning = false;

            if (_countdownUI != null)
            {
                _countdownUI.Hide();
            }
        }

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor)
                && doorController != null
                && !doorController.IsMoving
                && !IsSurvivalRunning;
        }

        public override string GetInteractionPrompt(GameObject interactor)
        {
            return InteractionPrompt;
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (!IsUnlockCompleted)
            {
                BeginSurvival(interactor);
                return;
            }

            doorController.Toggle();
        }

        /// <summary>
        /// Starts the survival unlock countdown for the provided player interactor.
        /// </summary>
        public bool BeginSurvival(GameObject interactor)
        {
            if (IsUnlockCompleted || IsSurvivalRunning)
            {
                return false;
            }

            if (doorController == null)
            {
                LogMissingDoorControllerOnce();
                return false;
            }

            if (interactor == null)
            {
                LogMissingCountdownUIOnce("No interactor was provided.");
                return false;
            }

            SurvivalCountdownUI countdownUI =
                interactor.GetComponentInChildren<SurvivalCountdownUI>(true);

            if (countdownUI == null)
            {
                LogMissingCountdownUIOnce(
                    $"Interactor '{interactor.name}' does not contain a {nameof(SurvivalCountdownUI)}.");
                return false;
            }

            _countdownUI = countdownUI;
            RemainingSeconds = unlockDurationSeconds;
            IsSurvivalRunning = true;

            _countdownUI.ShowCountdown(countdownTitle, RemainingSeconds);
            SurvivalStarted?.Invoke(this);
            onSurvivalStarted?.Invoke();

            return true;
        }

        /// <summary>
        /// Completes the survival unlock immediately without opening the door.
        /// </summary>
        public bool CompleteImmediately()
        {
            if (IsUnlockCompleted)
            {
                return false;
            }

            CompleteSurvival();
            return true;
        }

        private void CompleteSurvival()
        {
            if (IsUnlockCompleted)
            {
                return;
            }

            RemainingSeconds = 0f;
            IsSurvivalRunning = false;
            IsUnlockCompleted = true;

            if (_countdownUI != null)
            {
                _countdownUI.ShowCompleted(
                    completionMessage,
                    completionDisplaySeconds);
            }

            SurvivalCompleted?.Invoke(this);
            onSurvivalCompleted?.Invoke();
        }

        private void ResolveDoorController()
        {
            if (doorController != null)
            {
                return;
            }

            doorController = GetComponent<DoorController>();
            if (doorController != null)
            {
                return;
            }

            doorController = GetComponentInParent<DoorController>();
        }

        private void LogMissingDoorControllerOnce()
        {
            if (_hasLoggedMissingDoorController)
            {
                return;
            }

            _hasLoggedMissingDoorController = true;
            Debug.LogError(
                $"[Door] {nameof(SurvivalUnlockDoorInteractable)} on '{name}' requires a DoorController.",
                this);
        }

        private void LogMissingCountdownUIOnce(string details)
        {
            if (_hasLoggedMissingCountdownUI)
            {
                return;
            }

            _hasLoggedMissingCountdownUI = true;
            Debug.LogError(
                $"[Door] Cannot start survival unlock on '{name}'. {details}",
                this);
        }

        private static string GetSafePrompt(string prompt, string fallback)
        {
            return string.IsNullOrWhiteSpace(prompt)
                ? fallback
                : prompt;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            ResolveDoorController();
        }

        private void OnValidate()
        {
            unlockDurationSeconds = Mathf.Max(1f, unlockDurationSeconds);
            completionDisplaySeconds = Mathf.Max(0f, completionDisplaySeconds);
        }
#endif
    }
}
