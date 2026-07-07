using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Shared low-key lighting profile for gameplay levels.</summary>
    [CreateAssetMenu(fileName = "SO_LevelLighting_Dark_Default", menuName = "Dive Protocol/Lighting/Level Lighting Profile", order = 81)]
    public sealed class LevelLightingProfile : ScriptableObject
    {
        [SerializeField] private Color _ambientLightColor = new(0.035f, 0.045f, 0.055f, 1f);
        [SerializeField, Range(0f, 2f)] private float _ambientIntensity = 0.45f;
        [SerializeField] private bool _skyboxEnabled;
        [SerializeField] private Color _cameraBackgroundColor = new(0.005f, 0.006f, 0.008f, 1f);
        [SerializeField] private bool _fogEnabled = true;
        [SerializeField] private Color _fogColor = new(0.015f, 0.018f, 0.022f, 1f);
        [SerializeField, Min(0f)] private float _fogDensity = 0.018f;
        [SerializeField] private float _defaultExposure;
        [SerializeField, Range(0f, 2f)] private float _environmentLightIntensityMultiplier = 0.35f;
        [SerializeField, Min(0f)] private float _shadowDistance = 35f;
        [SerializeField] private FlashlightConfig _playerFlashlightProfile;

        public Color AmbientLightColor => _ambientLightColor;
        public float AmbientIntensity => _ambientIntensity;
        public bool SkyboxEnabled => _skyboxEnabled;
        public Color CameraBackgroundColor => _cameraBackgroundColor;
        public bool FogEnabled => _fogEnabled;
        public Color FogColor => _fogColor;
        public float FogDensity => _fogDensity;
        public float DefaultExposure => _defaultExposure;
        public float EnvironmentLightIntensityMultiplier => _environmentLightIntensityMultiplier;
        public float ShadowDistance => _shadowDistance;
        public FlashlightConfig PlayerFlashlightProfile => _playerFlashlightProfile;

        public void ConfigureFlashlightProfile(FlashlightConfig profile)
        {
            _playerFlashlightProfile = profile;
        }
    }
}
