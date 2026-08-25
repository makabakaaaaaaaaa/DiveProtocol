using System;
using DiveProtocol.Builds;
using UnityEngine;
using UnityEngine.Events;

namespace DiveProtocol
{
    /// <summary>Creates or positions the basic player once when a run level starts.</summary>
    public sealed class PlayerSpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class TransformEvent : UnityEvent<Transform>
        {
        }

        [SerializeField] private PlayerMovement _playerPrefab;
        [SerializeField] private PlayerSpawnPoint _spawnPoint;
        [SerializeField] private TransformEvent _onPlayerSpawned;

        private const float GroundRaycastStartHeight = 1f;
        private const float GroundRaycastDistance = 6f;
        private const float MinimumGroundClearance = 0.03f;
        private const float MaximumGroundClearance = 0.1f;

        public Transform SpawnedPlayer { get; private set; }

        /// <summary>
        /// Raised after the player instance has been created or positioned at the spawn point.
        /// </summary>
        public event Action<Transform> PlayerSpawned;

        private void Start()
        {
            if (_playerPrefab == null || _spawnPoint == null)
            {
                Debug.LogError("[Player] PlayerSpawner requires a player prefab and PlayerSpawnPoint.");
                return;
            }

            if (!AppRoot.TryGetInstance(out var appRoot) ||
                appRoot.RunManager.CurrentRun == null ||
                !appRoot.RunManager.CurrentRun.IsActive)
            {
                Debug.LogWarning("[Player] Player was not spawned because there is no active run.");
                return;
            }

            var player = FindFirstObjectByType<PlayerMovement>();
            if (player == null)
            {
                player = Instantiate(_playerPrefab, _spawnPoint.transform.position, _spawnPoint.transform.rotation);
                player.gameObject.name = _playerPrefab.gameObject.name;
            }

            PlacePlayerAtSpawnSafely(player);

            player.SetMovementCamera(Camera.main);
            SpawnedPlayer = player.transform;
            BuildRunBridge.EnsureAndSync(SpawnedPlayer, appRoot.RunManager.CurrentRun);
            PlayerSpawned?.Invoke(SpawnedPlayer);
            _onPlayerSpawned?.Invoke(SpawnedPlayer);
            Debug.Log($"[Player] Player positioned at spawn point '{_spawnPoint.name}'.");
        }

        /// <summary>
        /// Places either a fresh or existing player at the spawn marker with its controller bottom slightly above real ground.
        /// </summary>
        private void PlacePlayerAtSpawnSafely(PlayerMovement player)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
            {
                controller.enabled = false;
            }

            player.transform.SetPositionAndRotation(_spawnPoint.transform.position, _spawnPoint.transform.rotation);
            Physics.SyncTransforms();

            if (controller != null && TryFindGroundBelowSpawn(player.transform, out RaycastHit groundHit))
            {
                float desiredBottomY = groundHit.point.y + GetGroundClearance(controller, player.transform);
                float currentBottomY = GetControllerBottomWorldY(controller, player.transform);
                player.transform.position += Vector3.up * (desiredBottomY - currentBottomY);
                Physics.SyncTransforms();
            }
            else
            {
                Debug.LogWarning($"[PlayerSpawner] No valid ground found below spawn point: {_spawnPoint.name}", this);
            }

            if (controllerWasEnabled)
            {
                controller.enabled = true;
            }

            player.ResetVerticalVelocity();
        }

        private bool TryFindGroundBelowSpawn(Transform player, out RaycastHit groundHit)
        {
            Vector3 rayOrigin = _spawnPoint.transform.position + Vector3.up * GroundRaycastStartHeight;
            RaycastHit[] hits = Physics.RaycastAll(
                rayOrigin,
                Vector3.down,
                GroundRaycastDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                Collider collider = hit.collider;
                if (collider == null ||
                    collider.isTrigger ||
                    collider.transform.IsChildOf(player) ||
                    Vector3.Dot(hit.normal, Vector3.up) < 0.5f)
                {
                    continue;
                }

                groundHit = hit;
                return true;
            }

            groundHit = default;
            return false;
        }

        private static float GetControllerBottomWorldY(CharacterController controller, Transform player)
        {
            Vector3 localBottom = controller.center - Vector3.up * (controller.height * 0.5f);
            return player.TransformPoint(localBottom).y;
        }

        private static float GetGroundClearance(CharacterController controller, Transform player)
        {
            float worldSkinWidth = controller.skinWidth * Mathf.Abs(player.lossyScale.y);
            return Mathf.Clamp(worldSkinWidth, MinimumGroundClearance, MaximumGroundClearance);
        }
    }
}
