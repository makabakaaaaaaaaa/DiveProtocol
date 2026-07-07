using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiveProtocol
{
    /// <summary>Owns the Gameplay action map and exposes normalized player intent.</summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap _gameplayMap;
        private InputAction _moveAction;
        private InputAction _pauseAction;
        private GameStateMachine _stateMachine;
        private bool _isPaused;

        /// <summary>
        /// Raised when the Gameplay Pause action is performed.
        /// </summary>
        public static event Action PauseRequested;

        public event Action<bool> PauseStateChanged;

        public bool IsPaused => _isPaused;
        public bool CanMove => _gameplayMap != null && _gameplayMap.enabled && !_isPaused;
        public InputActionMap GameplayMap
        {
            get
            {
                EnsureGameplayActions();
                return _gameplayMap;
            }
        }

        private void Awake()
        {
            EnsureGameplayActions();
        }

        private void OnEnable()
        {
            EnsureGameplayActions();
            _pauseAction.performed += HandlePause;

            if (!AppRoot.TryGetInstance(out var appRoot))
            {
                Debug.LogWarning("[Input] PlayerInputReader requires AppRoot. Start from SCN_Bootstrap.");
                _gameplayMap.Disable();
                return;
            }

            _stateMachine = appRoot.GameStateMachine;
            _stateMachine.StateChanged += HandleGameStateChanged;
            ApplyGameState(_stateMachine.CurrentState);
        }

        private void OnDisable()
        {
            if (_pauseAction != null) _pauseAction.performed -= HandlePause;
            if (_stateMachine != null) _stateMachine.StateChanged -= HandleGameStateChanged;
            _stateMachine = null;
            _gameplayMap?.Disable();
            SetPaused(false);
        }

        private void OnDestroy()
        {
            _gameplayMap?.Dispose();
        }

        public Vector2 ReadMoveInput()
        {
            return CanMove
                ? PlayerMovementMath.NormalizeMoveInput(_moveAction.ReadValue<Vector2>())
                : Vector2.zero;
        }

        private void EnsureGameplayActions()
        {
            if (_gameplayMap != null)
            {
                return;
            }

            _gameplayMap = new InputActionMap("Gameplay");
            _moveAction = _gameplayMap.AddAction("Move", InputActionType.Value);
            _moveAction.expectedControlType = "Vector2";
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _pauseAction = _gameplayMap.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        }

        private void HandlePause(InputAction.CallbackContext context)
        {
            if (_stateMachine != null && _stateMachine.CurrentState == GameState.InRun)
            {
                PauseRequested?.Invoke();
            }
        }

        private void HandleGameStateChanged(GameState previousState, GameState nextState)
        {
            ApplyGameState(nextState);
        }

        private void ApplyGameState(GameState state)
        {
            if (state == GameState.InRun)
            {
                _gameplayMap.Enable();
                return;
            }

            SetPaused(false);
            _gameplayMap.Disable();
        }

        private void SetPaused(bool isPaused)
        {
            if (_isPaused == isPaused)
            {
                return;
            }

            _isPaused = isPaused;
            PauseStateChanged?.Invoke(_isPaused);
        }
    }
}
