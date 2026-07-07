using UnityEngine;
using UnityEngine.UI;

namespace DiveProtocol
{
    /// <summary>Handles the temporary main-menu actions.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _newRunButton;
        [SerializeField] private Button _quitButton;

        private void Start()
        {
            if (!AppRoot.TryGetInstance(out _))
            {
                Debug.LogWarning($"Main Menu started without AppRoot. Start Play Mode from {SceneNames.Bootstrap}.");
                SetButtonsInteractable(false);
            }
        }

        /// <summary>Creates a run and enters the configured starting level.</summary>
        public void NewRun()
        {
            if (!AppRoot.TryGetInstance(out var appRoot))
            {
                Debug.LogError("Cannot start a run because AppRoot is unavailable.");
                return;
            }

            if (!appRoot.RunManager.StartNewRun())
            {
                return;
            }

            SetButtonsInteractable(false);
            var startingLevelScene = appRoot.RunManager.StartingLevelSceneName;
            if (!appRoot.SceneLoader.LoadScene(startingLevelScene, GameState.InRun))
            {
                appRoot.RunManager.ClearRun();
                SetButtonsInteractable(true);
            }
        }

        /// <summary>Quits the player or stops Play Mode in the Unity Editor.</summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            if (_newRunButton != null)
            {
                _newRunButton.interactable = isInteractable;
            }

            if (_quitButton != null)
            {
                _quitButton.interactable = isInteractable;
            }
        }
    }
}
