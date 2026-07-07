#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Development-only commands routed through existing runtime services.</summary>
    public sealed class RunDebugCommands
    {
        public const int DebugAmmoAmount = 12;
        public const int DebugCurrencyAmount = 1000;

        private readonly AppRoot _appRoot;

        public RunDebugCommands(AppRoot appRoot)
        {
            _appRoot = appRoot;
        }

        public bool RestartSameSeed()
        {
            var run = _appRoot.RunManager.CurrentRun;
            if (run == null || !run.IsActive)
            {
                return false;
            }

            var seed = run.Seed;
            _appRoot.RunManager.ClearRun();
            if (!_appRoot.RunManager.StartNewRun(seed))
            {
                return false;
            }

            return LoadDemoOrClearRun("same seed");
        }

        public bool RestartNewSeed()
        {
            var run = _appRoot.RunManager.CurrentRun;
            if (run == null || !run.IsActive)
            {
                return false;
            }

            _appRoot.RunManager.ClearRun();
            if (!_appRoot.RunManager.StartNewRun())
            {
                return false;
            }

            return LoadDemoOrClearRun("new seed");
        }

        public bool HealFull()
        {
            var run = _appRoot.RunManager.CurrentRun;
            if (run == null || !run.IsActive)
            {
                return false;
            }

            run.Player.Heal(run.Player.MaxHealth);
            Debug.Log("[Debug] Restored player health.");
            return true;
        }

        public bool AddAmmo()
        {
            var run = _appRoot.RunManager.CurrentRun;
            if (run == null || !run.IsActive)
            {
                return false;
            }

            run.Player.AddReserveAmmo(DebugAmmoAmount);
            Debug.Log($"[Debug] Added {DebugAmmoAmount} reserve ammo.");
            return true;
        }

        public bool ForceDemoComplete()
        {
            return EndRunAndLoadResults(RunEndReason.DemoCompleted);
        }

        public bool ForcePlayerDeath()
        {
            return EndRunAndLoadResults(RunEndReason.PlayerDied);
        }

        public bool AbortCurrentRun()
        {
            if (!_appRoot.RunManager.AbortCurrentRun())
            {
                return false;
            }

            if (!_appRoot.SceneLoader.LoadScene(SceneNames.MainMenu, GameState.MainMenu))
            {
                Debug.LogError("[Debug] Run was aborted, but Main Menu could not be loaded.");
                return false;
            }

            return true;
        }

        public bool ReloadMetaSave()
        {
            if (_appRoot.SaveManager == null)
            {
                return false;
            }

            _appRoot.SaveManager.ReloadMetaSave();
            return _appRoot.SaveManager.IsInitialized;
        }

        public bool ClearMetaSave()
        {
            return _appRoot.SaveManager.ClearMetaSave();
        }

        public bool AddTestCurrency()
        {
            return _appRoot.SaveManager.AddTestCurrency(DebugCurrencyAmount);
        }

        private bool EndRunAndLoadResults(RunEndReason endReason)
        {
            if (!_appRoot.RunManager.EndRun(endReason))
            {
                return false;
            }

            if (!_appRoot.SceneLoader.LoadScene(SceneNames.Results, GameState.Results))
            {
                Debug.LogError($"[Debug] Run ended with {endReason}, but Results could not be loaded.");
                return false;
            }

            return true;
        }

        private bool LoadDemoOrClearRun(string label)
        {
            if (_appRoot.SceneLoader.LoadScene(SceneNames.DemoLevel, GameState.InRun))
            {
                Debug.Log($"[Debug] Restarted run with {label}.");
                return true;
            }

            _appRoot.RunManager.ClearRun();
            return false;
        }
    }
}
#endif
