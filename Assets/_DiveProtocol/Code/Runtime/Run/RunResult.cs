using System;

namespace DiveProtocol
{
    /// <summary>Immutable summary produced when a run ends.</summary>
    public sealed class RunResult
    {
        internal RunResult(RunState runState, RunEndReason endReason, DateTime completedAtUtc)
        {
            if (runState == null)
            {
                throw new ArgumentNullException(nameof(runState));
            }

            Seed = runState.Seed;
            RunId = runState.RunId;
            EndReason = endReason;
            Score = runState.Score.TotalScore;
            BossKilled = runState.Score.BossesDefeated > 0;
            CompletedAtUtc = completedAtUtc;
        }

        public int Seed { get; }
        public string RunId { get; }
        public RunEndReason EndReason { get; }
        public int Score { get; }
        public int TotalScore => Score;
        public bool BossKilled { get; }
        public DateTime CompletedAtUtc { get; }
    }
}
