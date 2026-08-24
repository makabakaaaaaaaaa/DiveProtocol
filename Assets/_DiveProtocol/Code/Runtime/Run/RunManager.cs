using System;
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Owns transient state for the current run and its latest result.</summary>
    public sealed class RunManager : MonoBehaviour
    {
        public const string AbortConfirmationMessage = "当前下潜进度不会保存。确定放弃本局并返回主菜单吗？";

        [SerializeField] private GameConfig _gameConfig;

        public RunState CurrentRun { get; private set; }
        public RunResult LastResult { get; private set; }
        public string StartingLevelSceneName => _gameConfig != null
            ? _gameConfig.StartingLevelSceneName
            : SceneNames.Level01Drainage;

        internal void Initialize(GameConfig gameConfig)
        {
            _gameConfig = gameConfig;
        }

        /// <summary>Starts a new run if no active run exists.</summary>
        public bool StartNewRun()
        {
            var seedBytes = Guid.NewGuid().ToByteArray();
            var seed = BitConverter.ToInt32(seedBytes, 0) & int.MaxValue;
            return StartNewRun(seed);
        }

        /// <summary>Starts a deterministic-seed run for debugging and tests.</summary>
        public bool StartNewRun(int seed)
        {
            if (CurrentRun != null && CurrentRun.IsActive)
            {
                Debug.LogWarning("Cannot start a new run while another run is active.");
                return false;
            }

            if (_gameConfig == null)
            {
                Debug.LogError("RunManager requires a GameConfig. Create the default config and rebuild system scenes.");
                return false;
            }

            CurrentRun = RunFactory.Create(seed, _gameConfig);
            LastResult = null;
            return true;
        }

        /// <summary>Ends the active run exactly once and creates its result.</summary>
        public bool EndRun(RunEndReason endReason)
        {
            if (endReason == RunEndReason.Aborted)
            {
                return AbortCurrentRun();
            }

            if (CurrentRun == null || !CurrentRun.IsActive)
            {
                Debug.LogWarning("Cannot end a run because there is no active run.");
                return false;
            }

            if (endReason == RunEndReason.BossDefeated)
            {
                CurrentRun.Score.RecordBossDefeated();
            }

            if (!TryApplyEndStatus(CurrentRun, endReason))
            {
                Debug.LogError($"Could not apply end reason {endReason} to the active run.");
                return false;
            }

            LastResult = new RunResult(CurrentRun, endReason, DateTime.UtcNow);
            CurrentRun.BuildState.Clear();
            return true;
        }

        /// <summary>Abandons the active run without producing a reward-bearing result.</summary>
        public bool AbortCurrentRun()
        {
            if (CurrentRun == null || !CurrentRun.IsActive)
            {
                Debug.LogWarning("[Run] Cannot abort because there is no active run.");
                return false;
            }

            var runId = CurrentRun.RunId;
            CurrentRun.BuildState.Clear();
            if (!CurrentRun.Abort())
            {
                Debug.LogError($"[Run] Failed to mark run {runId} as aborted.");
                return false;
            }

            CurrentRun = null;
            LastResult = null;
            Debug.Log($"[Run] Aborted run {runId}. No result, reward, or RunState save was created.");
            return true;
        }

        /// <summary>Clears all transient current-run and result data.</summary>
        public void ClearRun()
        {
            CurrentRun?.BuildState.Clear();
            CurrentRun = null;
            LastResult = null;
        }

        /// <summary>
        /// Clears the active run's temporary builds without changing the surrounding run flow.
        /// </summary>
        public void ClearCurrentRunBuilds()
        {
            CurrentRun?.BuildState.Clear();
        }

        private static bool TryApplyEndStatus(RunState runState, RunEndReason endReason)
        {
            switch (endReason)
            {
                case RunEndReason.DemoCompleted:
                case RunEndReason.BossDefeated:
                    return runState.Complete();
                case RunEndReason.PlayerDied:
                    return runState.Fail();
                case RunEndReason.Extracted:
                    return runState.Extract();
                default:
                    return false;
            }
        }
    }
}
