using UnityEngine;

namespace DiveProtocol.Environment.Lighting
{
    /// <summary>
    /// Applies a simple smooth flicker effect to a Light on this GameObject or one of its children.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LightFlicker : MonoBehaviour
    {
        [SerializeField] private bool flickerEnabled = true;
        [SerializeField] private float minIntensity = 0.4f;
        [SerializeField] private float maxIntensity = 1.6f;
        [SerializeField] private float flickerSpeed = 8f;
        [SerializeField] private float randomJitter = 0.25f;
        [SerializeField] private bool useUnscaledTime = false;
        [SerializeField] private bool startWithRandomOffset = true;
        [SerializeField] private bool occasionallyBlackout = false;
        [SerializeField] private float blackoutChancePerSecond = 0.05f;
        [SerializeField] private float blackoutDuration = 0.08f;

        private UnityEngine.Light _targetLight;
        private float _baseIntensity;
        private float _timeOffset;
        private float _blackoutRemainingSeconds;
        private bool _hasWarnedMissingLight;

        private void Awake()
        {
            ResolveLight();
            _timeOffset = startWithRandomOffset ? Random.Range(0f, 1000f) : 0f;
        }

        private void OnEnable()
        {
            ResolveLight();
        }

        private void Update()
        {
            if (!flickerEnabled)
            {
                return;
            }

            if (_targetLight == null)
            {
                WarnMissingLightOnce();
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (_blackoutRemainingSeconds > 0f)
            {
                _blackoutRemainingSeconds -= deltaTime;
                _targetLight.intensity = 0f;
                return;
            }

            TryStartBlackout(deltaTime);
            if (_blackoutRemainingSeconds > 0f)
            {
                _targetLight.intensity = 0f;
                return;
            }

            float time = (useUnscaledTime ? Time.unscaledTime : Time.time) + _timeOffset;
            float baseNoise = Mathf.PerlinNoise(time * Mathf.Max(0f, flickerSpeed), 0.271f);
            float jitterNoise = Mathf.PerlinNoise((time + 37.19f) * Mathf.Max(0f, flickerSpeed * 1.73f), 4.913f);

            float normalizedJitter = (jitterNoise - 0.5f) * 2f * Mathf.Max(0f, randomJitter);
            float flickerValue = Mathf.Clamp01(baseNoise + normalizedJitter);
            _targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, flickerValue);
        }

        private void OnValidate()
        {
            if (minIntensity < 0f)
            {
                minIntensity = 0f;
            }

            if (maxIntensity < minIntensity)
            {
                maxIntensity = minIntensity;
            }

            if (flickerSpeed < 0f)
            {
                flickerSpeed = 0f;
            }

            if (randomJitter < 0f)
            {
                randomJitter = 0f;
            }

            if (blackoutChancePerSecond < 0f)
            {
                blackoutChancePerSecond = 0f;
            }

            if (blackoutDuration < 0f)
            {
                blackoutDuration = 0f;
            }
        }

        /// <summary>
        /// Enables or disables the flicker effect. Disabling restores the original light intensity.
        /// </summary>
        public void SetFlickerEnabled(bool enabled)
        {
            flickerEnabled = enabled;

            if (!enabled)
            {
                RestoreBaseIntensity();
            }
        }

        /// <summary>
        /// Updates the flicker intensity range.
        /// </summary>
        public void SetIntensityRange(float min, float max)
        {
            minIntensity = Mathf.Max(0f, min);
            maxIntensity = Mathf.Max(minIntensity, max);
        }

        /// <summary>
        /// Restores the light intensity captured when this component resolved its target Light.
        /// </summary>
        public void RestoreBaseIntensity()
        {
            if (_targetLight == null)
            {
                ResolveLight();
            }

            if (_targetLight != null)
            {
                _targetLight.intensity = _baseIntensity;
            }
        }

        private void ResolveLight()
        {
            if (_targetLight != null)
            {
                return;
            }

            _targetLight = GetComponent<UnityEngine.Light>();
            if (_targetLight == null)
            {
                _targetLight = GetComponentInChildren<UnityEngine.Light>(true);
            }

            if (_targetLight != null)
            {
                _baseIntensity = _targetLight.intensity;
                _hasWarnedMissingLight = false;
            }
            else
            {
                WarnMissingLightOnce();
            }
        }

        private void TryStartBlackout(float deltaTime)
        {
            if (!occasionallyBlackout || blackoutDuration <= 0f || blackoutChancePerSecond <= 0f)
            {
                return;
            }

            float chanceThisFrame = blackoutChancePerSecond * Mathf.Max(0f, deltaTime);
            if (Random.value <= chanceThisFrame)
            {
                _blackoutRemainingSeconds = blackoutDuration;
            }
        }

        private void WarnMissingLightOnce()
        {
            if (_hasWarnedMissingLight)
            {
                return;
            }

            _hasWarnedMissingLight = true;
            Debug.LogWarning($"LightFlicker on '{name}' could not find a Light on this GameObject or its children.", this);
        }
    }
}
