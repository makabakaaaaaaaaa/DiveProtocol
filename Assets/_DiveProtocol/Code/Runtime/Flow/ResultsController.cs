using UnityEngine;
using UnityEngine.UI;

namespace DiveProtocol
{
    /// <summary>Presents the latest result and routes the next flow action.</summary>
    public sealed class ResultsController : MonoBehaviour
    {
        [SerializeField] private Text _resultsText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _returnToMainMenuButton;

        private bool _isNavigating;

        private void Start()
        {
            if (!AppRoot.TryGetInstance(out var appRoot))
            {
                Debug.LogWarning($"Results started without AppRoot. Start Play Mode from {SceneNames.Bootstrap}.");
                ShowUnavailableResult();
                SetButtonsInteractable(false);
                return;
            }

            var result = appRoot.RunManager.LastResult;
            if (result == null)
            {
                Debug.LogWarning("Results scene opened without a valid RunResult. No meta reward was applied.");
                ShowUnavailableResult();
                return;
            }

            if (result.EndReason == RunEndReason.Aborted)
            {
                Debug.LogWarning("ResultsController refused an Aborted RunResult. Aborted runs must return directly to Main Menu.");
                ShowUnavailableResult();
                return;
            }

            var outcome = appRoot.SaveManager.ApplyRunResult(result);
            var meta = appRoot.SaveManager.CurrentMeta;
            if (outcome.Status != RunResultApplyStatus.Applied &&
                outcome.Status != RunResultApplyStatus.AlreadyProcessed)
            {
                Debug.LogWarning($"RunResult settlement did not apply: {outcome.Status}.");
            }

            SetResultsText(
                $"End Reason: {result.EndReason}\n" +
                $"Total Score: {result.TotalScore}\n" +
                $"Currency Gained: {outcome.CurrencyGained}\n" +
                $"Settlement: {outcome.Status}\n" +
                $"Total Currency: {meta?.TotalCurrency ?? 0}\n" +
                $"Total Runs Settled: {meta?.TotalRunsSettled ?? 0}\n" +
                $"Successful Runs: {meta?.SuccessfulRuns ?? 0}\n" +
                $"Boss Kills: {meta?.BossKills ?? 0}");
        }

        /// <summary>Clears the old run, creates a new one, and reloads the configured starting level.</summary>
        public void Retry()
        {
            if (!TryBeginNavigation(out var appRoot))
            {
                return;
            }

            appRoot.RunManager.ClearRun();
            var startingLevelScene = appRoot.RunManager.StartingLevelSceneName;
            if (!appRoot.RunManager.StartNewRun() ||
                !appRoot.SceneLoader.LoadScene(startingLevelScene, GameState.InRun))
            {
                appRoot.RunManager.ClearRun();
                CancelNavigation();
            }
        }

        /// <summary>Clears run data and returns to the main menu.</summary>
        public void ReturnToMainMenu()
        {
            if (!TryBeginNavigation(out var appRoot))
            {
                return;
            }

            appRoot.RunManager.ClearRun();
            if (!appRoot.SceneLoader.LoadScene(SceneNames.MainMenu, GameState.MainMenu))
            {
                CancelNavigation();
            }
        }

        private bool TryBeginNavigation(out AppRoot appRoot)
        {
            appRoot = null;
            if (_isNavigating)
            {
                return false;
            }

            if (!AppRoot.TryGetInstance(out appRoot))
            {
                Debug.LogError("Cannot navigate because AppRoot is unavailable.");
                return false;
            }

            _isNavigating = true;
            SetButtonsInteractable(false);
            return true;
        }

        private void CancelNavigation()
        {
            _isNavigating = false;
            SetButtonsInteractable(true);
        }

        private void ShowUnavailableResult()
        {
            SetResultsText("No eligible RunResult is available.\nNo meta reward was applied.");
        }

        private void SetResultsText(string value)
        {
            if (_resultsText != null)
            {
                _resultsText.text = value;
            }
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            if (_retryButton != null)
            {
                _retryButton.interactable = isInteractable;
            }

            if (_returnToMainMenuButton != null)
            {
                _returnToMainMenuButton.interactable = isInteractable;
            }
        }
    }
}
