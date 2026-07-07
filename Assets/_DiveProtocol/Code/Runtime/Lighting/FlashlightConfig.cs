using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Static tuning for the player's flashlight.</summary>
    [CreateAssetMenu(fileName = "SO_Flashlight_Default", menuName = "Dive Protocol/Lighting/Flashlight Config", order = 80)]
    public sealed class FlashlightConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _intensity = 4.5f;
        [SerializeField, Min(0.1f)] private float _range = 12f;
        [SerializeField, Range(1f, 120f)] private float _spotAngle = 48f;
        [SerializeField, Range(1f, 120f)] private float _innerSpotAngle = 28f;
        [SerializeField] private Color _color = new(1f, 0.93f, 0.82f, 1f);
        [SerializeField] private LightShadows _shadowType = LightShadows.Soft;
        [SerializeField, Range(0f, 1f)] private float _shadowStrength = 0.75f;
        [SerializeField] private Vector3 _localPosition = new(0f, 1.15f, 0.35f);
        [SerializeField] private Vector3 _localRotation = new(12f, 0f, 0f);
        [SerializeField, Min(0f)] private float _rotationSpeed = 720f;
        [SerializeField] private bool _enableOnStart = true;
        [SerializeField] private bool _allowToggle = true;
        [SerializeField] private KeyCode _toggleInput = KeyCode.F;
        [SerializeField] private bool _debugGizmo = true;

        public float Intensity => _intensity;
        public float Range => _range;
        public float SpotAngle => _spotAngle;
        public float InnerSpotAngle => _innerSpotAngle;
        public Color Color => _color;
        public LightShadows ShadowType => _shadowType;
        public float ShadowStrength => _shadowStrength;
        public Vector3 LocalPosition => _localPosition;
        public Vector3 LocalRotation => _localRotation;
        public float RotationSpeed => _rotationSpeed;
        public bool EnableOnStart => _enableOnStart;
        public bool AllowToggle => _allowToggle;
        public KeyCode ToggleInput => _toggleInput;
        public bool DebugGizmo => _debugGizmo;
        public bool IsValid => _range > 0f && _spotAngle > 0f && _innerSpotAngle > 0f && _innerSpotAngle <= _spotAngle;

        private void OnValidate()
        {
            _intensity = Mathf.Max(0f, _intensity);
            _range = Mathf.Max(0.1f, _range);
            _spotAngle = Mathf.Clamp(_spotAngle, 1f, 120f);
            _innerSpotAngle = Mathf.Clamp(_innerSpotAngle, 1f, _spotAngle);
            _shadowStrength = Mathf.Clamp01(_shadowStrength);
            _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
        }
    }
}
