using System.Collections.Generic;
using DiveProtocol.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace DiveProtocol.Doors
{
    /// <summary>
    /// Independent double-leaf automatic sliding door driven by a trigger volume and local-space leaf movement.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class AutomaticSlidingDoor : MonoBehaviour
    {
        private const float ArrivalTolerance = 0.001f;

        [Header("Door Leaves")]
        [Tooltip("Left leaf. It opens along its closed local X negative direction.")]
        [SerializeField]
        private Transform leftDoorLeaf;

        [Tooltip("Right leaf. It opens along its closed local X positive direction.")]
        [SerializeField]
        private Transform rightDoorLeaf;

        [Header("Motion")]
        [Tooltip("Local-space distance each leaf travels from its closed position.")]
        [SerializeField, Min(0.01f)]
        private float openDistance = 1f;

        [Tooltip("Door leaf movement speed in local units per second.")]
        [SerializeField, Min(0.01f)]
        private float moveSpeed = 2.5f;

        [Tooltip("Delay after all player colliders leave before the door starts closing.")]
        [SerializeField, Min(0f)]
        private float closeDelaySeconds = 0.5f;

        [Tooltip("When enabled, both leaves start fully open without playing movement or events.")]
        [SerializeField]
        private bool startOpen;

        [Header("Trigger")]
        [Tooltip("When enabled, player trigger entry opens the door automatically.")]
        [SerializeField]
        private bool openOnPlayerEnter = true;

        [Tooltip("When enabled, the door closes after the last player collider leaves the trigger.")]
        [SerializeField]
        private bool closeOnPlayerExit;

        [Header("Locking")]
        [Tooltip("When locked, player triggers cannot automatically open this door.")]
        [SerializeField]
        private bool locked;

        [Header("Debug")]
        [SerializeField]
        private bool debugLogs;

        [Header("Navigation")]
        [Tooltip("Ensures each non-trigger door leaf BoxCollider carves the NavMesh for enemy agents.")]
        [SerializeField]
        private bool ensureNavMeshObstacles = true;

        [Tooltip("Copies each leaf BoxCollider center and size to its NavMeshObstacle.")]
        [SerializeField]
        private bool configureNavMeshObstaclesFromColliders = true;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onFullyOpened;

        [SerializeField]
        private UnityEvent onFullyClosed;

        private readonly HashSet<int> _playerColliderIds = new();

        private Vector3 _leftClosedLocalPosition;
        private Vector3 _rightClosedLocalPosition;
        private Vector3 _leftOpenLocalPosition;
        private Vector3 _rightOpenLocalPosition;
        private Vector3 _leftTargetLocalPosition;
        private Vector3 _rightTargetLocalPosition;
        private bool _isConfigured;
        private bool _targetOpen;
        private bool _closeDelayActive;
        private float _closeAtTime;

        public bool IsOpen { get; private set; }
        public bool IsMoving { get; private set; }
        public bool HasPlayerInRange => _playerColliderIds.Count > 0;
        public bool IsLocked => locked;

        private void Awake()
        {
            CacheDoorPositions();
            ValidateTriggerCollider();

            if (!_isConfigured)
            {
                enabled = false;
                return;
            }

            SetOpenImmediate(startOpen);
            EnsureNavigationBlocking();
        }

        private void Update()
        {
            if (!_isConfigured)
            {
                return;
            }

            UpdateCloseDelay();
            MoveLeaves();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHandlePlayerTrigger(other, "Enter");
        }

        private void OnTriggerStay(Collider other)
        {
            TryHandlePlayerTrigger(other, "Stay");
        }

        private void TryHandlePlayerTrigger(Collider other, string phase)
        {
            if (!_isConfigured || !openOnPlayerEnter)
            {
                LogDebug($"AutoDoor Trigger {phase}: ignored because configured={_isConfigured}, openOnPlayerEnter={openOnPlayerEnter}.");
                return;
            }

            bool isPlayer = IsPlayerCollider(other);
            LogDebug($"AutoDoor Trigger {phase}: other='{(other != null ? other.name : "<null>")}', IsPlayer={isPlayer}, Locked={locked}.");

            if (locked || !isPlayer)
            {
                return;
            }

            _playerColliderIds.Add(other.GetInstanceID());
            _closeDelayActive = false;
            LogDebug("AutoDoor: Calling Open().");
            Open();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_isConfigured || !IsPlayerCollider(other))
            {
                return;
            }

            _playerColliderIds.Remove(other.GetInstanceID());
            LogDebug($"AutoDoor Trigger Exit: other='{other.name}', remainingPlayerColliders={_playerColliderIds.Count}.");

            if (_playerColliderIds.Count > 0 || !closeOnPlayerExit)
            {
                return;
            }

            if (closeDelaySeconds <= 0f)
            {
                Close();
                return;
            }

            _closeDelayActive = true;
            _closeAtTime = Time.time + closeDelaySeconds;
        }

        private void OnDisable()
        {
            _playerColliderIds.Clear();
            _closeDelayActive = false;
        }

        /// <summary>
        /// Starts opening both door leaves toward their cached local open positions.
        /// </summary>
        public void Open()
        {
            if (!_isConfigured || locked)
            {
                return;
            }

            _closeDelayActive = false;
            SetTarget(open: true);
        }

        /// <summary>
        /// Starts closing both door leaves toward their cached local closed positions.
        /// </summary>
        public void Close()
        {
            if (!_isConfigured)
            {
                return;
            }

            _closeDelayActive = false;
            SetTarget(open: false);
        }

        /// <summary>
        /// Enables or disables automatic opening. Locking closes the door if possible.
        /// </summary>
        public void SetLocked(bool locked)
        {
            this.locked = locked;
            _playerColliderIds.Clear();
            _closeDelayActive = false;

            if (locked)
            {
                Close();
            }
        }

        /// <summary>
        /// UnityEvent-friendly method that prevents future automatic opening and closes the door.
        /// </summary>
        public void LockDoor()
        {
            SetLocked(true);
        }

        /// <summary>
        /// UnityEvent-friendly method that allows player triggers to open the door again.
        /// </summary>
        public void UnlockDoor()
        {
            SetLocked(false);
        }

        /// <summary>
        /// Immediately places both leaves at their open or closed positions without firing completion events.
        /// </summary>
        public void SetOpenImmediate(bool open)
        {
            if (!_isConfigured)
            {
                return;
            }

            _closeDelayActive = false;
            _targetOpen = open;
            IsOpen = open;
            IsMoving = false;

            _leftTargetLocalPosition = open
                ? _leftOpenLocalPosition
                : _leftClosedLocalPosition;

            _rightTargetLocalPosition = open
                ? _rightOpenLocalPosition
                : _rightClosedLocalPosition;

            leftDoorLeaf.localPosition = _leftTargetLocalPosition;
            rightDoorLeaf.localPosition = _rightTargetLocalPosition;
        }

        /// <summary>
        /// Enables or disables NavMeshObstacle components on both sliding leaves.
        /// </summary>
        public void SetNavigationBlocking(bool blocked)
        {
            DoorNavigationObstacleUtility.SetEnabled(leftDoorLeaf, blocked);
            DoorNavigationObstacleUtility.SetEnabled(rightDoorLeaf, blocked);
        }

        private void EnsureNavigationBlocking()
        {
            if (!ensureNavMeshObstacles)
            {
                return;
            }

            DoorNavigationObstacleUtility.EnsureBoxObstacles(
                leftDoorLeaf,
                addMissing: true,
                configureFromCollider: configureNavMeshObstaclesFromColliders);

            DoorNavigationObstacleUtility.EnsureBoxObstacles(
                rightDoorLeaf,
                addMissing: true,
                configureFromCollider: configureNavMeshObstaclesFromColliders);
        }

        private void CacheDoorPositions()
        {
            if (leftDoorLeaf == null || rightDoorLeaf == null)
            {
                Debug.LogError(
                    $"[Door] {nameof(AutomaticSlidingDoor)} on '{name}' requires both left and right door leaves.",
                    this);

                _isConfigured = false;
                return;
            }

            openDistance = Mathf.Max(0.01f, openDistance);
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
            closeDelaySeconds = Mathf.Max(0f, closeDelaySeconds);

            _leftClosedLocalPosition = leftDoorLeaf.localPosition;
            _rightClosedLocalPosition = rightDoorLeaf.localPosition;
            _leftOpenLocalPosition = _leftClosedLocalPosition + Vector3.left * openDistance;
            _rightOpenLocalPosition = _rightClosedLocalPosition + Vector3.right * openDistance;
            _isConfigured = true;
        }

        private void ValidateTriggerCollider()
        {
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider == null)
            {
                return;
            }

            if (!triggerCollider.isTrigger)
            {
                triggerCollider.isTrigger = true;
                Debug.LogWarning(
                    $"[Door] {nameof(AutomaticSlidingDoor)} on '{name}' changed its BoxCollider to Is Trigger at runtime.",
                    this);
            }
        }

        private void UpdateCloseDelay()
        {
            if (!_closeDelayActive || HasPlayerInRange || Time.time < _closeAtTime)
            {
                return;
            }

            _closeDelayActive = false;
            Close();
        }

        private void MoveLeaves()
        {
            if (!IsMoving)
            {
                return;
            }

            float maxDistanceDelta = moveSpeed * Time.deltaTime;

            leftDoorLeaf.localPosition = Vector3.MoveTowards(
                leftDoorLeaf.localPosition,
                _leftTargetLocalPosition,
                maxDistanceDelta);

            rightDoorLeaf.localPosition = Vector3.MoveTowards(
                rightDoorLeaf.localPosition,
                _rightTargetLocalPosition,
                maxDistanceDelta);

            if (!HaveLeavesReachedTargets())
            {
                return;
            }

            leftDoorLeaf.localPosition = _leftTargetLocalPosition;
            rightDoorLeaf.localPosition = _rightTargetLocalPosition;
            IsMoving = false;
            IsOpen = _targetOpen;

            if (IsOpen)
            {
                onFullyOpened?.Invoke();
            }
            else
            {
                onFullyClosed?.Invoke();
            }
        }

        private void SetTarget(bool open)
        {
            _targetOpen = open;
            _leftTargetLocalPosition = open
                ? _leftOpenLocalPosition
                : _leftClosedLocalPosition;
            _rightTargetLocalPosition = open
                ? _rightOpenLocalPosition
                : _rightClosedLocalPosition;

            if (HaveLeavesReachedTargets())
            {
                leftDoorLeaf.localPosition = _leftTargetLocalPosition;
                rightDoorLeaf.localPosition = _rightTargetLocalPosition;
                IsMoving = false;
                IsOpen = open;
                return;
            }

            IsMoving = true;
        }

        private bool HaveLeavesReachedTargets()
        {
            return Vector3.SqrMagnitude(leftDoorLeaf.localPosition - _leftTargetLocalPosition) <= ArrivalTolerance * ArrivalTolerance &&
                   Vector3.SqrMagnitude(rightDoorLeaf.localPosition - _rightTargetLocalPosition) <= ArrivalTolerance * ArrivalTolerance;
        }

        private static bool IsPlayerCollider(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            return other.GetComponentInParent<PlayerInteractor>() != null ||
                   other.GetComponentInParent<DiveProtocol.PlayerMovement>() != null ||
                   other.GetComponentInParent<CharacterController>() != null;
        }

        private void LogDebug(string message)
        {
            if (debugLogs)
            {
                Debug.Log($"[Door] {message}", this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            openDistance = Mathf.Max(0.01f, openDistance);
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
            closeDelaySeconds = Mathf.Max(0f, closeDelaySeconds);

            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }
#endif
    }
}
