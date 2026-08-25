using UnityEngine;
using UnityEngine.UI;

namespace DiveProtocol.UI
{
    /// <summary>Lightweight Canvas-based scanline, noise, flicker, and glitch treatment.</summary>
    [DisallowMultipleComponent]
    public sealed class TerminalCrtEffects : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _screenGroup;
        [SerializeField] private RawImage _noiseImage;
        [SerializeField] private Image _glitchLine;
        [SerializeField, Range(0f, 0.15f)] private float _flickerAmount = 0.025f;
        [SerializeField, Min(0f)] private float _flickerSpeed = 7f;
        [SerializeField, Range(0f, 0.2f)] private float _noiseOpacity = 0.045f;
        [Header("Signal Instability")]
        [SerializeField, Min(1f)] private float _minimumMalfunctionInterval = 60f;
        [SerializeField, Min(1f)] private float _maximumMalfunctionInterval = 180f;
        [SerializeField, Min(0.01f)] private float _minimumMalfunctionDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float _maximumMalfunctionDuration = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _malfunctionNoiseIncrease = 0.35f;
        [SerializeField, Range(0f, 0.5f)] private float _glitchLineOpacity = 0.12f;

        private Texture2D _noiseTexture;
        private float _glitchUntil;
        private float _nextMalfunctionTime;
        private Color _glitchLineColor;

        public void Configure(CanvasGroup screenGroup, RawImage noiseImage, Image glitchLine)
        {
            _screenGroup = screenGroup;
            _noiseImage = noiseImage;
            _glitchLine = glitchLine;
        }

        private void Awake()
        {
            CreateNoiseTexture();
            if (_glitchLine != null)
            {
                _glitchLineColor = _glitchLine.color;
                _glitchLine.enabled = false;
            }

            ScheduleNextMalfunction(Time.unscaledTime);
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            if (_screenGroup != null)
            {
                float wave = Mathf.PerlinNoise(time * _flickerSpeed, 0.31f) - 0.5f;
                _screenGroup.alpha = Mathf.Clamp01(1f + wave * _flickerAmount * 2f);
            }

            if (_noiseImage != null)
            {
                Color color = _noiseImage.color;
                bool malfunctioning = time < _glitchUntil;
                color.a = malfunctioning
                    ? Mathf.Clamp01(_noiseOpacity * (1f + _malfunctionNoiseIncrease))
                    : _noiseOpacity;
                _noiseImage.color = color;
                _noiseImage.uvRect = new Rect(time * 0.02f, time * 0.035f, 5f, 4f);
            }

            if (_glitchLine == null)
            {
                return;
            }

            if (time >= _nextMalfunctionTime)
            {
                RectTransform rect = _glitchLine.rectTransform;
                rect.anchoredPosition = new Vector2(Random.Range(-24f, 24f), Random.Range(-430f, 430f));
                _glitchUntil = time + Random.Range(_minimumMalfunctionDuration, Mathf.Max(_minimumMalfunctionDuration, _maximumMalfunctionDuration));
                Color color = _glitchLineColor;
                color.a *= _glitchLineOpacity;
                _glitchLine.color = color;
                _glitchLine.enabled = true;
                ScheduleNextMalfunction(time);
            }
            else if (time >= _glitchUntil)
            {
                _glitchLine.enabled = false;
            }
        }

        private void ScheduleNextMalfunction(float currentTime)
        {
            float maximumInterval = Mathf.Max(_minimumMalfunctionInterval, _maximumMalfunctionInterval);
            _nextMalfunctionTime = currentTime + Random.Range(_minimumMalfunctionInterval, maximumInterval);
        }

        private void OnDestroy()
        {
            if (_noiseTexture != null)
            {
                Destroy(_noiseTexture);
            }
        }

        private void CreateNoiseTexture()
        {
            if (_noiseImage == null || _noiseImage.texture != null)
            {
                return;
            }

            _noiseTexture = new Texture2D(48, 48, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };

            for (int y = 0; y < _noiseTexture.height; y++)
            {
                for (int x = 0; x < _noiseTexture.width; x++)
                {
                    byte value = (byte)Random.Range(90, 180);
                    _noiseTexture.SetPixel(x, y, new Color32(value, value, value, 255));
                }
            }

            _noiseTexture.Apply(false, true);
            _noiseImage.texture = _noiseTexture;
        }
    }
}
