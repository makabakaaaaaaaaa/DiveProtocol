using UnityEngine;

namespace DiveProtocol
{
    /// <summary>
    /// Relays a runtime-spawned player target to manually assigned scene enemies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneEnemyTargetRelay : MonoBehaviour
    {
        [SerializeField]
        private EnemyPatrolChaseController[] sceneEnemies;

        [SerializeField]
        private EnemyWaveSpawner[] waveSpawners;

        /// <summary>
        /// Assigns the runtime player target to configured scene enemies and wave spawners.
        /// </summary>
        public void SetPlayerTarget(Transform target)
        {
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
    }
}
