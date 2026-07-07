using DiveProtocol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DiveProtocol
{
    /// <summary>Drives the temporary completion choices in the empty demo level.</summary>
    public sealed class DemoLevelController : MonoBehaviour
    {
        [SerializeField] private Text _runStateText;
        [SerializeField] private Button _completeDemoButton;
        [SerializeField] private Button _simulateDeathButton;

        private bool _isEndingRun;

        private void Start()
        {
            if (!AppRoot.TryGetInstance(out var appRoot))
            {
                Debug.LogWarning($"Demo Level started without AppRoot. Start Play Mode from {SceneNames.Bootstrap}.");
                SetButtonsInteractable(false);
                SetRunStateText("Run state unavailable");
                return;
            }

            var currentRun = appRoot.RunManager.CurrentRun;
            if (currentRun == null || !currentRun.IsActive)
            {
                Debug.LogError("Demo Level requires an active run.");
                SetButtonsInteractable(false);
                SetRunStateText("No active run");
                return;
            }

            SetRunStateText(
                $"Seed: {currentRun.Seed}\n" +
                $"Run ID: {currentRun.RunId}\n" +
                $"Health: {currentRun.Player.CurrentHealth} / {currentRun.Player.MaxHealth}\n" +
                $"Ammo: {currentRun.Player.LoadedAmmo} / {currentRun.Player.ReserveAmmo}\n" +
                $"Corpse Activity: {currentRun.Environment.CorpseActivity}\n" +
                $"Resource Density: {currentRun.Environment.ResourceDensity}\n" +
                $"Level: {currentRun.CurrentLevelId}");
        }

        public void CompleteDemo()
        {
            GameplayPauseController.ForceResumeActivePause();
            FinishRun(RunEndReason.DemoCompleted);
        }

        public void SimulateDeath()
        {
            GameplayPauseController.ForceResumeActivePause();
            FinishRun(RunEndReason.PlayerDied);
        }

        private void FinishRun(RunEndReason endReason)
        {
            if (_isEndingRun)
            {
                return;
            }

            if (!AppRoot.TryGetInstance(out var appRoot))
            {
                Debug.LogError("Cannot end the run because AppRoot is unavailable.");
                return;
            }

            _isEndingRun = true;
            SetButtonsInteractable(false);

            if (!appRoot.RunManager.EndRun(endReason))
            {
                _isEndingRun = false;
                SetButtonsInteractable(true);
                return;
            }

            if (!appRoot.SceneLoader.LoadScene(SceneNames.Results, GameState.Results))
            {
                Debug.LogError("The run ended, but the Results scene could not be loaded.");
            }
        }

        private void SetRunStateText(string value)
        {
            if (_runStateText != null)
            {
                _runStateText.text = value;
            }
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            if (_completeDemoButton != null)
            {
                _completeDemoButton.interactable = isInteractable;
            }

            if (_simulateDeathButton != null)
            {
                _simulateDeathButton.interactable = isInteractable;
            }
        }
    }
}
