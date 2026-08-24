using System.Collections;
using System;
using DiveProtocol.Builds;
using DiveProtocol.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DiveProtocol
{
    /// <summary>
    /// Handles player death side effects without owning death UI or restart flow decisions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDeathController : MonoBehaviour
    {
        [Header("Health")]
        [Tooltip("Health component that represents the player's runtime life.")]
        [SerializeField]
        private HealthComponent health;

        [Header("Disable On Death")]
        [Tooltip("Behaviours to disable when the player dies, such as movement, weapon, or interaction scripts.")]
        [SerializeField]
        private Behaviour[] behavioursToDisableOnDeath;

        [Tooltip("Objects to disable when the player dies.")]
        [SerializeField]
        private GameObject[] objectsToDisableOnDeath;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onPlayerDied;

        private bool _reloadRequested;

        /// <summary>
        /// Raised once after the player death flow starts.
        /// </summary>
        public event Action<PlayerDeathController> PlayerDied;

        public bool IsDead { get; private set; }

        private void Awake()
        {
            ResolveHealth();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        /// <summary>
        /// Reloads the active scene once.
        /// </summary>
        public void ReloadCurrentScene()
        {
            if (_reloadRequested)
            {
                return;
            }

            _reloadRequested = true;
            GameplayPauseController.ForceResumeActivePause();
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        /// <summary>
        /// Reloads the active scene after a scaled-time delay.
        /// </summary>
        public void ReloadCurrentSceneAfterDelay(float delaySeconds)
        {
            if (_reloadRequested)
            {
                return;
            }

            StartCoroutine(ReloadCurrentSceneRoutine(Mathf.Max(0f, delaySeconds)));
        }

        private IEnumerator ReloadCurrentSceneRoutine(float delaySeconds)
        {
            _reloadRequested = true;

            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            GameplayPauseController.ForceResumeActivePause();
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void HandleDied(HealthComponent deadHealth)
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            if (AppRoot.TryGetInstance(out AppRoot appRoot))
            {
                appRoot.RunManager.ClearCurrentRunBuilds();
            }

            GetComponent<PlayerBuildController>()?.ClearUpgrades();
            GameplayPauseController.ForceResumeActivePause();
            DisableConfiguredBehaviours();
            DisableConfiguredObjects();
            PlayerDied?.Invoke(this);
            onPlayerDied?.Invoke();
        }

        private void DisableConfiguredBehaviours()
        {
            if (behavioursToDisableOnDeath == null)
            {
                return;
            }

            for (int i = 0; i < behavioursToDisableOnDeath.Length; i++)
            {
                Behaviour target = behavioursToDisableOnDeath[i];
                if (target != null)
                {
                    target.enabled = false;
                }
            }
        }

        private void DisableConfiguredObjects()
        {
            if (objectsToDisableOnDeath == null)
            {
                return;
            }

            for (int i = 0; i < objectsToDisableOnDeath.Length; i++)
            {
                GameObject target = objectsToDisableOnDeath[i];
                if (target != null)
                {
                    target.SetActive(false);
                }
            }
        }

        private void ResolveHealth()
        {
            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }
        }
    }
}
