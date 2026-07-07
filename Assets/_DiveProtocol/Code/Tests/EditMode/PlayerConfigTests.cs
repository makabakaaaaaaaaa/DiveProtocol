using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class PlayerConfigTests
    {
        [Test]
        public void DefaultConfigContainsLegalMovementValues()
        {
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            try
            {
                Assert.That(config.IsValid, Is.True);
                Assert.That(config.MoveSpeed, Is.GreaterThan(0f));
                Assert.That(config.RotationSpeed, Is.GreaterThanOrEqualTo(0f));
                Assert.That(config.Gravity, Is.GreaterThanOrEqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(0f, 1f, 1f)]
        [TestCase(1f, -1f, 1f)]
        [TestCase(1f, 1f, -1f)]
        public void InvalidValuesAreRejected(float moveSpeed, float rotationSpeed, float gravity)
        {
            Assert.That(PlayerConfig.AreValuesValid(moveSpeed, rotationSpeed, gravity), Is.False);
        }
    }
}
