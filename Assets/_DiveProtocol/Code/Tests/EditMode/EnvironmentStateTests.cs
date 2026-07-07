using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class EnvironmentStateTests
    {
        [Test]
        public void SameSeedProducesSameEnvironment()
        {
            var first = EnvironmentState.CreateFromSeed(24680);
            var second = EnvironmentState.CreateFromSeed(24680);

            Assert.That(second.CorpseActivity, Is.EqualTo(first.CorpseActivity));
            Assert.That(second.ResourceDensity, Is.EqualTo(first.ResourceDensity));
        }

        [Test]
        public void GeneratedValuesAreValidAcrossSeeds()
        {
            for (var seed = -100; seed <= 100; seed++)
            {
                var environment = EnvironmentState.CreateFromSeed(seed);
                Assert.That(System.Enum.IsDefined(typeof(CorpseActivity), environment.CorpseActivity), Is.True);
                Assert.That(System.Enum.IsDefined(typeof(ResourceDensity), environment.ResourceDensity), Is.True);
            }
        }

        [Test]
        public void GenerationDoesNotChangeUnityGlobalRandomState()
        {
            var originalState = Random.state;
            try
            {
                Random.InitState(12345);
                var expected = Random.value;
                Random.InitState(12345);
                EnvironmentState.CreateFromSeed(9876);
                Assert.That(Random.value, Is.EqualTo(expected));
            }
            finally
            {
                Random.state = originalState;
            }
        }

        [Test]
        public void EnvironmentContainsNoUnityObjectReferences()
        {
            foreach (var field in typeof(EnvironmentState).GetFields(
                         System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.Public |
                         System.Reflection.BindingFlags.NonPublic))
            {
                Assert.That(typeof(Object).IsAssignableFrom(field.FieldType), Is.False, field.Name);
            }
        }
    }
}
