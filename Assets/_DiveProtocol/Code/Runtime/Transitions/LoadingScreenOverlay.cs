using System.Collections;
using System.Collections.Generic;
using DiveProtocol.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiveProtocol
{
    /// <summary>
    /// Persistent terminal-styled presentation used while an asynchronous scene load is in progress.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadingScreenOverlay : MonoBehaviour
    {
        private const string LoadingSceneName = "SCN_Loading";
        private const int ProgressSegments = 20;

        [Header("References")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _screenGroup;
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _terminalLogText;
        [SerializeField] private TMP_Text _tipText;
        [SerializeField] private TMP_Text _connectionText;
        [SerializeField] private RawImage _scanlineImage;

        [Header("Display Duration")]
        [SerializeField, Min(0f)] private float _minimumLoadingDuration = 3.5f;

        [Header("CRT Camera Binding")]
        [SerializeField, Min(0)] private int _crtRendererIndex = 1;

        [Header("Terminal Timing")]
        [SerializeField, Min(0.005f)] private float _typingSpeed = 0.035f;
        [SerializeField, Min(0f)] private float _lineDelay = 0.12f;
        [SerializeField, Min(0.05f)] private float _cursorBlinkInterval = 0.45f;

        private static LoadingScreenOverlay _instance;
        private Texture2D _scanlineTexture;
        private Coroutine _typingRoutine;
        private Coroutine _logRoutine;
        private Coroutine _delayedHideRoutine;
        private bool _hideAfterNextSceneActivation;
        private float _shownAt = -1f;
        private string _routeName;
        private string _routeDescription;

        /// <summary>Returns the current persistent overlay instance when one has been created.</summary>
        public static LoadingScreenOverlay Instance => _instance;

        /// <summary>Returns whether the overlay is currently presenting a scene load.</summary>
        public bool IsVisible => gameObject.activeInHierarchy;

        /// <summary>Minimum time that each loading presentation remains visible.</summary>
        public float MinimumLoadingDuration => _minimumLoadingDuration;

        /// <summary>Assigns the visual fields created by the loading-screen prefab.</summary>
        public void Configure(
            Canvas canvas,
            CanvasGroup screenGroup,
            TMP_Text loadingText,
            TMP_Text progressText,
            TMP_Text terminalLogText,
            TMP_Text tipText,
            TMP_Text connectionText,
            RawImage scanlineImage)
        {
            _canvas = canvas;
            _screenGroup = screenGroup;
            _loadingText = loadingText;
            _progressText = progressText;
            _terminalLogText = terminalLogText;
            _tipText = tipText;
            _connectionText = connectionText;
            _scanlineImage = scanlineImage;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += HandleSceneLoaded;
                BindToCurrentCrtCamera();
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            BindToCurrentCrtCamera();

            if (_connectionText != null)
            {
                bool cursorVisible =
                    Mathf.FloorToInt(Time.unscaledTime / _cursorBlinkInterval) % 2 == 0;
                _connectionText.text = cursorVisible
                    ? "CONNECTING... _"
                    : "CONNECTING...";
            }

            if (_scanlineImage != null)
            {
                float time = Time.unscaledTime;
                _scanlineImage.uvRect = new Rect(0f, time * 0.018f, 1f, 18f);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (_scanlineTexture != null)
            {
                Destroy(_scanlineTexture);
            }
        }

        /// <summary>Displays the overlay and restarts its terminal presentation.</summary>
        public void Show(string routeName, string routeDescription)
        {
            CancelDelayedHide();
            gameObject.SetActive(true);
            _hideAfterNextSceneActivation = false;
            _shownAt = Time.unscaledTime;
            _routeName = routeName;
            _routeDescription = routeDescription;

            if (_canvas != null)
            {
                _canvas.enabled = true;
            }

            BindToCurrentCrtCamera();

            if (_screenGroup != null)
            {
                _screenGroup.alpha = 1f;
            }

            EnsureScanlines();
            SetProgress(0f);

            if (_tipText != null)
            {
                _tipText.text = "TIP: Some doors can be opened before all threats are eliminated.";
            }

            RestartPresentation();
        }

        /// <summary>Updates the terminal progress indicator without modifying the active scene.</summary>
        public void SetProgress(float normalizedProgress)
        {
            if (_progressText == null)
            {
                return;
            }

            int filled = Mathf.RoundToInt(Mathf.Clamp01(normalizedProgress) * ProgressSegments);
            _progressText.text = new string('\u2588', filled) +
                                 new string('\u2591', ProgressSegments - filled);
        }

        /// <summary>Hides this overlay immediately once the target scene has become active.</summary>
        public void Hide()
        {
            _hideAfterNextSceneActivation = false;
            float remainingDuration = RemainingDisplayDuration;
            if (remainingDuration > 0f)
            {
                if (_delayedHideRoutine == null)
                {
                    _delayedHideRoutine = StartCoroutine(HideAfterMinimumDuration(remainingDuration));
                }

                return;
            }

            HideImmediately();
        }

        /// <summary>Keeps the presentation visible until a pending target scene replaces SCN_Loading.</summary>
        public void HideAfterNextSceneActivation()
        {
            _hideAfterNextSceneActivation = true;
        }

        private void RestartPresentation()
        {
            StopPresentation();
            _typingRoutine = StartCoroutine(TypeLoadingText());
            _logRoutine = StartCoroutine(TypeTerminalLog());
        }

        private void StopPresentation()
        {
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
                _typingRoutine = null;
            }

            if (_logRoutine != null)
            {
                StopCoroutine(_logRoutine);
                _logRoutine = null;
            }
        }

        private IEnumerator TypeLoadingText()
        {
            const string message = "LOADING FACILITY DATA";
            if (_loadingText == null)
            {
                yield break;
            }

            _loadingText.text = string.Empty;
            for (int i = 1; i <= message.Length; i++)
            {
                _loadingText.text = message.Substring(0, i);
                yield return new WaitForSecondsRealtime(_typingSpeed);
            }
        }

        private IEnumerator TypeTerminalLog()
        {
            if (_terminalLogText == null)
            {
                yield break;
            }

            var lines = new List<string>
            {
                "SYSTEM CHECK....... OK",
                "ARCHIVE LINK....... CONNECTING",
                "ENVIRONMENT DATA... LOADING",
                "FACILITY ACCESS.... WAITING"
            };

            if (!string.IsNullOrWhiteSpace(_routeName))
            {
                lines.Add($"ROUTE.............. {_routeName.ToUpperInvariant()}");
            }

            if (!string.IsNullOrWhiteSpace(_routeDescription))
            {
                lines.Add("CLEARANCE.......... CONFIRMED");
            }

            _terminalLogText.text = string.Empty;
            string completed = string.Empty;
            foreach (string line in lines)
            {
                for (int i = 1; i <= line.Length; i++)
                {
                    _terminalLogText.text = completed + line.Substring(0, i);
                    yield return new WaitForSecondsRealtime(_typingSpeed);
                }

                completed += line + "\n";
                _terminalLogText.text = completed;
                yield return new WaitForSecondsRealtime(_lineDelay);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindToCurrentCrtCamera();
            if (_hideAfterNextSceneActivation && scene.name != LoadingSceneName)
            {
                Hide();
            }
        }

        private float RemainingDisplayDuration => _shownAt < 0f
            ? 0f
            : Mathf.Max(0f, _minimumLoadingDuration - (Time.unscaledTime - _shownAt));

        private IEnumerator HideAfterMinimumDuration(float initialRemainingDuration)
        {
            yield return new WaitForSecondsRealtime(initialRemainingDuration);
            _delayedHideRoutine = null;
            HideImmediately();
        }

        private void HideImmediately()
        {
            CancelDelayedHide();
            StopPresentation();
            _shownAt = -1f;
            gameObject.SetActive(false);
        }

        private void CancelDelayedHide()
        {
            if (_delayedHideRoutine == null)
            {
                return;
            }

            StopCoroutine(_delayedHideRoutine);
            _delayedHideRoutine = null;
        }

        private void BindToCurrentCrtCamera()
        {
            if (_canvas == null || Camera.main == null)
            {
                return;
            }

            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = Camera.main;
            _canvas.planeDistance = 1f;

            if (SceneManager.GetActiveScene().name != LoadingSceneName)
            {
                return;
            }

            if (Camera.main.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                cameraData.SetRenderer(_crtRendererIndex);
            }
        }

        private void EnsureScanlines()
        {
            if (_scanlineImage == null || _scanlineImage.texture != null)
            {
                return;
            }

            _scanlineTexture = new Texture2D(1, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };
            _scanlineTexture.SetPixels(new[]
            {
                new Color(0f, 0f, 0f, 0.05f),
                new Color(0f, 0f, 0f, 0.22f),
                new Color(0f, 0f, 0f, 0.05f),
                new Color(0f, 0f, 0f, 0.15f)
            });
            _scanlineTexture.Apply(false, true);
            _scanlineImage.texture = _scanlineTexture;
        }
    }
}
