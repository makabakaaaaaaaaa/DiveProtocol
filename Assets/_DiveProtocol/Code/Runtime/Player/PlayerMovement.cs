using UnityEngine;
using DiveProtocol.CameraSystem;
using DiveProtocol.Gameplay;

namespace DiveProtocol
{
    /// <summary>Moves a CharacterController from camera-relative Gameplay input.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private Camera _movementCamera;

        private CharacterController _characterController;
        private PlayerInputReader _inputReader;
        private float _verticalVelocity;
        private bool _missingCameraLogged;
        private bool _hasLatchedCameraBasis;
        private Vector3 _latchedForward;
        private Vector3 _latchedRight;

        public PlayerConfig Config => _config;
        public float ExternalSpeedMultiplier { get; set; } = 1f;
        public float CurrentHorizontalSpeed { get; private set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _inputReader = GetComponent<PlayerInputReader>();
        }

        private void Start()
        {
            if (_characterController == null)
            {
                Debug.LogError("[Player] PlayerMovement requires a CharacterController on the same GameObject.");
                enabled = false;
                return;
            }

            if (_inputReader == null)
            {
                Debug.LogError("[Player] PlayerMovement requires a PlayerInputReader on the same GameObject.");
                enabled = false;
                return;
            }

            if (_config == null || !_config.IsValid)
            {
                Debug.LogError("[Player] PlayerMovement requires a valid PlayerConfig.");
                enabled = false;
                return;
            }

            ResolveMovementCamera();
        }

        private void Update()
        {
            if (_config == null || _characterController == null || _inputReader == null || !_inputReader.CanMove)
            {
                return;
            }

            if (_movementCamera == null)
            {
                ResolveMovementCamera();
                if (_movementCamera == null)
                {
                    return;
                }
            }

            var rawMoveInput = _inputReader.ReadMoveInput();
            if (CameraPeekController.IsAnyCameraPeeking || GameplayInputLock.IsLocked)
            {
                rawMoveInput = Vector2.zero;
            }

            var moveDirection = CalculateMoveDirection(rawMoveInput);

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity -= _config.Gravity * Time.deltaTime;
            float speedMultiplier = Mathf.Max(0f, ExternalSpeedMultiplier);
            var horizontalVelocity = moveDirection * (_config.MoveSpeed * speedMultiplier);
            CurrentHorizontalSpeed = horizontalVelocity.magnitude;
            var velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);

            if (_config.RotateTowardMovement && moveDirection.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _config.RotationSpeed * Time.deltaTime);
            }
        }

        public void SetMovementCamera(Camera movementCamera)
        {
            _movementCamera = movementCamera;
            _missingCameraLogged = false;
        }

        /// <summary>Clears accumulated vertical movement after an external spawn or reposition operation.</summary>
        public void ResetVerticalVelocity()
        {
            _verticalVelocity = 0f;
        }

        private Vector3 CalculateMoveDirection(Vector2 rawInput)
        {
            var normalizedInput = PlayerMovementMath.NormalizeMoveInput(rawInput);
            var active = normalizedInput.sqrMagnitude > _config.MovementInputDeadZone * _config.MovementInputDeadZone;
            switch (_config.MovementReferenceMode)
            {
                case PlayerMovementReferenceMode.WorldRelative:
                    _hasLatchedCameraBasis = false;
                    return active ? PlayerMovementMath.CalculateWorldRelativeDirection(normalizedInput) : Vector3.zero;

                case PlayerMovementReferenceMode.TankControl:
                    _hasLatchedCameraBasis = false;
                    return CalculateTankControlDirection(normalizedInput);

                case PlayerMovementReferenceMode.CameraRelativeLatched:
                    if (!active)
                    {
                        _hasLatchedCameraBasis = false;
                        return Vector3.zero;
                    }

                    if (!_hasLatchedCameraBasis)
                    {
                        CaptureCameraBasis(_movementCamera.transform);
                    }

                    return PlayerMovementMath.CalculateCameraRelativeDirectionFromNormalizedInput(normalizedInput, _latchedForward, _latchedRight);

                case PlayerMovementReferenceMode.CameraRelative:
                default:
                    _hasLatchedCameraBasis = false;
                    return active
                        ? PlayerMovementMath.CalculateCameraRelativeDirectionFromNormalizedInput(normalizedInput, _movementCamera.transform.forward, _movementCamera.transform.right)
                        : Vector3.zero;
            }
        }

        private Vector3 CalculateTankControlDirection(Vector2 normalizedInput)
        {
            if (Mathf.Abs(normalizedInput.x) > 0.001f)
            {
                transform.Rotate(Vector3.up, normalizedInput.x * _config.RotationSpeed * Time.deltaTime);
            }

            if (Mathf.Abs(normalizedInput.y) <= _config.MovementInputDeadZone)
            {
                return Vector3.zero;
            }

            var direction = transform.forward;
            direction.y = 0f;
            return direction.sqrMagnitude > 0f ? direction.normalized * Mathf.Clamp(normalizedInput.y, -1f, 1f) : Vector3.zero;
        }

        private void CaptureCameraBasis(Transform cameraTransform)
        {
            _latchedForward = cameraTransform.forward;
            _latchedRight = cameraTransform.right;
            _latchedForward.y = 0f;
            _latchedRight.y = 0f;
            _latchedForward = _latchedForward.sqrMagnitude > 0.0001f ? _latchedForward.normalized : Vector3.forward;
            _latchedRight = _latchedRight.sqrMagnitude > 0.0001f ? _latchedRight.normalized : Vector3.right;
            _hasLatchedCameraBasis = true;
        }

        private void ResolveMovementCamera()
        {
            _movementCamera = Camera.main;
            if (_movementCamera == null && !_missingCameraLogged)
            {
                _missingCameraLogged = true;
                Debug.LogError("[Player] No camera tagged MainCamera is available for camera-relative movement.");
            }
        }
    }
}
