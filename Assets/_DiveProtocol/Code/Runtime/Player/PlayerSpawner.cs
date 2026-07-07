using System;
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
            else
            {
                MoveToSpawnPoint(player);
            }

            player.SetMovementCamera(Camera.main);
            SpawnedPlayer = player.transform;
            PlayerSpawned?.Invoke(SpawnedPlayer);
            _onPlayerSpawned?.Invoke(SpawnedPlayer);
            Debug.Log($"[Player] Player positioned at spawn point '{_spawnPoint.name}'.");
        }

        private void MoveToSpawnPoint(PlayerMovement player)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.transform.SetPositionAndRotation(_spawnPoint.transform.position, _spawnPoint.transform.rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }
}
