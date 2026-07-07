using UnityEngine;
using UnityEngine.Rendering;

namespace DiveProtocol
{
    /// <summary>Applies the shared dark lighting profile to a gameplay scene.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelLightingController : MonoBehaviour
    {
        [SerializeField] private LevelLightingProfile _profile;
        [SerializeField] private Camera _targetCamera;

        public LevelLightingProfile Profile => _profile;

        private void Start()
        {
            ApplyProfile();
        }

        public void Configure(LevelLightingProfile profile, Camera targetCamera)
        {
            _profile = profile != null ? profile : _profile;
            _targetCamera = targetCamera != null ? targetCamera : _targetCamera;
        }

        public void ApplyProfile()
        {
            if (_profile == null)
            {
                Debug.LogWarning("[Lighting] LevelLightingController has no profile.");
                return;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _profile.AmbientLightColor * _profile.AmbientIntensity;
            RenderSettings.fog = _profile.FogEnabled;
            RenderSettings.fogColor = _profile.FogColor;
            RenderSettings.fogDensity = _profile.FogDensity;
            RenderSettings.skybox = _profile.SkyboxEnabled ? RenderSettings.skybox : null;

            QualitySettings.shadowDistance = _profile.ShadowDistance;

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera != null)
            {
                _targetCamera.clearFlags = CameraClearFlags.SolidColor;
                _targetCamera.backgroundColor = _profile.CameraBackgroundColor;
            }
        }
    }
}
