using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class PlayerInputReaderTests
    {
        [Test]
        public void GameplayMapContainsRequiredActions()
        {
            var gameObject = new GameObject("PlayerInputReaderTest");
            try
            {
                var reader = gameObject.AddComponent<PlayerInputReader>();
                Assert.That(reader.GameplayMap.name, Is.EqualTo("Gameplay"));
                Assert.That(reader.GameplayMap.FindAction("Move"), Is.Not.Null);
                Assert.That(reader.GameplayMap.FindAction("Pause"), Is.Not.Null);
                Assert.That(reader.ReadMoveInput(), Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
