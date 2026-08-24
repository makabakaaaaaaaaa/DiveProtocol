using System;
using DiveProtocol.Builds;

namespace DiveProtocol
{
    /// <summary>Aggregate root for all transient state owned by one run.</summary>
    [Serializable]
    public sealed class RunState
    {
        internal RunState(
            int seed,
            string runId,
            string initialLevelId,
            int initialLevelIndex,
            DateTime startedAtUtc,
            PlayerRuntimeState player,
            EnvironmentState environment,
            InventoryState inventory,
            ScoreState score,
            RunBuildState buildState)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                throw new ArgumentException("Run ID cannot be blank.", nameof(runId));
            }

            if (string.IsNullOrWhiteSpace(initialLevelId))
            {
                throw new ArgumentException("Initial level ID cannot be blank.", nameof(initialLevelId));
            }

            if (initialLevelIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialLevelIndex));
            }

            Seed = seed;
            RunId = runId;
            Status = RunStatus.Active;
            CurrentLevelId = initialLevelId;
            CurrentLevelIndex = initialLevelIndex;
            StartedAtUtc = startedAtUtc;
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Score = score ?? throw new ArgumentNullException(nameof(score));
            BuildState = buildState ?? throw new ArgumentNullException(nameof(buildState));
        }

        public int Seed { get; }
        public string RunId { get; }
        public RunStatus Status { get; private set; }
        public bool IsActive => Status == RunStatus.Active;
        public string CurrentLevelId { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public DateTime StartedAtUtc { get; }
        public PlayerRuntimeState Player { get; }
        public EnvironmentState Environment { get; }
        public InventoryState Inventory { get; }
        public ScoreState Score { get; }
        public RunBuildState BuildState { get; }

        /// <summary>Moves an active run to a specified level.</summary>
        public bool EnterLevel(string levelId, int levelIndex)
        {
            if (!IsActive || string.IsNullOrWhiteSpace(levelId) || levelIndex < 0)
            {
                return false;
            }

            CurrentLevelId = levelId.Trim();
            CurrentLevelIndex = levelIndex;
            return true;
        }

        public bool Complete() => TrySetEndedStatus(RunStatus.Completed);
        public bool Fail() => TrySetEndedStatus(RunStatus.Failed);
        public bool Extract() => TrySetEndedStatus(RunStatus.Extracted);
        public bool Abort() => TrySetEndedStatus(RunStatus.Aborted);

        private bool TrySetEndedStatus(RunStatus endedStatus)
        {
            if (!IsActive || endedStatus == RunStatus.None || endedStatus == RunStatus.Active)
            {
                return false;
            }

            Status = endedStatus;
            return true;
        }
    }
}
