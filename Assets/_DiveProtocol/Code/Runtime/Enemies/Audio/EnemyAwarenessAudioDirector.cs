using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DiveProtocol
{
    /// <summary>
    /// Plays low-pressure zombie ambience while no enemy has detected the player, then lets individual enemies speak after detection.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class EnemyAwarenessAudioDirector : MonoBehaviour
    {
        private const string AutoCreatedObjectName = "_EnemyAwarenessAudioDirector_Runtime";
        private const string DefaultAmbientResourceFolder = "Audio/Enemies/Idle";
        private const float GlobalAmbientVolumeScale = 0.55f;

        [Header("Background Ambience")]
        [Tooltip("2D AudioSource used for low-volume undetected enemy ambience one-shots.")]
        [SerializeField]
        private AudioSource _backgroundSource;

        [Tooltip("Deprecated. The old 847400 loop is no longer used.")]
        [SerializeField]
        private AudioClip _undetectedBackgroundLoop;

        [Tooltip("Random low-volume clips played while no enemy has detected the player. If empty, defaults are loaded from Resources/Audio/Enemies/Idle.")]
        [SerializeField]
        private AudioClip[] _undetectedAmbientClips;

        [SerializeField, Range(0f, 1f)]
        private float _backgroundVolume = 0.09f;

        [SerializeField, Range(0f, 1f)]
        private float _ambientVolumeMin = 0.04f;

        [SerializeField, Range(0f, 1f)]
        private float _ambientVolumeMax = 0.09f;

        [SerializeField, Min(0.1f)]
        private float _ambientIntervalSeconds = 3f;

        [SerializeField, Min(0f)]
        private float _restoreDelay = 4f;

        [SerializeField]
        private bool _playBackgroundOnStart = true;

        [SerializeField]
        private bool _autoFindEnemies = true;

        [SerializeField]
        private bool _autoAttachEnemyEmitters = true;

        [SerializeField]
        private bool _logDebug;

        private readonly List<IEnemyAwarenessAudioState> _fallbackStates = new();

        private float _undetectedSince;
        private float _nextFallbackRefreshTime;
        private float _nextEmitterAttachTime;
        private float _nextAmbientPlayTime;
        private bool _hasAnyDetected;
        private bool _hasLoggedMissingClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeRuntimeDirectorBootstrap()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureRuntimeDirectorExists(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRuntimeDirectorExists(scene);
        }

        private static void EnsureRuntimeDirectorExists(Scene scene)
        {
            if (!IsGameplayLevelScene(scene) || FindFirstObjectByType<EnemyAwarenessAudioDirector>() != null)
            {
                return;
            }

            GameObject directorObject = new GameObject(AutoCreatedObjectName);
            SceneManager.MoveGameObjectToScene(directorObject, scene);
            AudioSource source = directorObject.AddComponent<AudioSource>();
            EnemyAwarenessAudioDirector director = directorObject.AddComponent<EnemyAwarenessAudioDirector>();
            director._backgroundSource = source;
        }

        private static bool IsGameplayLevelScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            return scene.name == "SCN_L01_Drainage" ||
                   scene.name == "SCN_L02_Containment" ||
                   scene.name == "SCN_L03_MaintenanceTransfer" ||
                   scene.name == "SCN_L04_FacilityCore";
        }

        private void Reset()
        {
            ResolveReferences();
            ConfigureBackgroundSource();
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureBackgroundSource();
        }

        private void Start()
        {
            LoadDefaultAmbientClipsIfNeeded();
            _undetectedSince = Time.time;
            _nextAmbientPlayTime = Time.time;

            if (_playBackgroundOnStart && !AnyEnemyDetected())
            {
                TryPlayRandomUndetectedAmbient();
            }
        }

        private void Update()
        {
            if (_autoAttachEnemyEmitters && Time.time >= _nextEmitterAttachTime)
            {
                _nextEmitterAttachTime = Time.time + 1f;
                AutoAttachEnemyEmitters();
            }

            bool anyDetected = AnyEnemyDetected();

            if (anyDetected)
            {
                _hasAnyDetected = true;
                _undetectedSince = -1f;
                StopUndetectedAmbient();
                return;
            }

            if (_undetectedSince < 0f)
            {
                _undetectedSince = Time.time;
            }

            bool canRestore = !_hasAnyDetected || Time.time - _undetectedSince >= _restoreDelay;
            if (canRestore && Time.time >= _nextAmbientPlayTime)
            {
                TryPlayRandomUndetectedAmbient();
            }
        }

        /// <summary>
        /// Immediately refreshes fallback enemy awareness state references.
        /// </summary>
        public void RefreshEnemyStateCache()
        {
            _fallbackStates.Clear();

            if (!_autoFindEnemies)
            {
                return;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IEnemyAwarenessAudioState state)
                {
                    _fallbackStates.Add(state);
                }
            }
        }

        private bool AnyEnemyDetected()
        {
            foreach (EnemyAlertAudioEmitter emitter in EnemyAlertAudioEmitter.ActiveEmitters)
            {
                if (emitter != null && emitter.IsPlayerDetectedForAudio && !emitter.IsDeadForAudio)
                {
                    return true;
                }
            }

            if (!_autoFindEnemies)
            {
                return false;
            }

            if (Time.time >= _nextFallbackRefreshTime)
            {
                _nextFallbackRefreshTime = Time.time + 1f;
                RefreshEnemyStateCache();
            }

            for (int i = _fallbackStates.Count - 1; i >= 0; i--)
            {
                IEnemyAwarenessAudioState state = _fallbackStates[i];
                if (state == null)
                {
                    _fallbackStates.RemoveAt(i);
                    continue;
                }

                if (state.IsPlayerDetectedForAudio && !state.IsDeadForAudio)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (_backgroundSource == null)
            {
                _backgroundSource = GetComponent<AudioSource>();
            }
        }

        private void ConfigureBackgroundSource()
        {
            if (_backgroundSource == null)
            {
                return;
            }

            _backgroundSource.playOnAwake = false;
            _backgroundSource.loop = false;
            _backgroundSource.spatialBlend = 0f;
            _backgroundSource.volume = _backgroundVolume;
            _backgroundSource.pitch = 1f;
            _backgroundSource.dopplerLevel = 0f;
        }

        private void TryPlayRandomUndetectedAmbient()
        {
            if (_backgroundSource == null)
            {
                return;
            }

            LoadDefaultAmbientClipsIfNeeded();

            AudioClip clip = PickAmbientClip();
            if (clip == null)
            {
                LogMissingClipOnce();
                _nextAmbientPlayTime = Time.time + _ambientIntervalSeconds;
                return;
            }

            _backgroundSource.volume = Random.Range(_ambientVolumeMin, _ambientVolumeMax) *
                                       GlobalAmbientVolumeScale *
                                       GetClipVolumeScale(clip);
            _backgroundSource.pitch = 1f;
            _backgroundSource.PlayOneShot(clip, 1f);
            _nextAmbientPlayTime = Time.time + _ambientIntervalSeconds;

            if (_logDebug)
            {
                Debug.Log($"[EnemyAudio] Undetected ambient one-shot: {clip.name}", this);
            }
        }

        private AudioClip PickAmbientClip()
        {
            if (_undetectedAmbientClips == null || _undetectedAmbientClips.Length == 0)
            {
                return null;
            }

            int safety = 0;
            while (safety < 16)
            {
                safety++;
                AudioClip clip = _undetectedAmbientClips[Random.Range(0, _undetectedAmbientClips.Length)];
                if (clip != null && !clip.name.Contains("847400"))
                {
                    return clip;
                }
            }

            return null;
        }

        private void LoadDefaultAmbientClipsIfNeeded()
        {
            if (_undetectedAmbientClips != null && _undetectedAmbientClips.Length > 0)
            {
                return;
            }

            _undetectedAmbientClips = Resources.LoadAll<AudioClip>(DefaultAmbientResourceFolder);
        }

        private void StopUndetectedAmbient()
        {
            if (_backgroundSource != null && _backgroundSource.isPlaying)
            {
                _backgroundSource.Stop();
            }
        }

        private void AutoAttachEnemyEmitters()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                GameObject candidate = behaviour.gameObject;
                if (!IsOrdinaryEnemyRoot(candidate) || candidate.GetComponent<EnemyAlertAudioEmitter>() != null)
                {
                    continue;
                }

                AudioSource source = candidate.GetComponent<AudioSource>();
                if (source == null)
                {
                    source = candidate.AddComponent<AudioSource>();
                }

                EnemyAlertAudioEmitter emitter = candidate.AddComponent<EnemyAlertAudioEmitter>();
                if (_logDebug)
                {
                    Debug.Log($"[EnemyAudio] Runtime-attached EnemyAlertAudioEmitter to '{candidate.name}'.", emitter);
                }
            }
        }

        private void LogMissingClipOnce()
        {
            if (_hasLoggedMissingClip)
            {
                return;
            }

            _hasLoggedMissingClip = true;
            Debug.LogWarning("[EnemyAudio] EnemyAwarenessAudioDirector has no undetected ambient clips assigned or loadable.", this);
        }

        private static bool IsOrdinaryEnemyRoot(GameObject candidate)
        {
            if (candidate == null ||
                candidate.GetComponent<EnemyAlertAudioEmitter>() != null ||
                candidate.GetComponentInParent<EnemyWaveSpawner>() != null)
            {
                return false;
            }

            if (ContainsExcludedName(candidate.name))
            {
                return false;
            }

            bool hasAwarenessState = candidate.GetComponent<IEnemyAwarenessAudioState>() != null;
            bool hasAgent = candidate.GetComponent<NavMeshAgent>() != null;
            bool hasHealthAndAttack = candidate.GetComponent<HealthComponent>() != null && candidate.GetComponent<EnemyContactAttack>() != null;
            return hasAwarenessState || hasAgent || hasHealthAndAttack;
        }

        private static bool ContainsExcludedName(string objectName)
        {
            string[] tokens =
            {
                "Player", "Boss", "Door", "Trigger", "Pickup", "Camera", "UI",
                "HUD", "NavMesh", "PatrolPoint", "SpawnPoint", "Light", "Audio",
                "Corpse", "BuildChoice", "PixelDisplay",
            };

            foreach (string token in tokens)
            {
                if (objectName.Contains(token, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetClipVolumeScale(AudioClip clip)
        {
            if (clip == null)
            {
                return 1f;
            }

            string clipName = clip.name;
            if (clipName.Contains("163447") ||
                clipName.Contains("246487") ||
                clipName.Contains("555417"))
            {
                return 0.55f;
            }

            if (clipName.Contains("181375"))
            {
                return 0.65f;
            }

            if (clipName.Contains("181374") ||
                clipName.Contains("66134"))
            {
                return 0.75f;
            }

            return 0.7f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _backgroundVolume = Mathf.Clamp01(_backgroundVolume);
            _ambientVolumeMin = Mathf.Clamp01(_ambientVolumeMin);
            _ambientVolumeMax = Mathf.Max(_ambientVolumeMin, _ambientVolumeMax);
            _ambientIntervalSeconds = Mathf.Max(0.1f, _ambientIntervalSeconds);
            _restoreDelay = Mathf.Max(0f, _restoreDelay);
            ResolveReferences();
            ConfigureBackgroundSource();
        }
#endif
    }
}
