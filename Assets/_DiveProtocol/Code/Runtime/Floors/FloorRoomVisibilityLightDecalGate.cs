using System;
using System.Collections.Generic;
using System.Linq;
using DiveProtocol.RoomVisibility;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol
{
    /// <summary>Composes Floor02 and Room Visibility ownership for the small set of shared Light and Decal components.</summary>
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public sealed class FloorRoomVisibilityLightDecalGate : MonoBehaviour
    {
        [SerializeField] private MultiFloorVisibilityController _floorController;
        [SerializeField] private RoomVisibilityManager _roomVisibility;
        [SerializeField] private RoomVisibilitySceneData _sceneData;
        [SerializeField] private Light[] _floor02Lights = Array.Empty<Light>();
        [SerializeField] private DecalProjector[] _floor02Decals = Array.Empty<DecalProjector>();

        private readonly Dictionary<Light, bool> _lightBaselineEnabled = new();
        private readonly Dictionary<DecalProjector, bool> _decalBaselineEnabled = new();

        public IReadOnlyList<Light> ManagedFloor02Lights => _floor02Lights;
        public IReadOnlyList<DecalProjector> ManagedFloor02Decals => _floor02Decals;

        private void Awake()
        {
            if (_roomVisibility == null) _roomVisibility = GetComponent<RoomVisibilityManager>();
            if (_sceneData == null) _sceneData = GetComponent<RoomVisibilitySceneData>();
            CacheBaselineStates();
        }

        private void LateUpdate()
        {
            ApplyCompositeVisibility();
        }

        private void OnDisable()
        {
            RestoreBaselineStates();
        }

        /// <summary>Assigns the L03-only Floor02 components collected from the existing floor group.</summary>
        public void Configure(
            MultiFloorVisibilityController floorController,
            RoomVisibilityManager roomVisibility,
            RoomVisibilitySceneData sceneData,
            IEnumerable<Light> floor02Lights,
            IEnumerable<DecalProjector> floor02Decals)
        {
            _floorController = floorController != null ? floorController : _floorController;
            _roomVisibility = roomVisibility != null ? roomVisibility : _roomVisibility;
            _sceneData = sceneData != null ? sceneData : _sceneData;
            _floor02Lights = DistinctNonNull(floor02Lights);
            _floor02Decals = DistinctNonNull(floor02Decals);
        }

        private void ApplyCompositeVisibility()
        {
            bool floor02Visible = _floorController == null || _floorController.CurrentState != FloorVisibilityState.Floor01Only;
            foreach (KeyValuePair<Light, bool> entry in _lightBaselineEnabled)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = entry.Value && floor02Visible && IsVisibleInActiveRoom(entry.Key);
                }
            }

            foreach (KeyValuePair<DecalProjector, bool> entry in _decalBaselineEnabled)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = entry.Value && floor02Visible && IsVisibleInActiveRoom(entry.Key);
                }
            }
        }

        private bool IsVisibleInActiveRoom(Component component)
        {
            if (_sceneData == null || _roomVisibility == null) return true;

            bool assignedToRoom = false;
            foreach (RoomVisibilityRoomEntry entry in _sceneData.Rooms)
            {
                if (entry == null || !Contains(entry, component)) continue;
                assignedToRoom = true;
                if (_roomVisibility.ActiveRooms.Contains(entry)) return true;
            }

            return !assignedToRoom;
        }

        private static bool Contains(RoomVisibilityRoomEntry entry, Component component)
        {
            if (component is Light light) return entry.Lights.Contains(light);
            if (component is DecalProjector decal) return entry.Decals.Contains(decal);
            return false;
        }

        private void CacheBaselineStates()
        {
            foreach (Light light in _floor02Lights)
            {
                if (light != null && !_lightBaselineEnabled.ContainsKey(light)) _lightBaselineEnabled.Add(light, light.enabled);
            }

            foreach (DecalProjector decal in _floor02Decals)
            {
                if (decal != null && !_decalBaselineEnabled.ContainsKey(decal)) _decalBaselineEnabled.Add(decal, decal.enabled);
            }
        }

        private void RestoreBaselineStates()
        {
            foreach (KeyValuePair<Light, bool> entry in _lightBaselineEnabled)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            foreach (KeyValuePair<DecalProjector, bool> entry in _decalBaselineEnabled)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
        }

        private static T[] DistinctNonNull<T>(IEnumerable<T> values) where T : UnityEngine.Object
        {
            return values == null
                ? Array.Empty<T>()
                : values.Where(value => value != null).Distinct().ToArray();
        }
    }
}
