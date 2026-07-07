using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class InventoryStateTests
    {
        [Test]
        public void KeyItemsAreUniqueValidatedAndRemovable()
        {
            var inventory = new InventoryState();
            Assert.That(inventory.AddKeyItem("Key_A"), Is.True);
            Assert.That(inventory.AddKeyItem("Key_A"), Is.False);
            Assert.That(inventory.AddKeyItem(""), Is.False);
            Assert.That(inventory.AddKeyItem("   "), Is.False);
            Assert.That(inventory.HasKeyItem("Key_A"), Is.True);
            Assert.That(inventory.RemoveKeyItem("Key_A"), Is.True);
            Assert.That(inventory.HasKeyItem("Key_A"), Is.False);
        }

        [Test]
        public void InventoriesDoNotShareCollectionsAndContainNoSceneReferences()
        {
            var first = new InventoryState();
            var second = new InventoryState();
            first.AddKeyItem("OnlyFirst");
            Assert.That(second.HasKeyItem("OnlyFirst"), Is.False);

            foreach (var field in typeof(InventoryState).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                Assert.That(typeof(Object).IsAssignableFrom(field.FieldType), Is.False, field.Name);
            }
        }
    }
}
