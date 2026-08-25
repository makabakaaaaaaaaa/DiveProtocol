using System.Collections.Generic;
using DiveProtocol.Bosses;
using DiveProtocol.Doors;
using DiveProtocol.Enemies;
using DiveProtocol.Enemies.CorpseReanimation;
using DiveProtocol.Encounters;
using DiveProtocol.Interaction;
using UnityEngine;
using UnityEngine.AI;

namespace DiveProtocol.RoomVisibility
{
    public static class RoomVisibilityUtility
    {
        private const float SatEpsilon = 0.0001f;

        public static bool IntersectsBounds(RoomVolume volume, Bounds bounds)
        {
            BoxCollider box = volume != null ? volume.BoxCollider : null;
            if (box == null) return false;

            Vector3 a = bounds.extents;
            Vector3 b = Vector3.Scale(box.size * 0.5f, Abs(box.transform.lossyScale));
            Vector3[] axes =
            {
                box.transform.right.normalized,
                box.transform.up.normalized,
                box.transform.forward.normalized
            };
            Vector3 centerDelta = box.transform.TransformPoint(box.center) - bounds.center;
            float[,] rotation = new float[3, 3];
            float[,] absoluteRotation = new float[3, 3];
            Vector3 t = new Vector3(centerDelta.x, centerDelta.y, centerDelta.z);

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    rotation[row, column] = axes[column][row];
                    absoluteRotation[row, column] = Mathf.Abs(rotation[row, column]) + SatEpsilon;
                }
            }

            for (int axis = 0; axis < 3; axis++)
            {
                float radiusA = a[axis];
                float radiusB = b.x * absoluteRotation[axis, 0] + b.y * absoluteRotation[axis, 1] + b.z * absoluteRotation[axis, 2];
                if (Mathf.Abs(t[axis]) > radiusA + radiusB) return false;
            }

            for (int axis = 0; axis < 3; axis++)
            {
                float radiusA = a.x * absoluteRotation[0, axis] + a.y * absoluteRotation[1, axis] + a.z * absoluteRotation[2, axis];
                float radiusB = b[axis];
                float projection = Mathf.Abs(t.x * rotation[0, axis] + t.y * rotation[1, axis] + t.z * rotation[2, axis]);
                if (projection > radiusA + radiusB) return false;
            }

            for (int left = 0; left < 3; left++)
            {
                int leftNext = (left + 1) % 3;
                int leftPrevious = (left + 2) % 3;
                for (int right = 0; right < 3; right++)
                {
                    int rightNext = (right + 1) % 3;
                    int rightPrevious = (right + 2) % 3;
                    float radiusA = a[leftNext] * absoluteRotation[leftPrevious, right] + a[leftPrevious] * absoluteRotation[leftNext, right];
                    float radiusB = b[rightNext] * absoluteRotation[left, rightPrevious] + b[rightPrevious] * absoluteRotation[left, rightNext];
                    float projection = Mathf.Abs(t[leftPrevious] * rotation[leftNext, right] - t[leftNext] * rotation[leftPrevious, right]);
                    if (projection > radiusA + radiusB) return false;
                }
            }

            return true;
        }

        public static float GetMembershipScore(RoomVolume volume, Bounds bounds)
        {
            int containedCorners = GetContainedCornerCount(volume, bounds);

            Bounds volumeBounds = volume.BoxCollider.bounds;
            Vector3 min = Vector3.Max(bounds.min, volumeBounds.min);
            Vector3 max = Vector3.Min(bounds.max, volumeBounds.max);
            Vector3 overlap = Vector3.Max(Vector3.zero, max - min);
            float overlapVolume = overlap.x * overlap.y * overlap.z;
            float proximity = 1f / (0.01f + Vector3.Distance(bounds.center, volume.BoxCollider.ClosestPoint(bounds.center)));
            return containedCorners * 100000f + overlapVolume + proximity * 0.001f;
        }

        public static int GetContainedCornerCount(RoomVolume volume, Bounds bounds)
        {
            int containedCorners = 0;
            foreach (Vector3 corner in GetCorners(bounds))
            {
                if (volume.ContainsWorldPoint(corner)) containedCorners++;
            }

            return containedCorners;
        }

        /// <summary>Returns the gameplay root for an enemy or boss visual without relying on object names.</summary>
        public static bool TryGetEnemyOrBossRoot(Component component, out Transform root)
        {
            root = null;
            if (component == null) return false;

            Transform transform = component.transform;
            StationaryHalfBuriedBossController boss = transform.GetComponentInParent<StationaryHalfBuriedBossController>();
            if (boss != null)
            {
                root = boss.transform;
                return true;
            }

            EnemyChaseController chase = transform.GetComponentInParent<EnemyChaseController>();
            if (chase != null)
            {
                root = chase.transform;
                return true;
            }

            EnemyPatrolChaseController patrol = transform.GetComponentInParent<EnemyPatrolChaseController>();
            if (patrol != null)
            {
                root = patrol.transform;
                return true;
            }

            EnemyContactAttack attack = transform.GetComponentInParent<EnemyContactAttack>();
            if (attack != null)
            {
                root = attack.transform;
                return true;
            }

            EnemyAnimatorBridge animator = transform.GetComponentInParent<EnemyAnimatorBridge>();
            if (animator != null)
            {
                root = animator.transform;
                return true;
            }

            ReanimatingCorpseEnemy corpse = transform.GetComponentInParent<ReanimatingCorpseEnemy>();
            if (corpse != null)
            {
                root = corpse.transform;
                return true;
            }

            return false;
        }

        public static bool IsEnemyOrBossVisual(Component component)
        {
            return TryGetEnemyOrBossRoot(component, out _);
        }

        public static bool IsGameplayObject(Component component)
        {
            if (component == null) return true;
            Transform transform = component.transform;
            return transform.GetComponentInParent<PlayerMovement>() != null ||
                   transform.GetComponentInParent<PlayerInteractor>() != null ||
                   transform.GetComponentInParent<DoorController>() != null ||
                   transform.GetComponentInParent<DoorInteractable>() != null ||
                   transform.GetComponentInParent<AutomaticSlidingDoor>() != null ||
                   transform.GetComponentInParent<InteractableBase>() != null ||
                   transform.GetComponentInParent<PlayerSpawnPoint>() != null ||
                   transform.GetComponentInParent<EnemyWaveSpawner>() != null ||
                   transform.GetComponentInParent<LevelGameplayMarker>() != null ||
                   transform.GetComponentInParent<RoomVolume>() != null ||
                   transform.GetComponentInParent<RoomVisibilitySceneData>() != null ||
                   transform.GetComponentInParent<Camera>() != null ||
                   transform.GetComponentInParent<Canvas>() != null ||
                   transform.GetComponentInParent<NavMeshAgent>() != null ||
                   component.CompareTag("Player") || component.CompareTag("MainCamera");
        }

        public static bool IsRoomLocalLight(Light light)
        {
            return light != null &&
                   !IsGameplayObject(light) &&
                   !IsEnemyOrBossVisual(light) &&
                   (light.type == LightType.Point || light.type == LightType.Spot);
        }

        public static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return string.Empty;
            var names = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
            }

            return string.Join("/", names);
        }

        private static IEnumerable<Vector3> GetCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            yield return new Vector3(min.x, min.y, min.z);
            yield return new Vector3(min.x, min.y, max.z);
            yield return new Vector3(min.x, max.y, min.z);
            yield return new Vector3(min.x, max.y, max.z);
            yield return new Vector3(max.x, min.y, min.z);
            yield return new Vector3(max.x, min.y, max.z);
            yield return new Vector3(max.x, max.y, min.z);
            yield return new Vector3(max.x, max.y, max.z);
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
