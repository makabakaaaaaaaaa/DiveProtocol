using System;
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Owns controlled transitions between high-level application states.</summary>
    public sealed class GameStateMachine : MonoBehaviour
    {
        public event Action<GameState, GameState> StateChanged;

        public GameState CurrentState { get; private set; } = GameState.Boot;

        /// <summary>Attempts a transition and reports whether it was accepted.</summary>
        public bool TryTransition(GameState nextState)
        {
            if (nextState == CurrentState)
            {
                Debug.LogWarning($"Ignored duplicate game-state transition to {nextState}.");
                return false;
            }

            if (!IsTransitionAllowed(CurrentState, nextState))
            {
                Debug.LogError($"Illegal game-state transition: {CurrentState} -> {nextState}.");
                return false;
            }

            var previousState = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previousState, nextState);
            return true;
        }

        private static bool IsTransitionAllowed(GameState currentState, GameState nextState)
        {
            if (nextState == GameState.Loading)
            {
                return currentState != GameState.Loading;
            }

            if (currentState == GameState.Loading)
            {
                return nextState != GameState.Loading;
            }

            return currentState == GameState.Boot && nextState == GameState.MainMenu;
        }
    }
}
