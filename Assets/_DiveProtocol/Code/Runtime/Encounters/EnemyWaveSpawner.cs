using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DiveProtocol
{
    /// <summary>
    /// Runtime wave spawner that can be connected to survival-door start and completion events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyWaveSpawner : MonoBehaviour
    {
        [Header("Spawn")]
        [Tooltip("Enemy prefab spawned by this wave spawner.")]
        [SerializeField]
        private GameObject enemyPrefab;

        [Tooltip("Candidate spawn points. Null entries are skipped safely.")]
        [SerializeField]
        private Transform[] spawnPoints;

        [Tooltip("Target assigned to supported enemy chase and attack components.")]
        [SerializeField]
        private Transform playerTarget;

        [SerializeField, Min(0.1f)]
        private float spawnIntervalSeconds = 5f;

        [SerializeField, Min(1)]
        private int maxAliveEnemies = 6;

        [SerializeField]
        private bool spawnImmediatelyOnBegin = true;

        [SerializeField, Min(1)]
        private int initialSpawnCount = 1;

        [SerializeField]
        private bool fillToMaxAliveOnBegin;

        [SerializeField]
        private bool randomizeSpawnPoint = true;

        [Tooltip("Optional parent for spawned enemies.")]
        [SerializeField]
        private Transform spawnedEnemiesParent;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onSpawningStarted;

        [SerializeField]
        private UnityEvent onSpawningStopped;

        private readonly List<SpawnedEnemyRecord> _spawnedEnemies = new();
        private int _nextSpawnPointIndex;
        private float _nextSpawnTime;
        private bool _loggedMissingPrefab;
        private bool _loggedMissingSpawnPoints;
        private bool _loggedMissingPlayerTarget;

        public bool IsSpawning { get; private set; }

        public int AliveEnemyCount
        {
            get
            {
                PruneMissingSpawnedEnemies();
                return _spawnedEnemies.Count;
            }
        }

        private void Update()
        {
            PruneMissingSpawnedEnemies();

            if (!IsSpawning || Time.time < _nextSpawnTime)
            {
                return;
            }

            SpawnOne();
            _nextSpawnTime = Time.time + spawnIntervalSeconds;
        }

        private void OnDisable()
        {
            StopSpawning();
        }

        private void OnDestroy()
        {
            CleanupAllSubscriptions();
        }

        /// <summary>
        /// Starts spawning enemies at the configured interval.
        /// </summary>
        public bool BeginSpawning()
        {
            if (IsSpawning)
            {
                return false;
            }

            if (!CanSpawn())
            {
                return false;
            }

            IsSpawning = true;
            onSpawningStarted?.Invoke();

            if (spawnImmediatelyOnBegin)
            {
                SpawnInitialWave();
                _nextSpawnTime = Time.time + spawnIntervalSeconds;
            }
            else
            {
                _nextSpawnTime = Time.time + spawnIntervalSeconds;
            }

            return true;
        }

        /// <summary>
        /// Stops spawning new enemies without destroying enemies that are already alive.
        /// </summary>
        public bool StopSpawning()
        {
            if (!IsSpawning)
            {
                return false;
            }

            IsSpawning = false;
            onSpawningStopped?.Invoke();
            return true;
        }

        /// <summary>
        /// UnityEvent Inspector wrapper for BeginSpawning.
        /// </summary>
        public void BeginSpawningFromUnityEvent()
        {
            BeginSpawning();
        }

        /// <summary>
        /// UnityEvent Inspector wrapper for StopSpawning.
        /// </summary>
        public void StopSpawningFromUnityEvent()
        {
            StopSpawning();
        }

        /// <summary>
        /// Spawns one enemy if the spawner is configured and below the alive limit.
        /// </summary>
        public bool SpawnOne()
        {
            PruneMissingSpawnedEnemies();

            if (_spawnedEnemies.Count >= maxAliveEnemies || !CanSpawn())
            {
                return false;
            }

            Transform spawnPoint = SelectSpawnPoint();
            if (spawnPoint == null)
            {
                LogMissingSpawnPointsOnce();
                return false;
            }

            GameObject enemy = Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnedEnemiesParent);

            AssignEnemyTarget(enemy);
            TrackSpawnedEnemy(enemy);
            return true;
        }

        /// <summary>
        /// Spawns the configured initial wave without exceeding Max Alive.
        /// </summary>
        public int SpawnInitialWave()
        {
            PruneMissingSpawnedEnemies();

            int targetAliveCount = fillToMaxAliveOnBegin
                ? maxAliveEnemies
                : Mathf.Min(maxAliveEnemies, initialSpawnCount);

            int spawnedCount = 0;
            while (_spawnedEnemies.Count < targetAliveCount)
            {
                if (!SpawnOne())
                {
                    break;
                }

                spawnedCount++;
            }

            return spawnedCount;
        }

        /// <summary>
        /// Stops spawning and destroys all enemies created by this spawner.
        /// </summary>
        public void DespawnAll()
        {
            StopSpawning();

            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                SpawnedEnemyRecord record = _spawnedEnemies[i];
                Unsubscribe(record);

                if (record.Enemy != null)
                {
                    Destroy(record.Enemy);
                }
            }

            _spawnedEnemies.Clear();
        }

        /// <summary>
        /// Sets the target assigned to future spawned enemies.
        /// </summary>
        public void SetPlayerTarget(Transform target)
        {
            playerTarget = target;
        }

        private bool CanSpawn()
        {
            bool valid = true;

            if (enemyPrefab == null)
            {
                LogMissingPrefabOnce();
                valid = false;
            }

            if (!HasAnySpawnPoint())
            {
                LogMissingSpawnPointsOnce();
                valid = false;
            }

            if (playerTarget == null)
            {
                LogMissingPlayerTargetOnce();
                valid = false;
            }

            return valid;
        }

        private Transform SelectSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            if (randomizeSpawnPoint)
            {
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    int index = UnityEngine.Random.Range(0, spawnPoints.Length);
                    if (spawnPoints[index] != null)
                    {
                        return spawnPoints[index];
                    }
                }

                return FindFirstValidSpawnPoint();
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                int index = _nextSpawnPointIndex % spawnPoints.Length;
                _nextSpawnPointIndex++;

                if (spawnPoints[index] != null)
                {
                    return spawnPoints[index];
                }
            }

            return null;
        }

        private Transform FindFirstValidSpawnPoint()
        {
            if (spawnPoints == null)
            {
                return null;
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    return spawnPoints[i];
                }
            }

            return null;
        }

        private bool HasAnySpawnPoint()
        {
            return FindFirstValidSpawnPoint() != null;
        }

        private void AssignEnemyTarget(GameObject enemy)
        {
            if (enemy == null || playerTarget == null)
            {
                return;
            }

            EnemyChaseController chaseController =
                enemy.GetComponentInChildren<EnemyChaseController>();
            if (chaseController != null)
            {
                chaseController.SetTarget(playerTarget);
            }

            EnemyContactAttack contactAttack =
                enemy.GetComponentInChildren<EnemyContactAttack>();
            if (contactAttack != null)
            {
                contactAttack.SetTarget(playerTarget);
            }
        }

        private void TrackSpawnedEnemy(GameObject enemy)
        {
            HealthComponent health =
                enemy != null ? enemy.GetComponentInChildren<HealthComponent>() : null;

            var record = new SpawnedEnemyRecord(enemy, health);
            _spawnedEnemies.Add(record);

            if (health != null)
            {
                health.Died += HandleSpawnedEnemyDied;
            }
        }

        private void HandleSpawnedEnemyDied(HealthComponent health)
        {
            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                SpawnedEnemyRecord record = _spawnedEnemies[i];
                if (record.Health != health)
                {
                    continue;
                }

                Unsubscribe(record);
                _spawnedEnemies.RemoveAt(i);
                return;
            }
        }

        private void PruneMissingSpawnedEnemies()
        {
            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                SpawnedEnemyRecord record = _spawnedEnemies[i];
                if (record.Enemy != null &&
                    (record.Health == null || record.Health.IsAlive))
                {
                    continue;
                }

                Unsubscribe(record);
                _spawnedEnemies.RemoveAt(i);
            }
        }

        private void CleanupAllSubscriptions()
        {
            for (int i = 0; i < _spawnedEnemies.Count; i++)
            {
                Unsubscribe(_spawnedEnemies[i]);
            }
        }

        private void Unsubscribe(SpawnedEnemyRecord record)
        {
            if (record.Health != null)
            {
                record.Health.Died -= HandleSpawnedEnemyDied;
            }
        }

        private void LogMissingPrefabOnce()
        {
            if (_loggedMissingPrefab)
            {
                return;
            }

            _loggedMissingPrefab = true;
            Debug.LogError($"[Encounter] {nameof(EnemyWaveSpawner)} on '{name}' requires an enemy prefab.", this);
        }

        private void LogMissingSpawnPointsOnce()
        {
            if (_loggedMissingSpawnPoints)
            {
                return;
            }

            _loggedMissingSpawnPoints = true;
            Debug.LogError($"[Encounter] {nameof(EnemyWaveSpawner)} on '{name}' requires at least one spawn point.", this);
        }

        private void LogMissingPlayerTargetOnce()
        {
            if (_loggedMissingPlayerTarget)
            {
                return;
            }

            _loggedMissingPlayerTarget = true;
            Debug.LogError($"[Encounter] {nameof(EnemyWaveSpawner)} on '{name}' requires a player target.", this);
        }

        private sealed class SpawnedEnemyRecord
        {
            public SpawnedEnemyRecord(GameObject enemy, HealthComponent health)
            {
                Enemy = enemy;
                Health = health;
            }

            public GameObject Enemy { get; }
            public HealthComponent Health { get; }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            spawnIntervalSeconds = Mathf.Max(0.1f, spawnIntervalSeconds);
            maxAliveEnemies = Mathf.Max(1, maxAliveEnemies);
            initialSpawnCount = Mathf.Max(1, initialSpawnCount);
        }
#endif
    }
}
