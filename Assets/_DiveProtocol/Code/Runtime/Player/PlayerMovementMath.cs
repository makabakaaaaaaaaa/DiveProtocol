using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Pure movement helpers shared by runtime code and EditMode tests.</summary>
    public static class PlayerMovementMath
    {
        public static Vector2 NormalizeMoveInput(Vector2 input)
        {
            return Vector2.ClampMagnitude(input, 1f);
        }

        public static Vector3 CalculateCameraRelativeDirection(Vector2 input, Vector3 cameraForward, Vector3 cameraRight)
        {
            var normalizedInput = NormalizeMoveInput(input);
            return CalculateCameraRelativeDirectionFromNormalizedInput(normalizedInput, cameraForward, cameraRight);
        }

        public static Vector3 CalculateCameraRelativeDirectionFromNormalizedInput(Vector2 normalizedInput, Vector3 cameraForward, Vector3 cameraRight)
        {
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward = cameraForward.sqrMagnitude > 0f ? cameraForward.normalized : Vector3.forward;
            cameraRight = cameraRight.sqrMagnitude > 0f ? cameraRight.normalized : Vector3.right;
            return Vector3.ClampMagnitude(
                cameraForward * normalizedInput.y + cameraRight * normalizedInput.x,
                1f);
        }

        public static Vector3 CalculateWorldRelativeDirection(Vector2 input)
        {
            var normalizedInput = NormalizeMoveInput(input);
            return new Vector3(normalizedInput.x, 0f, normalizedInput.y);
        }
    }
}
