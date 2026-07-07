using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class RunStateTests
    {
        [Test]
        public void FactoryCreatesIndependentStateGraphsAndEndedRunCannotEnterLevel()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                var first = RunFactory.Create(1234, config);
                var second = RunFactory.Create(1234, config);

                Assert.That(first.Player, Is.Not.SameAs(second.Player));
                Assert.That(first.Environment, Is.Not.SameAs(second.Environment));
                Assert.That(first.Inventory, Is.Not.SameAs(second.Inventory));
                Assert.That(first.Score, Is.Not.SameAs(second.Score));
                Assert.That(first.RunId, Is.Not.EqualTo(second.RunId));
                Assert.That(first.Player, Is.Not.Null);
                Assert.That(first.Environment, Is.Not.Null);
                Assert.That(first.Inventory, Is.Not.Null);
                Assert.That(first.Score, Is.Not.Null);
                Assert.That(first.Environment.CorpseActivity, Is.EqualTo(second.Environment.CorpseActivity));
                Assert.That(first.Environment.ResourceDensity, Is.EqualTo(second.Environment.ResourceDensity));

                Assert.That(first.Complete(), Is.True);
                Assert.That(first.EnterLevel("L02_Test", 1), Is.False);
                Assert.That(first.CurrentLevelId, Is.EqualTo(RunFactory.InitialLevelId));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void RunStateContainsNoPersistenceOrSceneObjectFields()
        {
            foreach (var field in typeof(RunState).GetFields(
                         System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.Public |
                         System.Reflection.BindingFlags.NonPublic))
            {
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType), Is.False, field.Name);
                Assert.That(field.FieldType, Is.Not.EqualTo(typeof(System.IO.FileInfo)), field.Name);
                Assert.That(field.FieldType, Is.Not.EqualTo(typeof(System.IO.DirectoryInfo)), field.Name);
            }

            Assert.That(typeof(RunState).GetMethods().Any(method =>
                method.Name.IndexOf("Save", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                method.Name.IndexOf("Load", System.StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }
    }
}
