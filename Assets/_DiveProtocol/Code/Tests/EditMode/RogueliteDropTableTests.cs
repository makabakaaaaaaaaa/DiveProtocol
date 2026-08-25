using DiveProtocol.Loot;
using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class RogueliteDropTableTests
    {
        [TestCase(0.00f)]
        [TestCase(0.299f)]
        public void NormalEnemyRollsNoDropForItsFirstThirtyPercent(float roll)
        {
            var owner = new GameObject("Loot Table Test");
            try
            {
                EnemyLootDropper dropper = owner.AddComponent<EnemyLootDropper>();
                Assert.That(dropper.TryRollDrop(roll, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(0.30f, DropItemType.HealingDrop)]
        [TestCase(0.599f, DropItemType.HealingDrop)]
        [TestCase(0.60f, DropItemType.AmmoDrop)]
        [TestCase(0.849f, DropItemType.AmmoDrop)]
        [TestCase(0.85f, DropItemType.RandomBuildDrop)]
        [TestCase(0.999f, DropItemType.RandomBuildDrop)]
        public void NormalEnemyRollsExactlyOneConfiguredDrop(float roll, DropItemType expectedType)
        {
            var owner = new GameObject("Loot Table Test");
            try
            {
                EnemyLootDropper dropper = owner.AddComponent<EnemyLootDropper>();
                Assert.That(dropper.TryRollDrop(roll, out DropItemType actualType), Is.True);
                Assert.That(actualType, Is.EqualTo(expectedType));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
