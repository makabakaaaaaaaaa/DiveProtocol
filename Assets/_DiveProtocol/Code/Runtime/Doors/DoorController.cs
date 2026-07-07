using System;
using UnityEngine;

namespace DiveProtocol.Doors
{
    /// <summary>
    /// Rotates a hinge pivot between closed and open states for a reusable plain hinged door.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorController : MonoBehaviour
    {
        private const float OpenAngleDegrees = -90f;
        private const float DefaultRotationSpeedDegrees = 180f;
        private const float ArriveAngleToleranceDegrees = 0.1f;

        [Header("Door Motion")]
        [Tooltip("Pivot placed at the left edge of the door leaf. Only this transform is rotated.")]
        [SerializeField] private Transform hingePivot;

        [Tooltip("Door rotation speed in degrees per second.")]
        [SerializeField, Min(1f)] private float rotationSpeedDegrees = DefaultRotationSpeedDegrees;

        [Tooltip("When enabled, the door starts fully open without playing an opening motion.")]
        [SerializeField] private bool startOpen;

        [Header("Navigation")]
        [Tooltip("Ensures non-trigger BoxColliders under the hinge pivot carve the NavMesh for enemy agents.")]
        [SerializeField] private bool ensureNavMeshObstacles = true;

        [Tooltip("Copies each door leaf BoxCollider center and size to its NavMeshObstacle.")]
        [SerializeField] private bool configureNavMeshObstaclesFromColliders = true;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private Quaternion _targetRotation;
        private bool _isConfigured;
        private bool _targetOpenState;

        /// <summary>
        /// Raised when the door finishes opening or closing. The bool is the final open state.
        /// </summary>
        public event Action<bool> DoorStateChanged;

        public bool IsOpen { get; private set; }
        public bool IsMoving { get; private set; }
        public Transform HingePivot => hingePivot;

        private void Awake()
        {
            InitializeRotations();

            if (!_isConfigured)
            {
                return;
            }

            SetOpenImmediate(startOpen);
            EnsureNavigationBlocking();
        }

        private void Update()
        {
            if (!_isConfigured || !IsMoving)
            {
                return;
            }

            hingePivot.localRotation = Quaternion.RotateTowards(
                hingePivot.localRotation,
                _targetRotation,
                rotationSpeedDegrees * Time.deltaTime);

            if (Quaternion.Angle(hingePivot.localRotation, _targetRotation) > ArriveAngleToleranceDegrees)
            {
                return;
            }

            hingePivot.localRotation = _targetRotation;
            IsMoving = false;
            IsOpen = _targetOpenState;
            DoorStateChanged?.Invoke(IsOpen);
        }

        /// <summary>
        /// Starts opening the door if it is closed and idle.
        /// </summary>
        public bool Open()
        {
            if (!_isConfigured || IsMoving || IsOpen)
            {
                return false;
            }

            _targetRotation = _openRotation;
            _targetOpenState = true;
            IsMoving = true;
            return true;
        }

        /// <summary>
        /// Starts closing the door if it is open and idle.
        /// </summary>
        public bool Close()
        {
            if (!_isConfigured || IsMoving || !IsOpen)
            {
                return false;
            }

            _targetRotation = _closedRotation;
            _targetOpenState = false;
            IsMoving = true;
            return true;
        }

        /// <summary>
        /// Opens a closed door or closes an open door when idle.
        /// </summary>
        public bool Toggle()
        {
            if (!_isConfigured || IsMoving)
            {
                return false;
            }

            return IsOpen ? Close() : Open();
        }

        /// <summary>
        /// Immediately sets the completed open or closed state without animation.
        /// </summary>
        public void SetOpenImmediate(bool open)
        {
            if (!_isConfigured)
            {
                return;
            }

            IsMoving = false;
            IsOpen = open;
            _targetOpenState = open;
            _targetRotation = open ? _openRotation : _closedRotation;
            hingePivot.localRotation = _targetRotation;
        }

        /// <summary>
        /// Enables or disables all NavMeshObstacle components under this door leaf hierarchy.
        /// </summary>
        public void SetNavigationBlocking(bool blocked)
        {
            if (hingePivot == null)
            {
                return;
            }

            DoorNavigationObstacleUtility.SetEnabled(hingePivot, blocked);
        }

        private void EnsureNavigationBlocking()
        {
            if (!ensureNavMeshObstacles || hingePivot == null)
            {
                return;
            }

            DoorNavigationObstacleUtility.EnsureBoxObstacles(
                hingePivot,
                addMissing: true,
                configureFromCollider: configureNavMeshObstaclesFromColliders);
        }

        private void InitializeRotations()
        {
            if (hingePivot == null)
            {
                Debug.LogError(
                    "[Door] DoorController requires a Hinge Pivot transform.",
                    this);

                _isConfigured = false;
                enabled = false;
                return;
            }

            rotationSpeedDegrees = Mathf.Max(1f, rotationSpeedDegrees);
            _closedRotation = hingePivot.localRotation;
            _openRotation = _closedRotation * Quaternion.AngleAxis(OpenAngleDegrees, Vector3.up);
            _targetRotation = _closedRotation;
            _isConfigured = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            rotationSpeedDegrees = Mathf.Max(1f, rotationSpeedDegrees);
        }
#endif
    }
}
