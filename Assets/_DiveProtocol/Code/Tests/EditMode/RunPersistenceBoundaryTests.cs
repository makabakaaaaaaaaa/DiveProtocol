using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class RunPersistenceBoundaryTests
    {
        private static readonly string[] _forbiddenJsonTerms =
        {
            "CurrentRun", "ActiveRun", "RunState", "CurrentLevelId", "CurrentHealth",
            "LoadedAmmo", "ReserveAmmo", "CurrentSeed", "EnvironmentState", "InventoryState",
            "BuildState", "PlayerPosition", "ResumeData"
        };

        [Test]
        public void MetaSavePublicDataContainsOnlyApprovedProgressionFields()
        {
            var propertyNames = typeof(MetaSaveData).GetProperties().Select(property => property.Name).ToArray();
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "SaveVersion", "TotalCurrency", "TotalRunsSettled", "SuccessfulRuns",
                    "BossKills", "RecentlyProcessedRunIds", "LastUpdatedUtc"
                },
                propertyNames);
        }

        [Test]
        public void SerializedMetaContainsNoRunStateDataOrSlotFiles()
        {
            WithTemporaryDirectory(directory =>
            {
                var serializer = new SaveFileSerializer(directory);
                serializer.Save(MetaSaveData.CreateDefault(DateTime.UtcNow));
                var json = File.ReadAllText(serializer.SaveFilePath);

                foreach (var forbiddenTerm in _forbiddenJsonTerms)
                {
                    StringAssert.DoesNotContain(forbiddenTerm, json);
                }

                Assert.That(Directory.GetFiles(directory, "*slot*"), Is.Empty);
            });
        }

        [Test]
        public void SaveManagerPublicApiAcceptsNoSlotIdentifier()
        {
            foreach (var method in typeof(SaveManager).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                Assert.That(method.GetParameters().Any(parameter =>
                    parameter.Name.IndexOf("slot", StringComparison.OrdinalIgnoreCase) >= 0), Is.False, method.Name);
            }
        }

        [Test]
        public void ActiveRunAndShutdownFlushDoNotPersistOrSettleRun()
        {
            WithTemporaryDirectory(directory =>
            {
                var saveManager = new SaveManager(directory);
                saveManager.Initialize();
                var gameObject = new GameObject("RunManagerBoundaryTest");
                var config = ScriptableObject.CreateInstance<GameConfig>();
                try
                {
                    var runManager = gameObject.AddComponent<RunManager>();
                    runManager.Initialize(config);
                    runManager.StartNewRun(7788);
                    runManager.CurrentRun.Player.TakeDamage(30);
                    runManager.CurrentRun.EnterLevel("L_TEST", 4);
                    runManager.CurrentRun.Score.AddBonusScore(500);

                    saveManager.FlushPendingMetaChanges();
                    var json = File.ReadAllText(saveManager.SaveFilePath);
                    var reloadedSave = new SaveManager(directory);
                    reloadedSave.Initialize();
                    var freshRunObject = new GameObject("FreshRunManager");
                    try
                    {
                        var freshRunManager = freshRunObject.AddComponent<RunManager>();
                        freshRunManager.Initialize(config);
                        Assert.That(freshRunManager.CurrentRun, Is.Null);
                        Assert.That(reloadedSave.CurrentMeta.TotalCurrency, Is.Zero);
                        Assert.That(reloadedSave.CurrentMeta.TotalRunsSettled, Is.Zero);
                        foreach (var forbiddenTerm in _forbiddenJsonTerms) StringAssert.DoesNotContain(forbiddenTerm, json);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(freshRunObject);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                    UnityEngine.Object.DestroyImmediate(config);
                }
            });
        }

        private static void WithTemporaryDirectory(Action<string> test)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"DiveProtocolBoundaryTests_{Guid.NewGuid():N}");
            try
            {
                test(directory);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
