using NUnit.Framework;
using System.Linq;
using DiveProtocol.Builds;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class RunManagerTests
    {
        [Test]
        public void StartNewRunUsesSpecifiedSeedAndCreatesIndependentRuns()
        {
            WithRunManager((runManager, config) =>
            {
                Assert.That(runManager.CurrentRun, Is.Null);
                Assert.That(runManager.StartNewRun(456), Is.True);
                var first = runManager.CurrentRun;
                Assert.That(first.Status, Is.EqualTo(RunStatus.Active));
                Assert.That(first.Seed, Is.EqualTo(456));
                Assert.That(runManager.StartNewRun(789), Is.False);
                runManager.ClearRun();
                Assert.That(runManager.StartNewRun(789), Is.True);
                Assert.That(runManager.CurrentRun.RunId, Is.Not.EqualTo(first.RunId));
                Assert.That(runManager.CurrentRun.Player, Is.Not.SameAs(first.Player));
            });
        }

        [TestCase(RunEndReason.DemoCompleted)]
        [TestCase(RunEndReason.PlayerDied)]
        [TestCase(RunEndReason.Extracted)]
        [TestCase(RunEndReason.BossDefeated)]
        public void EligibleEndReasonCreatesOneResult(RunEndReason reason)
        {
            WithRunManager((runManager, config) =>
            {
                runManager.StartNewRun(123);
                var endedRun = runManager.CurrentRun;
                Assert.That(runManager.EndRun(reason), Is.True);
                Assert.That(runManager.LastResult, Is.Not.Null);
                Assert.That(runManager.LastResult.EndReason, Is.EqualTo(reason));
                Assert.That(runManager.EndRun(reason), Is.False);
                Assert.That(endedRun.EnterLevel("L99", 99), Is.False);
            });
        }

        [Test]
        public void AbortCurrentRunClearsStateWithoutCreatingResult()
        {
            WithRunManager((runManager, config) =>
            {
                Assert.That(runManager.StartNewRun(123), Is.True);
                Assert.That(runManager.AbortCurrentRun(), Is.True);
                Assert.That(runManager.CurrentRun, Is.Null);
                Assert.That(runManager.LastResult, Is.Null);
                Assert.That(runManager.AbortCurrentRun(), Is.False);
            });
        }

        [Test]
        public void EndRunAbortedSafelyUsesAbortPath()
        {
            WithRunManager((runManager, config) =>
            {
                runManager.StartNewRun(123);
                Assert.That(runManager.EndRun(RunEndReason.Aborted), Is.True);
                Assert.That(runManager.CurrentRun, Is.Null);
                Assert.That(runManager.LastResult, Is.Null);
            });
        }

        [Test]
        public void EndedAndClearedRunsDiscardTemporaryBuilds()
        {
            WithRunManager((runManager, config) =>
            {
                Assert.That(runManager.StartNewRun(123), Is.True);
                RunState endedRun = runManager.CurrentRun;
                endedRun.BuildState.GrantUpgrade(BuildUpgradeId.RedMarrow_Overdraft);

                Assert.That(runManager.EndRun(RunEndReason.PlayerDied), Is.True);
                Assert.That(endedRun.BuildState.OwnedUpgrades, Is.Empty);

                runManager.ClearRun();
                Assert.That(runManager.StartNewRun(456), Is.True);
                runManager.CurrentRun.BuildState.GrantUpgrade(BuildUpgradeId.OpticNerve_Calibration);
                runManager.ClearRun();
                Assert.That(runManager.CurrentRun, Is.Null);
            });
        }

        [Test]
        public void RunManagerHasNoStaticActiveRunStorage()
        {
            var staticRunFields = typeof(RunManager).GetFields(
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(staticRunFields.Any(field => field.FieldType == typeof(RunState)), Is.False);
        }

        private static void WithRunManager(System.Action<RunManager, GameConfig> test)
        {
            var gameObject = new GameObject("RunManagerTest");
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                var runManager = gameObject.AddComponent<RunManager>();
                runManager.Initialize(config);
                test(runManager, config);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(config);
            }
        }
    }
}
