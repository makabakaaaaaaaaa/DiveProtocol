using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class PlayerMovementMathTests
    {
        [Test]
        public void DiagonalInputIsClampedToStraightLineMagnitude()
        {
            var straight = PlayerMovementMath.NormalizeMoveInput(Vector2.up);
            var diagonal = PlayerMovementMath.NormalizeMoveInput(new Vector2(1f, 1f));

            Assert.That(straight.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(diagonal.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void CameraRelativeDirectionUsesHorizontalForwardAndRight()
        {
            var direction = PlayerMovementMath.CalculateCameraRelativeDirection(
                new Vector2(1f, 1f),
                new Vector3(0f, 2f, 1f),
                new Vector3(1f, 3f, 0f));

            Assert.That(direction.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.x, Is.GreaterThan(0f));
            Assert.That(direction.z, Is.GreaterThan(0f));
        }

        [Test]
        public void ZeroInputProducesZeroDirection()
        {
            Assert.That(PlayerMovementMath.CalculateCameraRelativeDirection(
                Vector2.zero, Vector3.forward, Vector3.right), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void LatchedCameraBasisCanRemainStableAfterCameraRotates()
        {
            var latchedForward = Vector3.forward;
            var latchedRight = Vector3.right;
            var currentCameraForwardAfterCut = Vector3.back;
            var currentCameraRightAfterCut = Vector3.left;

            var latched = PlayerMovementMath.CalculateCameraRelativeDirectionFromNormalizedInput(Vector2.up, latchedForward, latchedRight);
            var liveCameraRelative = PlayerMovementMath.CalculateCameraRelativeDirectionFromNormalizedInput(Vector2.up, currentCameraForwardAfterCut, currentCameraRightAfterCut);

            Assert.That(latched, Is.EqualTo(Vector3.forward));
            Assert.That(liveCameraRelative, Is.EqualTo(Vector3.back));
        }

        [Test]
        public void WorldRelativeDirectionDoesNotUseCameraAxes()
        {
            Assert.That(PlayerMovementMath.CalculateWorldRelativeDirection(new Vector2(1f, 1f)).magnitude, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
