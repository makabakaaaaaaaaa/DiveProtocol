using UnityEngine;
using UnityEngine.InputSystem;

namespace DiveProtocol
{
    /// <summary>Drives a player-mounted flashlight from the player's last movement-facing direction.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerFlashlightController : MonoBehaviour
    {
        [SerializeField] private FlashlightConfig _config;
        [SerializeField] private Transform _flashlightPivot;
        [SerializeField] private Light _flashlight;

        private PlayerInputReader _inputReader;
        private Vector3 _lastFacingDirection = Vector3.forward;

        public FlashlightConfig Config => _config;
        public Transform FlashlightPivot => _flashlightPivot;
        public Light Flashlight => _flashlight;

        private void Awake()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            if (_flashlightPivot == null)
            {
                _flashlightPivot = transform.Find("VisualRoot/FlashlightPivot");
            }

            if (_flashlight == null && _flashlightPivot != null)
            {
                _flashlight = _flashlightPivot.GetComponentInChildren<Light>(true);
            }
        }

        private void Start()
        {
            ApplyConfig(preserveEnabled: false);
        }

        private void Update()
        {
            if (_config == null || _flashlightPivot == null)
            {
                return;
            }

            if (_config.AllowToggle && ReadTogglePressed(_config.ToggleInput) && _flashlight != null)
            {
                _flashlight.enabled = !_flashlight.enabled;
            }

            var move = _inputReader != null ? _inputReader.ReadMoveInput() : Vector2.zero;
            if (move.sqrMagnitude > 0.0001f)
            {
                var movement = new Vector3(move.x, 0f, move.y);
                if (movement.sqrMagnitude > 0.0001f)
                {
                    _lastFacingDirection = transform.TransformDirection(movement.normalized);
                    _lastFacingDirection.y = 0f;
                    _lastFacingDirection = _lastFacingDirection.sqrMagnitude > 0.0001f ? _lastFacingDirection.normalized : transform.forward;
                }
            }
            else
            {
                var forward = transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    _lastFacingDirection = forward.normalized;
                }
            }

            var targetRotation = Quaternion.LookRotation(_lastFacingDirection, Vector3.up) * Quaternion.Euler(_config.LocalRotation);
            _flashlightPivot.rotation = Quaternion.RotateTowards(
                _flashlightPivot.rotation,
                targetRotation,
                _config.RotationSpeed * Time.deltaTime);
        }

        public void Configure(FlashlightConfig config, Transform pivot, Light flashlight)
        {
            _config = config != null ? config : _config;
            _flashlightPivot = pivot != null ? pivot : _flashlightPivot;
            _flashlight = flashlight != null ? flashlight : _flashlight;
            ApplyConfig(preserveEnabled: true);
        }

        public void ApplyConfig(bool preserveEnabled)
        {
            if (_config == null || _flashlight == null)
            {
                return;
            }

            var wasEnabled = _flashlight.enabled;
            _flashlight.type = LightType.Spot;
            _flashlight.intensity = _config.Intensity;
            _flashlight.range = _config.Range;
            _flashlight.spotAngle = _config.SpotAngle;
            _flashlight.innerSpotAngle = _config.InnerSpotAngle;
            _flashlight.color = _config.Color;
            _flashlight.shadows = _config.ShadowType;
            _flashlight.shadowStrength = _config.ShadowStrength;
            _flashlight.enabled = preserveEnabled ? wasEnabled : _config.EnableOnStart;

            if (_flashlightPivot != null)
            {
                _flashlightPivot.localPosition = _config.LocalPosition;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null || !_config.DebugGizmo || _flashlightPivot == null)
            {
                return;
            }

            Gizmos.color = _config.Color;
            Gizmos.DrawRay(_flashlightPivot.position, _flashlightPivot.forward * _config.Range);
        }

        private static bool ReadTogglePressed(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyCode switch
            {
                KeyCode.F => keyboard.fKey.wasPressedThisFrame,
                KeyCode.T => keyboard.tKey.wasPressedThisFrame,
                KeyCode.G => keyboard.gKey.wasPressedThisFrame,
                KeyCode.L => keyboard.lKey.wasPressedThisFrame,
                KeyCode.Space => keyboard.spaceKey.wasPressedThisFrame,
                KeyCode.None => false,
                _ => false
            };
        }
    }
}
