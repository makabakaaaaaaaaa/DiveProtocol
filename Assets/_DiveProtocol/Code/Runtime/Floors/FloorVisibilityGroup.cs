using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Visual component collection for one floor; gameplay colliders and triggers stay active.</summary>
    [DisallowMultipleComponent]
    public sealed class FloorVisibilityGroup : MonoBehaviour
    {
        [SerializeField] private FloorId _floorId;
        [SerializeField] private bool _startingFloor;

        [Header("Renderer Collection")]
        [SerializeField] private Transform[] _rendererRoots;
        [SerializeField] private bool _collectRenderersFromRootsOnAwake = true;
        [SerializeField] private bool _includeInactiveRenderers = true;
        [SerializeField] private bool _onlyCollectWhenRendererListIsEmpty = true;

        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Light[] _lights;
        [SerializeField] private ReflectionProbe[] _reflectionProbes;
        [SerializeField] private Behaviour[] _decalProjectors;
        [SerializeField] private ParticleSystem[] _particleSystems;
        [SerializeField] private Behaviour[] _visualEffects;
        [SerializeField] private AudioSource[] _audioSources;
        [SerializeField] private bool _muteAudioWhenHidden = true;

        public FloorId FloorId => _floorId;
        public bool StartingFloor => _startingFloor;
        public Renderer[] Renderers => _renderers;

        private void Awake()
        {
            if (_collectRenderersFromRootsOnAwake &&
                (!_onlyCollectWhenRendererListIsEmpty ||
                 _renderers == null ||
                 _renderers.Length == 0))
            {
                CollectRenderersFromRoots();
            }
        }

        public void Configure(FloorId floorId, bool startingFloor)
        {
            _floorId = floorId;
            _startingFloor = startingFloor;
        }

        public void SetVisualsVisible(bool visible)
        {
            SetRenderersVisible(visible);
            SetLightsVisible(visible);
            SetReflectionProbesVisible(visible);
            SetBehavioursVisible(_decalProjectors, visible);
            SetBehavioursVisible(_visualEffects, visible);
            SetParticlesVisible(visible);
            SetAudioVisible(visible);
        }

        /// <summary>
        /// Shows or hides only the configured visual components; gameplay colliders stay active.
        /// </summary>
        public void SetVisible(bool visible)
        {
            SetVisualsVisible(visible);
        }

        /// <summary>
        /// Rebuilds the renderer list from the configured visual roots.
        /// </summary>
        public void CollectRenderersFromRoots()
        {
            if (_rendererRoots == null || _rendererRoots.Length == 0)
            {
                _renderers = System.Array.Empty<Renderer>();
                return;
            }

            var collected = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < _rendererRoots.Length; i++)
            {
                Transform root = _rendererRoots[i];
                if (root == null)
                {
                    continue;
                }

                Renderer[] found = root.GetComponentsInChildren<Renderer>(_includeInactiveRenderers);
                for (int foundIndex = 0; foundIndex < found.Length; foundIndex++)
                {
                    Renderer renderer = found[foundIndex];
                    if (renderer != null && !collected.Contains(renderer))
                    {
                        collected.Add(renderer);
                    }
                }
            }

            _renderers = collected.ToArray();
        }

        public void SetCollectedComponents(
            Renderer[] renderers,
            Light[] lights,
            ReflectionProbe[] reflectionProbes,
            Behaviour[] decalProjectors,
            ParticleSystem[] particleSystems,
            Behaviour[] visualEffects,
            AudioSource[] audioSources)
        {
            _renderers = renderers ?? System.Array.Empty<Renderer>();
            _lights = lights ?? System.Array.Empty<Light>();
            _reflectionProbes = reflectionProbes ?? System.Array.Empty<ReflectionProbe>();
            _decalProjectors = decalProjectors ?? System.Array.Empty<Behaviour>();
            _particleSystems = particleSystems ?? System.Array.Empty<ParticleSystem>();
            _visualEffects = visualEffects ?? System.Array.Empty<Behaviour>();
            _audioSources = audioSources ?? System.Array.Empty<AudioSource>();
        }

        private void SetRenderersVisible(bool visible)
        {
            if (_renderers == null) return;
            foreach (var item in _renderers)
            {
                if (item != null)
                {
                    item.enabled = visible;
                }
            }
        }

        private void SetLightsVisible(bool visible)
        {
            if (_lights == null) return;
            foreach (var item in _lights)
            {
                if (item != null)
                {
                    item.enabled = visible;
                }
            }
        }

        private void SetReflectionProbesVisible(bool visible)
        {
            if (_reflectionProbes == null) return;
            foreach (var item in _reflectionProbes)
            {
                if (item != null)
                {
                    item.enabled = visible;
                }
            }
        }

        private static void SetBehavioursVisible(Behaviour[] behaviours, bool visible)
        {
            if (behaviours == null) return;
            foreach (var item in behaviours)
            {
                if (item != null)
                {
                    item.enabled = visible;
                }
            }
        }

        private void SetParticlesVisible(bool visible)
        {
            if (_particleSystems == null) return;
            foreach (var item in _particleSystems)
            {
                if (item == null) continue;
                if (visible)
                {
                    item.Play(withChildren: true);
                }
                else
                {
                    item.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void SetAudioVisible(bool visible)
        {
            if (_audioSources == null || !_muteAudioWhenHidden) return;
            foreach (var item in _audioSources)
            {
                if (item != null)
                {
                    item.mute = !visible;
                }
            }
        }
    }
}
