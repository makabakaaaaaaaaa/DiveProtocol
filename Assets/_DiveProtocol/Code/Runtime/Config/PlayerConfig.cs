using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Static tuning values copied by the basic player movement system.</summary>
    [CreateAssetMenu(fileName = "SO_PlayerConfig_Default", menuName = "Dive Protocol/Config/Player Config", order = 20)]
    public sealed class PlayerConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _moveSpeed = 5f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float _gravity = 20f;
        [SerializeField] private bool _rotateTowardMovement = true;
        [SerializeField] private PlayerMovementReferenceMode _movementReferenceMode = PlayerMovementReferenceMode.CameraRelativeLatched;
        [SerializeField, Range(0f, 0.5f)] private float _movementInputDeadZone = 0.08f;

        public float MoveSpeed => _moveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public float Gravity => _gravity;
        public bool RotateTowardMovement => _rotateTowardMovement;
        public PlayerMovementReferenceMode MovementReferenceMode => _movementReferenceMode;
        public float MovementInputDeadZone => _movementInputDeadZone;
        public bool IsValid => AreValuesValid(_moveSpeed, _rotationSpeed, _gravity);

        internal static bool AreValuesValid(float moveSpeed, float rotationSpeed, float gravity)
        {
            return moveSpeed > 0f && rotationSpeed >= 0f && gravity >= 0f;
        }

        public void ConfigureMovementReferenceMode(PlayerMovementReferenceMode mode, float deadZone)
        {
            _movementReferenceMode = mode;
            _movementInputDeadZone = Mathf.Clamp(deadZone, 0f, 0.5f);
        }

        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0.01f, _moveSpeed);
            _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
            _gravity = Mathf.Max(0f, _gravity);
            _movementInputDeadZone = Mathf.Clamp(_movementInputDeadZone, 0f, 0.5f);
        }
    }

    public enum PlayerMovementReferenceMode
    {
        CameraRelative,
        CameraRelativeLatched,
        WorldRelative,
        TankControl
    }
}
