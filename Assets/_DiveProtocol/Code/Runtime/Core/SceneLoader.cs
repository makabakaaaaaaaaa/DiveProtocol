using System;
using System.Collections;
using DiveProtocol.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiveProtocol
{
    /// <summary>Serializes asynchronous scene loads and coordinates loading state.</summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private GameStateMachine _stateMachine;
        private bool _isLoading;

        public bool IsLoading => _isLoading;

        internal void Initialize(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        /// <summary>Starts loading a build scene if no other load is active.</summary>
        public bool LoadScene(string sceneName, GameState targetState)
        {
            if (_stateMachine == null)
            {
                Debug.LogError("SceneLoader is not initialized with a GameStateMachine.");
                return false;
            }

            if (_isLoading)
            {
                Debug.LogWarning($"Cannot load {sceneName}; another scene load is already in progress.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is unavailable. Create it and add it to Build Settings first.");
                return false;
            }

            var previousState = _stateMachine.CurrentState;
            if (!_stateMachine.TryTransition(GameState.Loading))
            {
                return false;
            }

            GameplayPauseController.ForceResumeActivePause();
            _isLoading = true;
            Debug.Log($"[Scene] Loading {sceneName} with Single mode");
            StartCoroutine(LoadSceneRoutine(sceneName, targetState, previousState));
            return true;
        }

        private IEnumerator LoadSceneRoutine(string sceneName, GameState targetState, GameState fallbackState)
        {
            AsyncOperation operation;

            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                _isLoading = false;
                _stateMachine.TryTransition(fallbackState);
                Debug.LogException(exception);
                yield break;
            }

            if (operation == null)
            {
                _isLoading = false;
                _stateMachine.TryTransition(fallbackState);
                Debug.LogError($"Unity did not create an async load operation for scene '{sceneName}'.");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            _isLoading = false;
            var activeScene = SceneManager.GetActiveScene();
            Debug.Log($"[Scene] Loaded {sceneName}");
            Debug.Log($"[Scene] Active scene: {activeScene.name}");

            if (activeScene.name != sceneName)
            {
                Debug.LogError($"[Scene] Expected active scene '{sceneName}', but Unity reports '{activeScene.name}'.");
            }

            if (SceneManager.sceneCount != 1)
            {
                Debug.LogWarning($"[Scene] Expected one loaded content scene after a Single load, but found {SceneManager.sceneCount}.");
            }

            if (!_stateMachine.TryTransition(targetState))
            {
                Debug.LogError($"Scene '{sceneName}' loaded, but transition to {targetState} failed.");
            }
        }
    }
}
