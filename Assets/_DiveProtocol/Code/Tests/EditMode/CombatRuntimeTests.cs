using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class CombatRuntimeTests
    {
        [Test]
        public void HealthComponentAppliesDamageAndDiesOnce()
        {
            GameObject go = new GameObject("Health Test");
            try
            {
                HealthComponent health = go.AddComponent<HealthComponent>();
                InvokeAwake(health);

                int diedCount = 0;
                health.Died += _ => diedCount++;

                health.TakeDamage(new DamageInfo(25f, go));
                Assert.That(health.CurrentHealth, Is.EqualTo(75f));
                Assert.That(health.IsAlive, Is.True);

                health.TakeDamage(new DamageInfo(200f, go));
                Assert.That(health.CurrentHealth, Is.Zero);
                Assert.That(health.IsAlive, Is.False);
                Assert.That(diedCount, Is.EqualTo(1));

                health.TakeDamage(new DamageInfo(200f, go));
                Assert.That(diedCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HealthComponentHealDoesNotExceedMaxHealth()
        {
            GameObject go = new GameObject("Heal Test");
            try
            {
                HealthComponent health = go.AddComponent<HealthComponent>();
                InvokeAwake(health);

                health.TakeDamage(new DamageInfo(40f, go));
                Assert.That(health.Heal(500f), Is.EqualTo(40f));
                Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HealthComponentTrySpendHealthRejectsIllegalOverspend()
        {
            GameObject go = new GameObject("Spend Test");
            try
            {
                HealthComponent health = go.AddComponent<HealthComponent>();
                InvokeAwake(health);

                Assert.That(health.TrySpendHealth(0f), Is.False);
                Assert.That(health.TrySpendHealth(100f), Is.False);
                Assert.That(health.TrySpendHealth(99f), Is.True);
                Assert.That(health.CurrentHealth, Is.GreaterThan(0f));
                Assert.That(health.TrySpendHealth(1f, go, true), Is.True);
                Assert.That(health.IsAlive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EnemyWaveSpawnerBeginAndStopAreNotRepeated()
        {
            GameObject spawnerObject = new GameObject("Wave Spawner Test");
            GameObject enemyPrefab = new GameObject("Enemy Prefab");
            GameObject spawnPointObject = new GameObject("Spawn Point");
            GameObject targetObject = new GameObject("Player Target");

            try
            {
                enemyPrefab.AddComponent<HealthComponent>();
                EnemyWaveSpawner spawner = spawnerObject.AddComponent<EnemyWaveSpawner>();

                SetSerializedField(spawner, "enemyPrefab", enemyPrefab);
                SetSerializedField(spawner, "spawnPoints", new[] { spawnPointObject.transform });
                SetSerializedField(spawner, "playerTarget", targetObject.transform);
                SetSerializedField(spawner, "spawnImmediatelyOnBegin", false);

                Assert.That(spawner.BeginSpawning(), Is.True);
                Assert.That(spawner.BeginSpawning(), Is.False);
                Assert.That(spawner.IsSpawning, Is.True);

                Assert.That(spawner.StopSpawning(), Is.True);
                Assert.That(spawner.StopSpawning(), Is.False);
                Assert.That(spawner.IsSpawning, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(spawnerObject);
                Object.DestroyImmediate(enemyPrefab);
                Object.DestroyImmediate(spawnPointObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        private static void InvokeAwake(MonoBehaviour behaviour)
        {
            if (behaviour is HealthComponent health &&
                (health.CurrentHealth > 0f || health.IsAlive))
            {
                return;
            }

            MethodInfo awakeMethod = behaviour.GetType().GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(
                awakeMethod,
                Is.Not.Null,
                $"{behaviour.GetType().Name} does not define an Awake method.");

            awakeMethod.Invoke(behaviour, null);
        }

        private static void SetSerializedField<T>(Object target, string fieldName, T value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field '{fieldName}'.");

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = value as Object;
                    break;

                case SerializedPropertyType.Boolean:
                    property.boolValue = value is bool boolValue && boolValue;
                    break;

                default:
                    if (property.isArray && value is Transform[] transforms)
                    {
                        property.arraySize = transforms.Length;
                        for (int i = 0; i < transforms.Length; i++)
                        {
                            property.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
                        }
                    }
                    else
                    {
                        Assert.Fail($"Unsupported serialized field type for '{fieldName}'.");
                    }
                    break;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
