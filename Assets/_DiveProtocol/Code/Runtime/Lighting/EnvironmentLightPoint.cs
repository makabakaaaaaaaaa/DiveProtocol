using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Reusable authored scene light with optional simple flicker.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class EnvironmentLightPoint : MonoBehaviour
    {
        [SerializeField] private string _lightId = "LIGHT_001";
        [SerializeField] private EnvironmentLightType _lightType = EnvironmentLightType.WallLight;
        [SerializeField, Min(0f)] private float _intensity = 1.2f;
        [SerializeField, Min(0.1f)] private float _range = 7f;
        [SerializeField] private Color _color = new(1f, 0.85f, 0.62f, 1f);
        [SerializeField, Range(1f, 120f)] private float _spotAngle = 55f;
        [SerializeField] private bool _castShadows = true;
        [SerializeField] private bool _startEnabled = true;
        [SerializeField] private bool _flickerEnabled;
        [SerializeField, Range(0f, 1f)] private float _flickerAmount = 0.12f;
        [SerializeField, Min(0f)] private float _flickerSpeed = 7f;
        [SerializeField] private bool _criticalNavigationLight;

        private Light _light;
        private float _baseIntensity;

        public string LightId => _lightId;
        public EnvironmentLightType LightType => _lightType;
        public bool CriticalNavigationLight => _criticalNavigationLight;

        private void Awake()
        {
            _light = GetComponent<Light>();
            ApplySettings();
        }

        private void Update()
        {
            if (_light == null || !_flickerEnabled || !_light.enabled)
            {
                return;
            }

            var noise = Mathf.PerlinNoise(Time.time * _flickerSpeed, transform.position.sqrMagnitude);
            var multiplier = 1f + (noise - 0.5f) * 2f * _flickerAmount;
            _light.intensity = _baseIntensity * multiplier;
        }

        public void Configure(string lightId, EnvironmentLightType type)
        {
            _lightId = string.IsNullOrWhiteSpace(lightId) ? _lightId : lightId.Trim();
            _lightType = type;
            ApplySettings();
        }

        public void ApplySettings()
        {
            if (_light == null)
            {
                _light = GetComponent<Light>();
            }

            _baseIntensity = Mathf.Max(0f, _intensity);
            _light.intensity = _baseIntensity;
            _light.range = Mathf.Max(0.1f, _range);
            _light.color = _color;
            _light.spotAngle = Mathf.Clamp(_spotAngle, 1f, 120f);
            _light.shadows = _castShadows ? LightShadows.Soft : LightShadows.None;
            _light.enabled = _startEnabled;
        }
    }
}
