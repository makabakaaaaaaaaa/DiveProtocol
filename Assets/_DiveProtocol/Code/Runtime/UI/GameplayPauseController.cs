using DiveProtocol.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DiveProtocol.UI
{
    /// <summary>
    /// Controls the in-game pause menu for gameplay scenes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayPauseController : MonoBehaviour
    {
        private static GameplayPauseController _activeController;

        [Header("UI")]
        [SerializeField] private GameObject _pauseMenuRoot;
        [SerializeField] private GameObject _pauseBlocker;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private CanvasGroup _pauseMenuCanvasGroup;
        [SerializeField] private GameObject _gameplayHudRoot;

        [Header("Behaviour")]
        [SerializeField] private bool _allowEscapeToggle = true;
        [SerializeField] private bool _pauseAudio = true;

        private CursorLockMode _previousCursorLockState;
        private bool _previousCursorVisible;
        private float _previousTimeScale = 1f;
        private bool _previousAudioPaused;
        private PlayerDeathController _registeredDeathController;
        private bool _isPlayerDead;

        public bool IsPaused { get; private set; }

        /// <summary>
        /// Forces the active gameplay pause menu, if any, to resume and restores global pause side effects.
        /// </summary>
        public static void ForceResumeActivePause()
        {
            if (_activeController != null)
            {
                _activeController.ForceResume();
                return;
            }

            RestoreGlobalPauseSideEffects();
        }

        /// <summary>
        /// Restores time scale and audio pause state without touching any scene UI.
        /// </summary>
        public static void RestoreGlobalPauseSideEffects()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Awake()
        {
            InitializeClosedState();
        }

        private void OnEnable()
        {
            _activeController = this;
            PlayerInputReader.PauseRequested += HandlePauseRequested;
            InitializeClosedState();
        }

        private void OnDisable()
        {
            PlayerInputReader.PauseRequested -= HandlePauseRequested;
            UnregisterPlayerDeathController();
            ForceResume();

            if (_activeController == this)
            {
                _activeController = null;
            }
        }

        private void OnDestroy()
        {
            if (_activeController == this)
            {
                _activeController = null;
            }
        }

        /// <summary>
        /// Binds a runtime-spawned player so death can close the pause menu safely.
        /// </summary>
        public void RegisterPlayer(Transform player)
        {
            UnregisterPlayerDeathController();
            _isPlayerDead = false;

            if (player == null)
            {
                return;
            }

            _registeredDeathController = player.GetComponentInChildren<PlayerDeathController>();
            if (_registeredDeathController == null)
            {
                return;
            }

            _isPlayerDead = _registeredDeathController.IsDead;
            _registeredDeathController.PlayerDied += HandlePlayerDied;
            SetPauseButtonAvailable(!_isPlayerDead);
        }

        /// <summary>
        /// Shows the pause menu and freezes gameplay time/input.
        /// </summary>
        public void PauseGame()
        {
            if (IsPaused || _isPlayerDead)
            {
                return;
            }

            _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            _previousAudioPaused = AudioListener.pause;
            _previousCursorLockState = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            IsPaused = true;
            GameplayInputLock.Acquire(this);
            Time.timeScale = 0f;

            if (_pauseAudio)
            {
                AudioListener.pause = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPauseMenuVisible(true);
        }

        /// <summary>
        /// Hides the pause menu and restores gameplay time/input.
        /// </summary>
        public void ResumeGame()
        {
            if (!IsPaused)
            {
                SetPauseMenuVisible(false);
                return;
            }

            IsPaused = false;
            GameplayInputLock.Release(this);
            Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            AudioListener.pause = _pauseAudio ? _previousAudioPaused : AudioListener.pause;
            Cursor.lockState = _previousCursorLockState;
            Cursor.visible = _previousCursorVisible;
            SetPauseMenuVisible(false);
        }

        /// <summary>
        /// Opens the pause menu when running, or resumes when already paused.
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused)
            {
                ResumeGame();
                return;
            }

            PauseGame();
        }

        /// <summary>
        /// Resumes safely even when invoked from scene transitions, death, or teardown.
        /// </summary>
        public void ForceResume()
        {
            if (IsPaused)
            {
                ResumeGame();
            }

            GameplayInputLock.Release(this);
            SetPauseMenuVisible(false);
            RestoreGlobalPauseSideEffects();
        }

        private void InitializeClosedState()
        {
            IsPaused = false;
            GameplayInputLock.Release(this);
            SetPauseMenuVisible(false);
            SetPauseButtonAvailable(!_isPlayerDead);
            RestoreGlobalPauseSideEffects();
        }

        private void HandlePauseRequested()
        {
            if (_allowEscapeToggle && !_isPlayerDead)
            {
                TogglePause();
            }
        }

        private void HandlePlayerDied(PlayerDeathController controller)
        {
            _isPlayerDead = true;
            ForceResume();
            SetPauseButtonAvailable(false);
        }

        private void UnregisterPlayerDeathController()
        {
            if (_registeredDeathController != null)
            {
                _registeredDeathController.PlayerDied -= HandlePlayerDied;
                _registeredDeathController = null;
            }
        }

        private void SetPauseMenuVisible(bool isVisible)
        {
            if (_pauseBlocker != null)
            {
                _pauseBlocker.SetActive(isVisible);
            }

            if (_pauseMenuRoot != null)
            {
                _pauseMenuRoot.SetActive(isVisible);
            }

            if (_pauseMenuCanvasGroup != null)
            {
                _pauseMenuCanvasGroup.alpha = isVisible ? 1f : 0f;
                _pauseMenuCanvasGroup.interactable = isVisible;
                _pauseMenuCanvasGroup.blocksRaycasts = isVisible;
            }

            if (_gameplayHudRoot != null)
            {
                _gameplayHudRoot.SetActive(!isVisible);
            }
        }

        private void SetPauseButtonAvailable(bool isAvailable)
        {
            if (_pauseButton != null)
            {
                _pauseButton.interactable = isAvailable;
                _pauseButton.gameObject.SetActive(isAvailable);
            }
        }
    }
}
