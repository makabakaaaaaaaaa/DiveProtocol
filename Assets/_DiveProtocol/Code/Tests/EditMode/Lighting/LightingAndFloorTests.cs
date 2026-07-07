using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode.Lighting
{
    public sealed class LightingAndFloorTests
    {
        [Test]
        public void FlashlightConfigDefaultsAreValid()
        {
            var config = ScriptableObject.CreateInstance<FlashlightConfig>();
            try
            {
                Assert.That(config.IsValid, Is.True);
                Assert.That(config.Range, Is.GreaterThan(0f));
                Assert.That(config.InnerSpotAngle, Is.LessThanOrEqualTo(config.SpotAngle));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void FloorVisibilityOnlyTogglesVisualComponents()
        {
            var floor = new GameObject("Floor");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var trigger = floor.AddComponent<BoxCollider>();
            try
            {
                visual.transform.SetParent(floor.transform, false);
                var renderer = visual.GetComponent<Renderer>();
                var group = floor.AddComponent<FloorVisibilityGroup>();
                group.SetCollectedComponents(
                    new[] { renderer },
                    System.Array.Empty<Light>(),
                    System.Array.Empty<ReflectionProbe>(),
                    System.Array.Empty<Behaviour>(),
                    System.Array.Empty<ParticleSystem>(),
                    System.Array.Empty<Behaviour>(),
                    System.Array.Empty<AudioSource>());

                group.SetVisualsVisible(false);

                Assert.That(renderer.enabled, Is.False);
                Assert.That(trigger.enabled, Is.True);
                Assert.That(floor.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(floor);
            }
        }

        [Test]
        public void MultiFloorControllerAppliesExpectedVisibilityStates()
        {
            var root = new GameObject("FloorController");
            var floor01 = new GameObject("Floor01");
            var floor02 = new GameObject("Floor02");
            var cube01 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cube02 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                cube01.transform.SetParent(floor01.transform, false);
                cube02.transform.SetParent(floor02.transform, false);
                var renderer01 = cube01.GetComponent<Renderer>();
                var renderer02 = cube02.GetComponent<Renderer>();
                var group01 = floor01.AddComponent<FloorVisibilityGroup>();
                var group02 = floor02.AddComponent<FloorVisibilityGroup>();
                group01.Configure(FloorId.Floor01, true);
                group02.Configure(FloorId.Floor02, false);
                group01.SetCollectedComponents(new[] { renderer01 }, null, null, null, null, null, null);
                group02.SetCollectedComponents(new[] { renderer02 }, null, null, null, null, null, null);

                var controller = root.AddComponent<MultiFloorVisibilityController>();
                controller.Configure(group01, group02);
                controller.ApplyState(FloorVisibilityState.Floor01Only, force: true);

                Assert.That(renderer01.enabled, Is.True);
                Assert.That(renderer02.enabled, Is.False);

                controller.ApplyState(FloorVisibilityState.TransitionBoth);
                Assert.That(renderer01.enabled, Is.True);
                Assert.That(renderer02.enabled, Is.True);

                controller.ApplyState(FloorVisibilityState.Floor02Only);
                Assert.That(renderer01.enabled, Is.False);
                Assert.That(renderer02.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(floor01);
                Object.DestroyImmediate(floor02);
            }
        }
    }
}
