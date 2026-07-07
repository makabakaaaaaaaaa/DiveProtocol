using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class SaveManagerTests
    {
        private string _temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"DiveProtocolTests_{Guid.NewGuid():N}");
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            if (Directory.Exists(_temporaryDirectory))
            {
                Directory.Delete(_temporaryDirectory, true);
            }
        }

        [Test]
        public void EligibleResultIsAppliedOnceAcrossReloads()
        {
            var now = new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);
            var manager = new SaveManager(_temporaryDirectory, () => now);
            manager.Initialize();
            var result = CreateResult(RunEndReason.DemoCompleted, 125);

            var firstOutcome = manager.ApplyRunResult(result);
            var duplicateOutcome = manager.ApplyRunResult(result);
            var reloadedManager = new SaveManager(_temporaryDirectory, () => now);
            reloadedManager.Initialize();
            var postRestartOutcome = reloadedManager.ApplyRunResult(result);

            Assert.That(firstOutcome.Status, Is.EqualTo(RunResultApplyStatus.Applied));
            Assert.That(firstOutcome.CurrencyGained, Is.EqualTo(12));
            Assert.That(duplicateOutcome.Status, Is.EqualTo(RunResultApplyStatus.AlreadyProcessed));
            Assert.That(postRestartOutcome.Status, Is.EqualTo(RunResultApplyStatus.AlreadyProcessed));
            Assert.That(reloadedManager.CurrentMeta.TotalCurrency, Is.EqualTo(12));
            Assert.That(reloadedManager.CurrentMeta.TotalRunsSettled, Is.EqualTo(1));
            Assert.That(reloadedManager.CurrentMeta.SuccessfulRuns, Is.EqualTo(1));
        }

        [Test]
        public void AbortedResultIsNotEligibleAndDoesNotRecordRunId()
        {
            var manager = new SaveManager(_temporaryDirectory);
            manager.Initialize();
            var result = CreateResult(RunEndReason.Aborted, 1000);

            var outcome = manager.ApplyRunResult(result);

            Assert.That(outcome.Status, Is.EqualTo(RunResultApplyStatus.NotEligible));
            Assert.That(manager.CurrentMeta.TotalCurrency, Is.Zero);
            Assert.That(manager.CurrentMeta.TotalRunsSettled, Is.Zero);
            Assert.That(manager.CurrentMeta.RecentlyProcessedRunIds, Is.Empty);
        }

        [Test]
        public void CorruptPrimaryRecoversValidBackup()
        {
            var manager = new SaveManager(_temporaryDirectory);
            manager.Initialize();
            Assert.That(manager.AddTestCurrency(100), Is.True);
            Assert.That(manager.AddTestCurrency(50), Is.True);
            File.WriteAllText(manager.SaveFilePath, "not valid json");
            LogAssert.Expect(
                LogType.Exception,
                new Regex(@"Save file is not a JSON object: .*diveprotocol_save\.json"));

            var recoveredManager = new SaveManager(_temporaryDirectory);
            Assert.DoesNotThrow(recoveredManager.Initialize);

            Assert.That(recoveredManager.CurrentMeta.TotalCurrency, Is.EqualTo(100));
            Assert.That(File.Exists(recoveredManager.SaveFilePath), Is.True);
        }

        [TestCase(RunEndReason.PlayerDied, 0, 0)]
        [TestCase(RunEndReason.Extracted, 1, 0)]
        [TestCase(RunEndReason.BossDefeated, 1, 1)]
        public void EligibleEndReasonsUpdateExpectedStatistics(
            RunEndReason endReason,
            int expectedSuccessfulRuns,
            int expectedBossKills)
        {
            var manager = new SaveManager(_temporaryDirectory);
            manager.Initialize();

            var outcome = manager.ApplyRunResult(CreateResult(endReason, 100));

            Assert.That(outcome.Status, Is.EqualTo(RunResultApplyStatus.Applied));
            Assert.That(manager.CurrentMeta.TotalRunsSettled, Is.EqualTo(1));
            Assert.That(manager.CurrentMeta.SuccessfulRuns, Is.EqualTo(expectedSuccessfulRuns));
            Assert.That(manager.CurrentMeta.BossKills, Is.EqualTo(expectedBossKills));
            Assert.That(manager.CurrentMeta.TotalCurrency, Is.EqualTo(10));
        }

        [Test]
        public void ProcessedRunHistoryKeepsOnlyMostRecent64Ids()
        {
            var data = MetaSaveData.CreateDefault(DateTime.UtcNow);
            for (var index = 0; index < 70; index++)
            {
                data.ApplySettlement($"run-{index}", 0, false, false, DateTime.UtcNow);
            }

            Assert.That(data.RecentlyProcessedRunIds.Count, Is.EqualTo(64));
            Assert.That(data.RecentlyProcessedRunIds[0], Is.EqualTo("run-6"));
            Assert.That(data.RecentlyProcessedRunIds[63], Is.EqualTo("run-69"));
        }

        [Test]
        public void BothCorruptFilesFallBackToFreshDefaults()
        {
            var manager = new SaveManager(_temporaryDirectory);
            manager.Initialize();
            Assert.That(manager.AddTestCurrency(100), Is.True);
            File.WriteAllText(manager.SaveFilePath, "broken primary");
            File.WriteAllText(Path.Combine(_temporaryDirectory, SaveFileSerializer.BackupFileName), "broken backup");
            LogAssert.Expect(
                LogType.Exception,
                new Regex(@"Save file is not a JSON object: .*diveprotocol_save\.json"));
            LogAssert.Expect(
                LogType.Exception,
                new Regex(@"Save file is not a JSON object: .*diveprotocol_save\.backup\.json"));

            var recoveredManager = new SaveManager(_temporaryDirectory);
            Assert.DoesNotThrow(recoveredManager.Initialize);

            Assert.That(recoveredManager.CurrentMeta.TotalCurrency, Is.Zero);
            Assert.That(File.Exists(recoveredManager.SaveFilePath), Is.True);
            Assert.That(Directory.GetFiles(_temporaryDirectory, "*.corrupt.*").Length, Is.EqualTo(2));
        }

        [Test]
        public void DifferentRunIdsSettleIndependentlyAndClearResetsMeta()
        {
            var manager = new SaveManager(_temporaryDirectory);
            manager.Initialize();
            var first = CreateResult(RunEndReason.DemoCompleted, 100);
            var second = CreateResult(RunEndReason.PlayerDied, 200);

            Assert.That(manager.ApplyRunResult(first).Status, Is.EqualTo(RunResultApplyStatus.Applied));
            Assert.That(manager.ApplyRunResult(second).Status, Is.EqualTo(RunResultApplyStatus.Applied));
            Assert.That(manager.CurrentMeta.TotalCurrency, Is.EqualTo(30));
            Assert.That(manager.CurrentMeta.TotalRunsSettled, Is.EqualTo(2));
            Assert.That(manager.CurrentMeta.SuccessfulRuns, Is.EqualTo(1));

            Assert.That(manager.ClearMetaSave(), Is.True);
            Assert.That(manager.CurrentMeta.TotalCurrency, Is.Zero);
            Assert.That(manager.CurrentMeta.TotalRunsSettled, Is.Zero);
            Assert.That(manager.CurrentMeta.RecentlyProcessedRunIds, Is.Empty);
        }

        [Test]
        public void AbortedResultDoesNotRewriteSaveFile()
        {
            var manager = new SaveManager(_temporaryDirectory);
            manager.Initialize();
            var before = File.ReadAllText(manager.SaveFilePath);

            var outcome = manager.ApplyRunResult(CreateResult(RunEndReason.Aborted, 1000));
            var after = File.ReadAllText(manager.SaveFilePath);

            Assert.That(outcome.Status, Is.EqualTo(RunResultApplyStatus.NotEligible));
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void ReloadAndReadDoNotIncreaseCurrency()
        {
            var manager = new SaveManager(_temporaryDirectory);
            manager.Initialize();
            Assert.That(manager.AddTestCurrency(50), Is.True);
            var firstRead = manager.CurrentMeta.TotalCurrency;
            manager.ReloadMetaSave();
            var secondRead = manager.CurrentMeta.TotalCurrency;
            Assert.That(secondRead, Is.EqualTo(firstRead));
        }

        private static RunResult CreateResult(RunEndReason endReason, int bonusScore)
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                var run = RunFactory.Create(99, config);
                run.Score.AddBonusScore(bonusScore);
                return new RunResult(run, endReason, DateTime.UtcNow);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
