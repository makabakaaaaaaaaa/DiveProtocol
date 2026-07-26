using System;
using System.Linq;
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>
    /// Relays a runtime-spawned player target to manually assigned or auto-discovered scene enemies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneEnemyTargetRelay : MonoBehaviour
    {
        [SerializeField]
        private EnemyPatrolChaseController[] sceneEnemies;

        [SerializeField]
        private EnemyChaseController[] sceneChaseEnemies;

        [SerializeField]
        private EnemyWaveSpawner[] waveSpawners;

        [SerializeField]
        private bool autoDiscoverSceneTargets = true;

        private PlayerSpawner _playerSpawner;
        private Transform _currentTarget;

        private void OnEnable()
        {
            RefreshSceneTargetsIfNeeded();
            SubscribeToPlayerSpawner();

            if (_playerSpawner != null && _playerSpawner.SpawnedPlayer != null)
            {
                SetPlayerTarget(_playerSpawner.SpawnedPlayer);
            }
        }

        private void OnDisable()
        {
            if (_playerSpawner != null)
            {
                _playerSpawner.PlayerSpawned -= SetPlayerTarget;
            }
        }

        /// <summary>
        /// Assigns the runtime player target to configured scene enemies and wave spawners.
        /// </summary>
        public void SetPlayerTarget(Transform target)
        {
            _currentTarget = target;
            RefreshSceneTargetsIfNeeded();

            if (sceneEnemies != null)
            {
                for (int i = 0; i < sceneEnemies.Length; i++)
                {
                    EnemyPatrolChaseController enemy = sceneEnemies[i];
                    if (enemy != null)
                    {
                        enemy.SetPlayerTarget(target);
                    }
                }
            }

            if (sceneChaseEnemies != null)
            {
                for (int i = 0; i < sceneChaseEnemies.Length; i++)
                {
                    EnemyChaseController enemy = sceneChaseEnemies[i];
                    if (enemy != null)
                    {
                        enemy.SetTarget(target);

                        EnemyContactAttack contactAttack = enemy.GetComponent<EnemyContactAttack>();
                        if (contactAttack != null)
                        {
                            contactAttack.SetTarget(target);
                        }
                    }
                }
            }

            if (waveSpawners == null)
            {
                return;
            }

            for (int i = 0; i < waveSpawners.Length; i++)
            {
                EnemyWaveSpawner spawner = waveSpawners[i];
                if (spawner != null)
                {
                    spawner.SetPlayerTarget(target);
                }
            }
        }

        /// <summary>
        /// Clears the runtime player target from every configured scene enemy.
        /// </summary>
        public void ClearPlayerTarget()
        {
            _currentTarget = null;
            RefreshSceneTargetsIfNeeded();

            if (sceneEnemies != null)
            {
                for (int i = 0; i < sceneEnemies.Length; i++)
                {
                    EnemyPatrolChaseController enemy = sceneEnemies[i];
                    if (enemy != null)
                    {
                        enemy.ClearPlayerTarget();
                    }
                }
            }

            if (sceneChaseEnemies != null)
            {
                for (int i = 0; i < sceneChaseEnemies.Length; i++)
                {
                    EnemyChaseController enemy = sceneChaseEnemies[i];
                    if (enemy != null)
                    {
                        enemy.ClearTarget();

                        EnemyContactAttack contactAttack = enemy.GetComponent<EnemyContactAttack>();
                        if (contactAttack != null)
                        {
                            contactAttack.ClearTarget();
                        }
                    }
                }
            }

            if (waveSpawners == null)
            {
                return;
            }

            for (int i = 0; i < waveSpawners.Length; i++)
            {
                EnemyWaveSpawner spawner = waveSpawners[i];
                if (spawner != null)
                {
                    spawner.SetPlayerTarget(null);
                }
            }
        }

        /// <summary>
        /// Rebuilds auto-discovered enemy and spawner lists for the current scene.
        /// </summary>
        public void RefreshSceneTargets()
        {
            sceneEnemies = FindObjectsByType<EnemyPatrolChaseController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None)
                .Where(ShouldRelayToEnemy)
                .ToArray();

            sceneChaseEnemies = FindObjectsByType<EnemyChaseController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None)
                .Where(ShouldRelayToEnemy)
                .ToArray();

            waveSpawners = FindObjectsByType<EnemyWaveSpawner>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
        }

        private void RefreshSceneTargetsIfNeeded()
        {
            if (!autoDiscoverSceneTargets)
            {
                return;
            }

            RefreshSceneTargets();
        }

        private void SubscribeToPlayerSpawner()
        {
            PlayerSpawner playerSpawner = FindFirstObjectByType<PlayerSpawner>();
            if (_playerSpawner == playerSpawner)
            {
                return;
            }

            if (_playerSpawner != null)
            {
                _playerSpawner.PlayerSpawned -= SetPlayerTarget;
            }

            _playerSpawner = playerSpawner;
            if (_playerSpawner != null)
            {
                _playerSpawner.PlayerSpawned += SetPlayerTarget;
            }
        }

        private static bool ShouldRelayToEnemy(Component enemyComponent)
        {
            if (enemyComponent == null)
            {
                return false;
            }

            Transform current = enemyComponent.transform;
            while (current != null)
            {
                string name = current.name;
                if (name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Corpse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Reanimating", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }

                current = current.parent;
            }

            MonoBehaviour[] behaviours = enemyComponent.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("ReanimatingCorpse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("CorpseReanimation", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
