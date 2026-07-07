using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DiveProtocol.Doors
{
    /// <summary>
    /// Shared helper for keeping runtime door blockers visible to NavMeshAgent pathing.
    /// </summary>
    internal static class DoorNavigationObstacleUtility
    {
        public static int EnsureBoxObstacles(
            Transform leafRoot,
            bool addMissing,
            bool configureFromCollider,
            List<NavMeshObstacle> results = null)
        {
            if (leafRoot == null)
            {
                return 0;
            }

            BoxCollider[] boxColliders = leafRoot.GetComponentsInChildren<BoxCollider>(true);
            int configuredCount = 0;

            for (int i = 0; i < boxColliders.Length; i++)
            {
                BoxCollider boxCollider = boxColliders[i];
                if (boxCollider == null || boxCollider.isTrigger)
                {
                    continue;
                }

                NavMeshObstacle obstacle = boxCollider.GetComponent<NavMeshObstacle>();
                if (obstacle == null)
                {
                    if (!addMissing)
                    {
                        continue;
                    }

                    obstacle = boxCollider.gameObject.AddComponent<NavMeshObstacle>();
                }

                if (configureFromCollider)
                {
                    ConfigureFromBoxCollider(obstacle, boxCollider);
                }

                results?.Add(obstacle);
                configuredCount++;
            }

            return configuredCount;
        }

        public static void SetEnabled(Transform leafRoot, bool enabled)
        {
            if (leafRoot == null)
            {
                return;
            }

            NavMeshObstacle[] obstacles = leafRoot.GetComponentsInChildren<NavMeshObstacle>(true);
            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] != null)
                {
                    obstacles[i].enabled = enabled;
                }
            }
        }

        private static void ConfigureFromBoxCollider(
            NavMeshObstacle obstacle,
            BoxCollider boxCollider)
        {
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = boxCollider.center;
            obstacle.size = boxCollider.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            obstacle.carvingMoveThreshold = 0.05f;
            obstacle.carvingTimeToStationary = 0.2f;
            obstacle.enabled = boxCollider.enabled;
        }
    }
}
