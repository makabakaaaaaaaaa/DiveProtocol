using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.RoomVisibility
{
    [Serializable]
    public sealed class RoomVisibilityRoomEntry
    {
        [SerializeField] private RoomVolume[] _volumes = Array.Empty<RoomVolume>();
        [SerializeField] private Renderer[] _renderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] _gameplayVisualRenderers = Array.Empty<Renderer>();
        [SerializeField] private Light[] _lights = Array.Empty<Light>();
        [SerializeField] private DecalProjector[] _decals = Array.Empty<DecalProjector>();
        [SerializeField] private Renderer[] _manualIncludeRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] _manualExcludeRenderers = Array.Empty<Renderer>();
        [SerializeField] private Light[] _manualIncludeLights = Array.Empty<Light>();
        [SerializeField] private Light[] _manualExcludeLights = Array.Empty<Light>();
        [SerializeField] private DecalProjector[] _manualIncludeDecals = Array.Empty<DecalProjector>();
        [SerializeField] private DecalProjector[] _manualExcludeDecals = Array.Empty<DecalProjector>();

        public IReadOnlyList<RoomVolume> Volumes => _volumes;
        public string RoomId => _volumes.Length > 0 && _volumes[0] != null ? _volumes[0].RoomId : string.Empty;
        public IReadOnlyList<Renderer> Renderers => _renderers;
        public IReadOnlyList<Renderer> GameplayVisualRenderers => _gameplayVisualRenderers;
        public IReadOnlyList<Light> Lights => _lights;
        public IReadOnlyList<DecalProjector> Decals => _decals;

        public RoomVisibilityRoomEntry()
        {
        }

        internal RoomVisibilityRoomEntry(IReadOnlyList<RoomVolume> volumes)
        {
            SetVolumes(volumes);
        }

        public bool ContainsVolume(RoomVolume volume)
        {
            return volume != null && Array.IndexOf(_volumes, volume) >= 0;
        }

        public bool ContainsWorldPoint(Vector3 point)
        {
            foreach (RoomVolume volume in _volumes)
            {
                if (volume != null && volume.ContainsWorldPoint(point)) return true;
            }

            return false;
        }

        internal void SetVolumes(IReadOnlyList<RoomVolume> volumes)
        {
            _volumes = new RoomVolume[volumes.Count];
            for (int index = 0; index < volumes.Count; index++) _volumes[index] = volumes[index];
        }

        /// <summary>Replaces automatically baked members while preserving manual include and exclude overrides.</summary>
        public void SetBakedMembers(
            IEnumerable<Renderer> renderers,
            IEnumerable<Renderer> gameplayVisualRenderers,
            IEnumerable<Light> lights,
            IEnumerable<DecalProjector> decals)
        {
            _renderers = Merge(renderers, _manualIncludeRenderers, _manualExcludeRenderers);
            _gameplayVisualRenderers = Merge(gameplayVisualRenderers, Array.Empty<Renderer>(), Array.Empty<Renderer>());
            _lights = Merge(lights, _manualIncludeLights, _manualExcludeLights);
            _decals = Merge(decals, _manualIncludeDecals, _manualExcludeDecals);
        }

        private static T[] Merge<T>(IEnumerable<T> automatic, IEnumerable<T> manualInclude, IEnumerable<T> manualExclude)
            where T : Component
        {
            var values = new HashSet<T>();
            if (automatic != null)
            {
                foreach (T value in automatic)
                {
                    if (value != null) values.Add(value);
                }
            }

            if (manualInclude != null)
            {
                foreach (T value in manualInclude)
                {
                    if (value != null) values.Add(value);
                }
            }

            if (manualExclude != null)
            {
                foreach (T value in manualExclude)
                {
                    if (value != null) values.Remove(value);
                }
            }

            T[] result = new T[values.Count];
            values.CopyTo(result);
            Array.Sort(result, CompareByPath);
            return result;
        }

        private static int CompareByPath<T>(T left, T right) where T : Component
        {
            return string.CompareOrdinal(RoomVisibilityUtility.GetHierarchyPath(left.transform), RoomVisibilityUtility.GetHierarchyPath(right.transform));
        }
    }

    /// <summary>Scene-local baked visual ownership for the Room Visibility runtime system.</summary>
    [DisallowMultipleComponent]
    public sealed class RoomVisibilitySceneData : MonoBehaviour
    {
        [SerializeField] private RoomVisibilityRoomEntry[] _rooms = Array.Empty<RoomVisibilityRoomEntry>();

        public IReadOnlyList<RoomVisibilityRoomEntry> Rooms => _rooms;

        public RoomVisibilityRoomEntry FindEntry(RoomVolume volume)
        {
            if (volume == null) return null;
            for (int index = 0; index < _rooms.Length; index++)
            {
                if (_rooms[index] != null && _rooms[index].ContainsVolume(volume))
                {
                    return _rooms[index];
                }
            }

            return null;
        }

        /// <summary>Creates one deterministic scene-data entry per authored room volume.</summary>
        public void ConfigureRooms(IReadOnlyList<RoomVolume> volumes)
        {
            var existingByRoomId = new Dictionary<string, RoomVisibilityRoomEntry>(StringComparer.Ordinal);
            foreach (RoomVisibilityRoomEntry entry in _rooms)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.RoomId)) existingByRoomId[entry.RoomId] = entry;
            }

            List<IGrouping<string, RoomVolume>> grouped = new List<IGrouping<string, RoomVolume>>(volumes.GroupBy(volume => volume.RoomId));
            _rooms = new RoomVisibilityRoomEntry[grouped.Count];
            for (int index = 0; index < grouped.Count; index++)
            {
                IGrouping<string, RoomVolume> group = grouped[index];
                if (existingByRoomId.TryGetValue(group.Key, out RoomVisibilityRoomEntry entry))
                {
                    entry.SetVolumes(group.ToList());
                    _rooms[index] = entry;
                }
                else
                {
                    _rooms[index] = new RoomVisibilityRoomEntry(group.ToList());
                }
            }
        }
    }
}
