#if UNITY_EDITOR || DEVELOPMENT_BUILD
using DiveProtocol.Builds;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiveProtocol
{
    /// <summary>Development-only IMGUI diagnostics toggled with F1.</summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        private const int _windowId = 51705;
        private const float _clearConfirmationDuration = 5f;
        private const float _windowMargin = 12f;
        private const float _minimumWindowWidth = 320f;
        private const float _maximumWindowWidth = 520f;
        private const float _maximumWindowHeight = 760f;

        private AppRoot _appRoot;
        private RunDebugCommands _commands;
        private Rect _windowRect = new Rect(_windowMargin, _windowMargin, 420f, 680f);
        private Vector2 _scrollPosition;
        private bool _isVisible;
        private float _clearConfirmationExpiresAt;
        private string _lastOperationResult = "No debug operation yet.";
        private CursorLockMode _previousCursorLockState;
        private bool _previousCursorVisible;
        private bool _hasSavedCursorState;

        internal void Initialize(AppRoot appRoot)
        {
            _appRoot = appRoot;
            _commands = new RunDebugCommands(appRoot);
        }

        private void OnGUI()
        {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F1)
            {
                SetVisible(!_isVisible);
                currentEvent.Use();
            }

            if (!_isVisible || _appRoot == null || _commands == null)
            {
                return;
            }

            _windowRect = CalculateWindowRect(_windowRect, Screen.width, Screen.height);
            _windowRect = GUI.Window(_windowId, _windowRect, DrawWindow, "DiveProtocol Debug (F1)");
        }

        private void DrawWindow(int windowId)
        {
            var run = _appRoot.RunManager.CurrentRun;
            var activeRun = run != null && run.IsActive;
            var meta = _appRoot.SaveManager.CurrentMeta;
            var headingStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            var wrappedLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.ExpandHeight(true));

            DrawHeading("Game State", headingStyle);
            GUILayout.Label(_appRoot.GameStateMachine.CurrentState.ToString());

            DrawHeading("Scene", headingStyle);
            GUILayout.Label(SceneManager.GetActiveScene().name);

            DrawHeading("Current / Last Run Information", headingStyle);
            GUILayout.Label($"Active Run: {activeRun}");

            if (run != null)
            {
                GUILayout.Label($"Seed: {run.Seed}");
                GUILayout.Label($"RunId: {run.RunId}");
                GUILayout.Label($"Level: {run.CurrentLevelId}");

                DrawHeading("Player State", headingStyle);
                GUILayout.Label($"Health: {run.Player.CurrentHealth} / {run.Player.MaxHealth}");
                GUILayout.Label($"Ammo: {run.Player.LoadedAmmo} / {run.Player.ReserveAmmo}");

                DrawHeading("Environment State", headingStyle);
                GUILayout.Label($"Corpse Activity: {run.Environment.CorpseActivity}");
                GUILayout.Label($"Resource Density: {run.Environment.ResourceDensity}");

                DrawHeading("Score", headingStyle);
                GUILayout.Label($"Total Score: {run.Score.TotalScore}");
            }
            else if (_appRoot.RunManager.LastResult != null)
            {
                var lastResult = _appRoot.RunManager.LastResult;
                GUILayout.Label($"Last RunId: {lastResult.RunId}");
                GUILayout.Label($"End Reason: {lastResult.EndReason}");
                GUILayout.Label($"Total Score: {lastResult.TotalScore}");
            }
            else
            {
                GUILayout.Label("No current or recent run.");
            }

            DrawHeading("Meta Save Information", headingStyle);
            if (meta != null)
            {
                GUILayout.Label($"Total Currency: {meta.TotalCurrency}");
                GUILayout.Label($"Total Runs Settled: {meta.TotalRunsSettled}");
                GUILayout.Label($"Successful Runs: {meta.SuccessfulRuns}");
                GUILayout.Label($"Boss Kills: {meta.BossKills}");
            }
            else
            {
                GUILayout.Label("SaveManager is unavailable.");
            }

            GUILayout.Label($"Save: {_appRoot.SaveManager.SaveFilePath}", wrappedLabelStyle);

            DrawHeading("Active Run Controls", headingStyle);
            if (!activeRun)
            {
                GUILayout.Label("No active run. Start a new run to use these controls.", wrappedLabelStyle);
            }

            var previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && activeRun && !_appRoot.SceneLoader.IsLoading;
            DrawActiveRunControls(run);
            GUI.enabled = previousEnabled;

            DrawHeading("Meta Save Controls", headingStyle);
            var metaControlsEnabled = _appRoot.SaveManager != null && _appRoot.SaveManager.IsInitialized;
            previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && metaControlsEnabled;
            if (DrawButton("Reload Meta Save"))
            {
                _lastOperationResult = _commands.ReloadMetaSave()
                    ? "Meta save reloaded."
                    : "SaveManager is unavailable.";
            }

            if (DrawButton($"Add Test Currency (+{RunDebugCommands.DebugCurrencyAmount})"))
            {
                _lastOperationResult = _commands.AddTestCurrency()
                    ? $"Added {RunDebugCommands.DebugCurrencyAmount} test currency."
                    : "Failed to add test currency.";
            }

            DrawHeading("Clear Save Confirmation", headingStyle);
            var confirmationActive = Time.realtimeSinceStartup <= _clearConfirmationExpiresAt;
            if (DrawButton(confirmationActive ? "CONFIRM Clear Meta Save" : "Clear Meta Save..."))
            {
                if (confirmationActive)
                {
                    _lastOperationResult = _commands.ClearMetaSave()
                        ? "Meta save cleared."
                        : "Failed to clear meta save.";
                    _clearConfirmationExpiresAt = 0f;
                }
                else
                {
                    _clearConfirmationExpiresAt = Time.realtimeSinceStartup + _clearConfirmationDuration;
                    _lastOperationResult = "Press CONFIRM Clear Meta Save within 5 seconds.";
                    Debug.Log("[Debug] Clear Meta Save requires a second confirmation within 5 seconds.");
                }
            }

            GUI.enabled = previousEnabled;

            DrawHeading("Last Debug Operation", headingStyle);
            GUILayout.Label(_lastOperationResult, wrappedLabelStyle);

            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        internal static Rect CalculateWindowRect(Rect currentRect, int screenWidth, int screenHeight)
        {
            var safeScreenWidth = Mathf.Max(1f, screenWidth);
            var safeScreenHeight = Mathf.Max(1f, screenHeight);
            var horizontalMargin = Mathf.Min(_windowMargin, safeScreenWidth * 0.25f);
            var verticalMargin = Mathf.Min(_windowMargin, safeScreenHeight * 0.25f);
            var availableWidth = Mathf.Max(1f, safeScreenWidth - horizontalMargin * 2f);
            var availableHeight = Mathf.Max(1f, safeScreenHeight - verticalMargin * 2f);

            var preferredWidth = Mathf.Clamp(safeScreenWidth * 0.42f, _minimumWindowWidth, _maximumWindowWidth);
            var width = Mathf.Min(preferredWidth, availableWidth);
            var height = Mathf.Min(_maximumWindowHeight, availableHeight);
            var maximumX = Mathf.Max(horizontalMargin, safeScreenWidth - horizontalMargin - width);
            var maximumY = Mathf.Max(verticalMargin, safeScreenHeight - verticalMargin - height);
            var x = Mathf.Clamp(currentRect.x, horizontalMargin, maximumX);
            var y = Mathf.Clamp(currentRect.y, verticalMargin, maximumY);
            return new Rect(x, y, width, height);
        }

        private void DrawActiveRunControls(RunState run)
        {
            var previousSeed = run?.Seed ?? 0;
            if (DrawButton("Restart Same Seed"))
            {
                _lastOperationResult = _commands.RestartSameSeed()
                    ? $"Restarted run with same seed: {previousSeed}"
                    : "Failed to restart run with the same seed.";
            }

            if (DrawButton("Restart New Seed"))
            {
                _lastOperationResult = _commands.RestartNewSeed()
                    ? $"Created run with new seed: {_appRoot.RunManager.CurrentRun?.Seed}"
                    : "Failed to create a run with a new seed.";
            }

            if (DrawButton("Heal Full"))
            {
                _lastOperationResult = _commands.HealFull() ? "Player healed to full." : "No active run.";
            }

            if (DrawButton("Add Ammo"))
            {
                _lastOperationResult = _commands.AddAmmo()
                    ? $"Added {RunDebugCommands.DebugAmmoAmount} reserve ammo."
                    : "No active run.";
            }

            if (DrawButton("Open Build Choice (Test)"))
            {
                PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
                _lastOperationResult = player != null &&
                                       BuildSelectionFlow.TryOpen(player.gameObject, 3)
                    ? "Build choice opened."
                    : "Could not open a build choice.";
            }

            if (DrawButton("Force Demo Complete"))
            {
                _lastOperationResult = _commands.ForceDemoComplete()
                    ? "Run completed and Results loading started."
                    : "Run has already ended or Results could not load.";
            }

            if (DrawButton("Force Player Death"))
            {
                _lastOperationResult = _commands.ForcePlayerDeath()
                    ? "Player death applied and Results loading started."
                    : "Run has already ended or Results could not load.";
            }

            if (DrawButton("Abort Current Run"))
            {
                _lastOperationResult = _commands.AbortCurrentRun()
                    ? "Run aborted. No rewards granted."
                    : "No active run or Main Menu could not load.";
            }
        }

        private static void DrawHeading(string title, GUIStyle style)
        {
            GUILayout.Space(5f);
            GUILayout.Label(title, style);
        }

        private static bool DrawButton(string label)
        {
            return GUILayout.Button(label, GUILayout.MinHeight(26f));
        }

        private void SetVisible(bool isVisible)
        {
            if (_isVisible == isVisible)
            {
                return;
            }

            _isVisible = isVisible;
            if (_isVisible)
            {
                _previousCursorLockState = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                _hasSavedCursorState = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                RestoreCursorState();
            }
        }

        private void RestoreCursorState()
        {
            if (!_hasSavedCursorState)
            {
                return;
            }

            Cursor.lockState = _previousCursorLockState;
            Cursor.visible = _previousCursorVisible;
            _hasSavedCursorState = false;
        }

        private void OnDisable()
        {
            RestoreCursorState();
            _isVisible = false;
        }

        private void OnDestroy()
        {
            RestoreCursorState();
            _isVisible = false;
        }
    }
}
#endif
