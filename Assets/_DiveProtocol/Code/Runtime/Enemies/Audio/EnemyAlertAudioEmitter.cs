using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>
    /// Plays occasional 3D zombie alert sounds for an individual enemy after it detects the player.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class EnemyAlertAudioEmitter : MonoBehaviour
    {
        private const string BackgroundLoopFileStem = "847400";
        private const string DefaultAlertResourceFolder = "Audio/Enemies/Idle";
        private const string DefaultHitResourceFolder = "Audio/Enemies/Hit";
        private const string DefaultDeathResourcePath = "Audio/Enemies/Death/196726__paulmorek__sz_squish_09";
        private const float GlobalEnemyVoiceVolumeScale = 0.55f;

        private static readonly HashSet<EnemyAlertAudioEmitter> RegisteredEmitters = new();

        [Header("References")]
        [Tooltip("3D AudioSource on the enemy root.")]
        [SerializeField]
        private AudioSource _audioSource;

        [Tooltip("Individual zombie growl/groan clips. Do not assign the global idle loop here.")]
        [SerializeField]
        private AudioClip[] _alertClips;

        [Tooltip("Randomly played when this enemy takes non-lethal damage.")]
        [SerializeField]
        private AudioClip[] _hitClips;

        [Tooltip("Played once when this enemy dies. If empty, a default squish clip is loaded from Resources.")]
        [SerializeField]
        private AudioClip _deathClip;

        [Header("Playback Rules")]
        [SerializeField]
        private bool _playOnlyWhenPlayerDetected = true;

        [SerializeField]
        private bool _stopWhenPlayerLost = true;

        [SerializeField]
        private bool _playOneShotOnFirstDetection = true;

        [Tooltip("When enabled, alert clips chain back-to-back while this enemy is detecting/chasing the player.")]
        [SerializeField]
        private bool _continuousWhilePlayerDetected = true;

        [SerializeField, Min(0f)]
        private float _firstDetectionDelayMin = 0f;

        [SerializeField, Min(0f)]
        private float _firstDetectionDelayMax = 0.4f;

        [SerializeField, Min(0f)]
        private float _minDelay;

        [SerializeField, Min(0f)]
        private float _maxDelay;

        [Header("Variation")]
        [SerializeField, Range(0f, 1f)]
        private float _volumeMin = 0.12f;

        [SerializeField, Range(0f, 1f)]
        private float _volumeMax = 0.28f;

        [SerializeField, Min(0.01f)]
        private float _pitchMin = 0.9f;

        [SerializeField, Min(0.01f)]
        private float _pitchMax = 1.08f;

        [Header("Limiter")]
        [SerializeField]
        private bool _useGlobalLimiter = true;

        [SerializeField]
        private bool _logDebug;

        private IEnemyAwarenessAudioState _awarenessState;
        private HealthComponent _health;
        private Coroutine _releaseVoiceCoroutine;
        private float _nextAllowedPlayTime;
        private bool _wasDetected;
        private bool _hasLimiterSlot;
        private bool _isSubscribedToHealth;
        private bool _hasPlayedDeathSound;
        private float _nextAllowedHitSoundTime;

        public static IReadOnlyCollection<EnemyAlertAudioEmitter> ActiveEmitters => RegisteredEmitters;
        public bool IsPlayerDetectedForAudio => _awarenessState != null && _awarenessState.IsPlayerDetectedForAudio;
        public bool IsDeadForAudio => (_awarenessState != null && _awarenessState.IsDeadForAudio) || (_health != null && !_health.IsAlive);

        private void Reset()
        {
            ResolveReferences();
            ConfigureAudioSource();
            ClampValues();
        }

        private void Awake()
        {
            ResolveReferences();
            LoadDefaultClipsIfNeeded();
            ConfigureAudioSource();
            ClampValues();
        }

        private void OnEnable()
        {
            RegisteredEmitters.Add(this);
            SubscribeHealth();
            _nextAllowedPlayTime = Time.time + Random.Range(_minDelay, _maxDelay);
        }

        private void Update()
        {
            if (IsDeadForAudio)
            {
                StopActivePlayback();
                _wasDetected = false;
                return;
            }

            bool detected = IsPlayerDetectedForAudio || !_playOnlyWhenPlayerDetected;

            if (!detected)
            {
                if (_stopWhenPlayerLost)
                {
                    StopActivePlayback();
                }

                _wasDetected = false;
                return;
            }

            if (!_wasDetected)
            {
                _wasDetected = true;
                _nextAllowedPlayTime = Time.time + Random.Range(_firstDetectionDelayMin, _firstDetectionDelayMax);

                if (!_playOneShotOnFirstDetection)
                {
                    _nextAllowedPlayTime = Time.time + GetPostAlertDelay();
                }
            }

            if (Time.time < _nextAllowedPlayTime)
            {
                return;
            }

            float playbackDuration = TryPlayAlertClip();
            _nextAllowedPlayTime = playbackDuration > 0f
                ? Time.time + playbackDuration + GetPostAlertDelay()
                : Time.time + 0.25f;
        }

        private void OnDisable()
        {
            RegisteredEmitters.Remove(this);
            UnsubscribeHealth();
            StopActivePlayback();
            EnemyAlertAudioLimiter.Release(this);
            _hasLimiterSlot = false;
        }

        private void OnDestroy()
        {
            RegisteredEmitters.Remove(this);
            UnsubscribeHealth();
            EnemyAlertAudioLimiter.Release(this);
        }

        private void ResolveReferences()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            _awarenessState = GetComponent<IEnemyAwarenessAudioState>();

            if (_health == null)
            {
                _health = GetComponent<HealthComponent>();
            }
        }

        private void SubscribeHealth()
        {
            if (_isSubscribedToHealth || _health == null)
            {
                return;
            }

            _health.Damaged += HandleHealthDamaged;
            _health.Died += HandleHealthDied;
            _isSubscribedToHealth = true;
        }

        private void UnsubscribeHealth()
        {
            if (!_isSubscribedToHealth || _health == null)
            {
                return;
            }

            _health.Damaged -= HandleHealthDamaged;
            _health.Died -= HandleHealthDied;
            _isSubscribedToHealth = false;
        }

        private void HandleHealthDamaged(HealthComponent health, DamageInfo damageInfo)
        {
            if (health == null || !health.IsAlive)
            {
                return;
            }

            PlayHitSound();
        }

        private void HandleHealthDied(HealthComponent health)
        {
            PlayDeathSound();
        }

        private void ConfigureAudioSource()
        {
            if (_audioSource == null)
            {
                return;
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _audioSource.minDistance = 1.5f;
            _audioSource.maxDistance = 12f;
            _audioSource.dopplerLevel = 0f;
            _audioSource.volume = 0.25f;
            _audioSource.pitch = 1f;
        }

        private float TryPlayAlertClip()
        {
            LoadDefaultClipsIfNeeded();

            if (_audioSource == null || _alertClips == null || _alertClips.Length == 0)
            {
                return -1f;
            }

            if (_hasLimiterSlot)
            {
                return -1f;
            }

            AudioClip clip = PickAlertClip();
            if (clip == null)
            {
                return -1f;
            }

            if (_useGlobalLimiter && !EnemyAlertAudioLimiter.TryAcquire(this))
            {
                return -1f;
            }

            _hasLimiterSlot = _useGlobalLimiter;

            float pitch = Random.Range(_pitchMin, _pitchMax);
            float volume = Random.Range(_volumeMin, _volumeMax) *
                           GlobalEnemyVoiceVolumeScale *
                           GetClipVolumeScale(clip);
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, volume);

            if (_logDebug)
            {
                Debug.Log($"[EnemyAudio] '{name}' played alert clip '{clip.name}'.", this);
            }

            if (_releaseVoiceCoroutine != null)
            {
                StopCoroutine(_releaseVoiceCoroutine);
            }

            float duration = clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
            _releaseVoiceCoroutine = StartCoroutine(ReleaseLimiterAfterDelay(duration));
            return duration;
        }

        private AudioClip PickAlertClip()
        {
            LoadDefaultClipsIfNeeded();

            int safety = 0;
            while (safety < 16)
            {
                safety++;
                AudioClip clip = _alertClips[Random.Range(0, _alertClips.Length)];
                if (clip != null && !clip.name.Contains(BackgroundLoopFileStem))
                {
                    return clip;
                }
            }

            return null;
        }

        private void PlayHitSound()
        {
            if (Time.time < _nextAllowedHitSoundTime)
            {
                return;
            }

            LoadDefaultClipsIfNeeded();
            AudioClip clip = PickRandomClip(_hitClips);
            if (clip == null)
            {
                return;
            }

            ResolveReferences();
            if (_audioSource == null)
            {
                return;
            }

            float previousPitch = _audioSource.pitch;
            _audioSource.pitch = Random.Range(_pitchMin, _pitchMax);
            _audioSource.PlayOneShot(
                clip,
                Random.Range(_volumeMin, _volumeMax) * GlobalEnemyVoiceVolumeScale * GetClipVolumeScale(clip));
            _audioSource.pitch = previousPitch;
            _nextAllowedHitSoundTime = Time.time + 0.08f;

            if (_logDebug)
            {
                Debug.Log($"[EnemyAudio] '{name}' played hit clip '{clip.name}'.", this);
            }
        }

        private void PlayDeathSound()
        {
            if (_hasPlayedDeathSound)
            {
                return;
            }

            _hasPlayedDeathSound = true;
            StopActivePlayback();
            LoadDefaultClipsIfNeeded();

            if (_deathClip == null)
            {
                return;
            }

            AudioSource.PlayClipAtPoint(
                _deathClip,
                transform.position,
                Mathf.Clamp(_volumeMax * GlobalEnemyVoiceVolumeScale * GetClipVolumeScale(_deathClip), 0.04f, 0.25f));

            if (_logDebug)
            {
                Debug.Log($"[EnemyAudio] '{name}' played death clip '{_deathClip.name}'.", this);
            }
        }

        private void LoadDefaultClipsIfNeeded()
        {
            if (_alertClips == null || _alertClips.Length == 0)
            {
                _alertClips = Resources.LoadAll<AudioClip>(DefaultAlertResourceFolder);
            }

            if (_hitClips == null || _hitClips.Length == 0)
            {
                _hitClips = Resources.LoadAll<AudioClip>(DefaultHitResourceFolder);
            }

            if (_deathClip == null)
            {
                _deathClip = Resources.Load<AudioClip>(DefaultDeathResourcePath);
            }
        }

        private static AudioClip PickRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int safety = 0;
            while (safety < 16)
            {
                safety++;
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static float GetClipVolumeScale(AudioClip clip)
        {
            if (clip == null)
            {
                return 1f;
            }

            string clipName = clip.name;

            if (clipName.Contains("163447"))
            {
                return 0.45f;
            }

            if (clipName.Contains("172004") ||
                clipName.Contains("246487") ||
                clipName.Contains("555417"))
            {
                return 0.55f;
            }

            if (clipName.Contains("181375"))
            {
                return 0.65f;
            }

            if (clipName.Contains("66134") ||
                clipName.Contains("181374") ||
                clipName.Contains("196726"))
            {
                return 0.75f;
            }

            return 0.7f;
        }

        private IEnumerator ReleaseLimiterAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            EnemyAlertAudioLimiter.Release(this);
            _hasLimiterSlot = false;
            _releaseVoiceCoroutine = null;
        }

        private float GetPostAlertDelay()
        {
            return _continuousWhilePlayerDetected
                ? 0f
                : Random.Range(_minDelay, _maxDelay);
        }

        private void StopActivePlayback()
        {
            if (_releaseVoiceCoroutine != null)
            {
                StopCoroutine(_releaseVoiceCoroutine);
                _releaseVoiceCoroutine = null;
            }

            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.pitch = 1f;
            }

            if (_hasLimiterSlot)
            {
                EnemyAlertAudioLimiter.Release(this);
                _hasLimiterSlot = false;
            }
        }

        private void ClampValues()
        {
            _firstDetectionDelayMin = Mathf.Max(0f, _firstDetectionDelayMin);
            _firstDetectionDelayMax = Mathf.Max(_firstDetectionDelayMin, _firstDetectionDelayMax);
            _minDelay = Mathf.Max(0f, _minDelay);
            _maxDelay = Mathf.Max(_minDelay, _maxDelay);
            _volumeMax = Mathf.Max(_volumeMin, _volumeMax);
            _pitchMin = Mathf.Max(0.01f, _pitchMin);
            _pitchMax = Mathf.Max(_pitchMin, _pitchMax);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampValues();
            ResolveReferences();
            ConfigureAudioSource();
        }
#endif
    }
}
