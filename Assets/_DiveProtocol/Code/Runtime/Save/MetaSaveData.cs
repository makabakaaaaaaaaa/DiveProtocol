using System;
using System.Collections.Generic;

namespace DiveProtocol
{
    /// <summary>Permanent progression only; it never contains resumable run state.</summary>
    [Serializable]
    public sealed class MetaSaveData
    {
        public const int MaxRecentlyProcessedRunIds = 64;

        private readonly List<string> _recentlyProcessedRunIds;

        private MetaSaveData(
            int saveVersion,
            int totalCurrency,
            int totalRunsSettled,
            int successfulRuns,
            int bossKills,
            IEnumerable<string> recentlyProcessedRunIds,
            string lastUpdatedUtc)
        {
            SaveVersion = saveVersion;
            TotalCurrency = Math.Max(0, totalCurrency);
            TotalRunsSettled = Math.Max(0, totalRunsSettled);
            SuccessfulRuns = Math.Max(0, successfulRuns);
            BossKills = Math.Max(0, bossKills);
            LastUpdatedUtc = lastUpdatedUtc ?? string.Empty;
            _recentlyProcessedRunIds = new List<string>();

            if (recentlyProcessedRunIds != null)
            {
                foreach (var runId in recentlyProcessedRunIds)
                {
                    AddProcessedRunId(runId);
                }
            }
        }

        public int SaveVersion { get; private set; }
        public int TotalCurrency { get; private set; }
        public int TotalRunsSettled { get; private set; }
        public int SuccessfulRuns { get; private set; }
        public int BossKills { get; private set; }
        public IReadOnlyList<string> RecentlyProcessedRunIds => _recentlyProcessedRunIds;
        public string LastUpdatedUtc { get; private set; }

        internal static MetaSaveData CreateDefault(DateTime utcNow)
        {
            return new MetaSaveData(DiveProtocol.SaveVersion.Current, 0, 0, 0, 0, null, utcNow.ToString("O"));
        }

        internal static MetaSaveData Restore(
            int saveVersion,
            int totalCurrency,
            int totalRunsSettled,
            int successfulRuns,
            int bossKills,
            IEnumerable<string> recentlyProcessedRunIds,
            string lastUpdatedUtc)
        {
            return new MetaSaveData(
                saveVersion,
                totalCurrency,
                totalRunsSettled,
                successfulRuns,
                bossKills,
                recentlyProcessedRunIds,
                lastUpdatedUtc);
        }

        internal MetaSaveData Clone()
        {
            return Restore(
                SaveVersion,
                TotalCurrency,
                TotalRunsSettled,
                SuccessfulRuns,
                BossKills,
                _recentlyProcessedRunIds,
                LastUpdatedUtc);
        }

        internal bool HasProcessedRun(string runId)
        {
            return !string.IsNullOrWhiteSpace(runId) && _recentlyProcessedRunIds.Contains(runId.Trim());
        }

        internal void ApplySettlement(string runId, int currencyGained, bool wasSuccessful, bool killedBoss, DateTime utcNow)
        {
            TotalCurrency = SaturatingAdd(TotalCurrency, Math.Max(0, currencyGained));
            TotalRunsSettled = SaturatingAdd(TotalRunsSettled, 1);
            if (wasSuccessful)
            {
                SuccessfulRuns = SaturatingAdd(SuccessfulRuns, 1);
            }

            if (killedBoss)
            {
                BossKills = SaturatingAdd(BossKills, 1);
            }

            AddProcessedRunId(runId);
            LastUpdatedUtc = utcNow.ToString("O");
        }

        internal void AddCurrency(int amount, DateTime utcNow)
        {
            if (amount <= 0)
            {
                return;
            }

            TotalCurrency = SaturatingAdd(TotalCurrency, amount);
            LastUpdatedUtc = utcNow.ToString("O");
        }

        internal void SetVersion(int version)
        {
            SaveVersion = version;
        }

        private void AddProcessedRunId(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                return;
            }

            var normalizedRunId = runId.Trim();
            if (_recentlyProcessedRunIds.Contains(normalizedRunId))
            {
                return;
            }

            _recentlyProcessedRunIds.Add(normalizedRunId);
            while (_recentlyProcessedRunIds.Count > MaxRecentlyProcessedRunIds)
            {
                _recentlyProcessedRunIds.RemoveAt(0);
            }
        }

        private static int SaturatingAdd(int currentValue, int amount)
        {
            return (int)Math.Min((long)currentValue + amount, int.MaxValue);
        }
    }
}
