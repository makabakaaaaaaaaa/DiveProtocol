using System;
using System.IO;
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Owns loading, mutation, deduplication, and automatic persistence of meta progression.</summary>
    public sealed class SaveManager
    {
        private readonly SaveFileSerializer _serializer;
        private readonly Func<DateTime> _utcNowProvider;
        private bool _hasPendingChanges;
        private bool _isReadOnlyForNewerVersion;

        public SaveManager(string saveDirectory, Func<DateTime> utcNowProvider = null)
        {
            _serializer = new SaveFileSerializer(saveDirectory);
            _utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
        }

        public MetaSaveData CurrentMeta { get; private set; }
        public string SaveFilePath => _serializer.SaveFilePath;
        public bool IsInitialized { get; private set; }

        /// <summary>Loads the single save, recovers a backup, or creates safe defaults.</summary>
        public void Initialize()
        {
            IsInitialized = false;
            _hasPendingChanges = false;
            _isReadOnlyForNewerVersion = false;

            if (TryLoadPrimary())
            {
                IsInitialized = true;
                return;
            }

            if (TryLoadBackup())
            {
                IsInitialized = true;
                if (!_isReadOnlyForNewerVersion)
                {
                    TrySaveCurrent("restored backup");
                }

                return;
            }

            CurrentMeta = MetaSaveData.CreateDefault(_utcNowProvider());
            IsInitialized = true;
            _hasPendingChanges = true;
            Debug.LogWarning("[Save] No valid meta save was found. Created default meta progression; active RunState is never persisted.");
            TrySaveCurrent("default save creation");
        }

        /// <summary>Applies one eligible result exactly once and automatically saves it.</summary>
        public RunResultApplyOutcome ApplyRunResult(RunResult runResult)
        {
            if (!IsInitialized || CurrentMeta == null || runResult == null || string.IsNullOrWhiteSpace(runResult.RunId))
            {
                Debug.LogError("[Meta] Cannot apply an invalid RunResult or use an uninitialized SaveManager.");
                return new RunResultApplyOutcome(RunResultApplyStatus.InvalidResult, 0);
            }

            if (!MetaRewardCalculator.IsEligible(runResult.EndReason))
            {
                Debug.LogWarning($"[Meta] RunResult {runResult.RunId} is not eligible for settlement: {runResult.EndReason}.");
                return new RunResultApplyOutcome(RunResultApplyStatus.NotEligible, 0);
            }

            if (CurrentMeta.HasProcessedRun(runResult.RunId))
            {
                Debug.LogWarning($"[Meta] Rejected duplicate RunResult: {runResult.RunId}.");
                return new RunResultApplyOutcome(RunResultApplyStatus.AlreadyProcessed, 0);
            }

            if (_isReadOnlyForNewerVersion)
            {
                Debug.LogError("[Save] Meta save is from a newer version and is read-only in this build.");
                return new RunResultApplyOutcome(RunResultApplyStatus.SaveUnavailable, 0);
            }

            var currencyGained = MetaRewardCalculator.CalculateCurrency(runResult.TotalScore);
            var snapshot = CurrentMeta.Clone();
            CurrentMeta.ApplySettlement(
                runResult.RunId,
                currencyGained,
                MetaRewardCalculator.IsSuccessful(runResult.EndReason),
                runResult.EndReason == RunEndReason.BossDefeated,
                _utcNowProvider());
            _hasPendingChanges = true;

            if (!TrySaveCurrent($"RunResult settlement {runResult.RunId}"))
            {
                CurrentMeta = snapshot;
                _hasPendingChanges = false;
                return new RunResultApplyOutcome(RunResultApplyStatus.SaveUnavailable, 0);
            }

            Debug.Log($"[Meta] Applied RunResult {runResult.RunId}: +{currencyGained} currency.");
            return new RunResultApplyOutcome(RunResultApplyStatus.Applied, currencyGained);
        }

        /// <summary>Adds development currency and immediately saves it.</summary>
        public bool AddTestCurrency(int amount)
        {
            if (!CanMutate() || amount <= 0)
            {
                return false;
            }

            var snapshot = CurrentMeta.Clone();
            CurrentMeta.AddCurrency(amount, _utcNowProvider());
            _hasPendingChanges = true;
            if (TrySaveCurrent("debug currency change"))
            {
                Debug.Log($"[Debug] Added {amount} test currency.");
                return true;
            }

            CurrentMeta = snapshot;
            _hasPendingChanges = false;
            return false;
        }

        /// <summary>Reloads the single meta save without touching the current in-memory run.</summary>
        public void ReloadMetaSave()
        {
            Initialize();
            Debug.Log("[Debug] Reloaded meta save. Active RunState was not read from or written to disk.");
        }

        /// <summary>Deletes the development save files and writes fresh defaults.</summary>
        public bool ClearMetaSave()
        {
            if (_isReadOnlyForNewerVersion)
            {
                Debug.LogError("[Save] Refusing to clear a newer-version save from this build.");
                return false;
            }

            try
            {
                _serializer.DeleteSaveFiles();
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                Debug.LogError($"[Save] Failed to clear meta save files: {exception.Message}");
                Debug.LogException(exception);
                return false;
            }

            CurrentMeta = MetaSaveData.CreateDefault(_utcNowProvider());
            IsInitialized = true;
            _hasPendingChanges = true;
            var saved = TrySaveCurrent("debug clear meta save");
            Debug.Log(saved ? "[Debug] Cleared meta save." : "[Debug] Meta save was reset in memory but could not be written.");
            return saved;
        }

        /// <summary>Saves only pending meta changes; it never settles or serializes an active run.</summary>
        public void FlushPendingMetaChanges()
        {
            if (_hasPendingChanges)
            {
                TrySaveCurrent("application shutdown meta flush");
            }

            Debug.Log("[Save] Active RunState is not persisted on application shutdown.");
        }

        private bool TryLoadPrimary()
        {
            if (!File.Exists(_serializer.SaveFilePath))
            {
                return false;
            }

            try
            {
                CurrentMeta = _serializer.Load(_serializer.SaveFilePath);
                PrepareLoadedData("primary save");
                Debug.Log($"[Save] Loaded meta save: {_serializer.SaveFilePath}");
                return true;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                LogLoadFailure("primary save", _serializer.SaveFilePath, exception);
                PreserveCorruptFile(_serializer.SaveFilePath);
                return false;
            }
        }

        private bool TryLoadBackup()
        {
            if (!File.Exists(_serializer.BackupFilePath))
            {
                return false;
            }

            try
            {
                CurrentMeta = _serializer.Load(_serializer.BackupFilePath);
                PrepareLoadedData("backup save");
                Debug.LogWarning($"[Save] Recovered meta progression from backup: {_serializer.BackupFilePath}");
                return true;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                LogLoadFailure("backup save", _serializer.BackupFilePath, exception);
                PreserveCorruptFile(_serializer.BackupFilePath);
                return false;
            }
        }

        private void PrepareLoadedData(string source)
        {
            if (CurrentMeta.SaveVersion > SaveVersion.Current)
            {
                _isReadOnlyForNewerVersion = true;
                Debug.LogWarning(
                    $"[Save] {source} version {CurrentMeta.SaveVersion} is newer than supported version {SaveVersion.Current}. It will not be overwritten.");
                return;
            }

            if (CurrentMeta.SaveVersion < SaveVersion.Current)
            {
                MigrateToCurrentVersion(CurrentMeta);
                _hasPendingChanges = true;
                TrySaveCurrent($"migration from {source}");
            }
        }

        private static void MigrateToCurrentVersion(MetaSaveData saveData)
        {
            if (saveData.SaveVersion <= 0)
            {
                Debug.LogWarning("[Save] Save version was missing; treating data as version 0 and migrating to version 1.");
                saveData.SetVersion(1);
            }

            // Future explicit steps belong here, for example MigrateFromVersion1ToVersion2.
            if (saveData.SaveVersion != SaveVersion.Current)
            {
                throw new InvalidDataException(
                    $"No migration path exists from save version {saveData.SaveVersion} to {SaveVersion.Current}.");
            }
        }

        private bool TrySaveCurrent(string reason)
        {
            if (CurrentMeta == null || _isReadOnlyForNewerVersion)
            {
                return false;
            }

            try
            {
                _serializer.Save(CurrentMeta);
                _hasPendingChanges = false;
                Debug.Log($"[Save] Saved meta progression ({reason}): {_serializer.SaveFilePath}");
                return true;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                _hasPendingChanges = true;
                Debug.LogError($"[Save] Failed to write meta progression ({reason}): {exception.Message}");
                Debug.LogException(exception);
                return false;
            }
        }

        private bool CanMutate()
        {
            if (!IsInitialized || CurrentMeta == null || _isReadOnlyForNewerVersion)
            {
                Debug.LogError("[Save] Meta progression is unavailable or read-only.");
                return false;
            }

            return true;
        }

        private void PreserveCorruptFile(string filePath)
        {
            try
            {
                var preservedPath = _serializer.PreserveCorruptFile(filePath, _utcNowProvider());
                if (!string.IsNullOrEmpty(preservedPath))
                {
                    Debug.LogWarning($"[Save] Preserved corrupt save as: {preservedPath}");
                }
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                Debug.LogError($"[Save] Could not preserve corrupt file '{filePath}': {exception.Message}");
                Debug.LogException(exception);
            }
        }

        private static void LogLoadFailure(string label, string filePath, Exception exception)
        {
            Debug.LogWarning($"[Save] Failed to load {label} '{filePath}': {exception.Message}");
            Debug.LogException(exception);
        }

        private static bool IsExpectedFileException(Exception exception)
        {
            return exception is IOException ||
                   exception is InvalidDataException ||
                   exception is UnauthorizedAccessException ||
                   exception is ArgumentException ||
                   exception is NotSupportedException;
        }
    }
}
