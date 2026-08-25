using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.RoomVisibility
{
    /// <summary>Applies scene-baked room visual and local-light isolation around the active player rooms.</summary>
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class RoomVisibilityManager : MonoBehaviour
    {
        [SerializeField] private RoomVisibilitySceneData _sceneData;
        [SerializeField] private bool _logActiveRoomChanges = true;
        [SerializeField] private bool _enableLeakDebugHotkey = true;

        private readonly RoomVisibilityActiveRoomSet _activeRoomState = new RoomVisibilityActiveRoomSet();
        private readonly HashSet<Renderer> _visibleRenderers = new HashSet<Renderer>();
        private readonly HashSet<Light> _visibleLights = new HashSet<Light>();
        private readonly HashSet<DecalProjector> _visibleDecals = new HashSet<DecalProjector>();
        private readonly Dictionary<Renderer, bool> _rendererBaselineForceOff = new Dictionary<Renderer, bool>();
        private readonly Dictionary<Light, bool> _lightBaselineEnabled = new Dictionary<Light, bool>();
        private readonly Dictionary<DecalProjector, bool> _decalBaselineEnabled = new Dictionary<DecalProjector, bool>();

        private Transform _player;
        private PlayerSpawner _playerSpawner;
        private bool _hasApplied;
        private bool _leakDebugEnabled;
        private Renderer _lastDebugRenderer;
        private float _nextDebugInspectionTime;

        public IReadOnlyCollection<RoomVisibilityRoomEntry> ActiveRooms => _activeRoomState.Current;
        public IReadOnlyCollection<RoomVisibilityRoomEntry> LastValidActiveRooms => _activeRoomState.LastValid;

        private void Awake()
        {
            if (_sceneData == null) _sceneData = GetComponent<RoomVisibilitySceneData>();
            CacheBaselineStates();
            SubscribeToPlayerSpawner();
            ApplyCurrentVisibility(force: true);
        }

        private void Start()
        {
            ResolvePlayer();
            ApplyCurrentVisibility(force: true);
        }

        private void Update()
        {
            ResolvePlayer();
            ApplyCurrentVisibility(force: false);
            UpdateLeakDebug();
        }

        private void LateUpdate()
        {
            // Camera Occlusion Fade may restore its fallback renderers late in the frame.
            // Reassert only hidden-room renderers so active-room occlusion fading stays untouched.
            foreach (KeyValuePair<Renderer, bool> item in _rendererBaselineForceOff)
            {
                if (item.Key != null && !_visibleRenderers.Contains(item.Key) && !item.Key.forceRenderingOff)
                {
                    item.Key.forceRenderingOff = true;
                }
            }
        }

        private void OnDisable()
        {
            RestoreBaselineStates();
            UnsubscribeFromPlayerSpawner();
        }

        private void OnDestroy()
        {
            RestoreBaselineStates();
            UnsubscribeFromPlayerSpawner();
        }

        private void SubscribeToPlayerSpawner()
        {
            _playerSpawner = FindFirstObjectByType<PlayerSpawner>();
            if (_playerSpawner != null) _playerSpawner.PlayerSpawned += HandlePlayerSpawned;
        }

        private void UnsubscribeFromPlayerSpawner()
        {
            if (_playerSpawner != null) _playerSpawner.PlayerSpawned -= HandlePlayerSpawned;
            _playerSpawner = null;
        }

        private void HandlePlayerSpawned(Transform player)
        {
            _player = player;
            ApplyCurrentVisibility(force: true);
        }

        private void ResolvePlayer()
        {
            if (_player != null) return;
            if (_playerSpawner == null) SubscribeToPlayerSpawner();
            if (_playerSpawner != null && _playerSpawner.SpawnedPlayer != null)
            {
                _player = _playerSpawner.SpawnedPlayer;
                return;
            }

            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
            if (movement != null) _player = movement.transform;
        }

        private void ApplyCurrentVisibility(bool force)
        {
            if (_sceneData == null) return;
            var detected = new HashSet<RoomVisibilityRoomEntry>();
            if (_player != null)
            {
                Vector3 detectionPoint = GetPlayerDetectionPoint(_player);
                foreach (RoomVisibilityRoomEntry entry in _sceneData.Rooms)
                {
                    if (entry != null && entry.ContainsWorldPoint(detectionPoint))
                    {
                        detected.Add(entry);
                    }
                }
            }

            bool changed = _activeRoomState.Update(detected);
            if (!force && _hasApplied && !changed) return;
            BuildVisibleUnion();
            ApplyUnion();
            _hasApplied = true;

            if (_logActiveRoomChanges && ActiveRooms.Count > 0)
            {
                var ids = new List<string>();
                foreach (RoomVisibilityRoomEntry room in ActiveRooms) ids.Add(room.RoomId);
                ids.Sort(System.StringComparer.Ordinal);
                Debug.Log($"[RoomVisibility] Active Rooms: {string.Join(", ", ids)}", this);
            }
        }

        private void BuildVisibleUnion()
        {
            _visibleRenderers.Clear();
            _visibleLights.Clear();
            _visibleDecals.Clear();
            foreach (RoomVisibilityRoomEntry entry in _sceneData.Rooms)
            {
                if (entry == null || !ActiveRooms.Contains(entry)) continue;
                foreach (Renderer renderer in entry.Renderers) if (renderer != null) _visibleRenderers.Add(renderer);
                foreach (Renderer renderer in entry.GameplayVisualRenderers) if (renderer != null) _visibleRenderers.Add(renderer);
                foreach (Light light in entry.Lights) if (light != null) _visibleLights.Add(light);
                foreach (DecalProjector decal in entry.Decals) if (decal != null) _visibleDecals.Add(decal);
            }
        }

        private void ApplyUnion()
        {
            foreach (KeyValuePair<Renderer, bool> item in _rendererBaselineForceOff)
            {
                if (item.Key != null) item.Key.forceRenderingOff = _visibleRenderers.Contains(item.Key) ? item.Value : true;
            }

            foreach (KeyValuePair<Light, bool> item in _lightBaselineEnabled)
            {
                if (item.Key != null) item.Key.enabled = _visibleLights.Contains(item.Key) && item.Value;
            }

            foreach (KeyValuePair<DecalProjector, bool> item in _decalBaselineEnabled)
            {
                if (item.Key != null) item.Key.enabled = _visibleDecals.Contains(item.Key) && item.Value;
            }
        }

        private void CacheBaselineStates()
        {
            if (_sceneData == null) return;
            foreach (RoomVisibilityRoomEntry entry in _sceneData.Rooms)
            {
                if (entry == null) continue;
                foreach (Renderer renderer in entry.Renderers)
                {
                    if (renderer != null && !_rendererBaselineForceOff.ContainsKey(renderer)) _rendererBaselineForceOff.Add(renderer, renderer.forceRenderingOff);
                }
                foreach (Renderer renderer in entry.GameplayVisualRenderers)
                {
                    if (renderer != null && !_rendererBaselineForceOff.ContainsKey(renderer)) _rendererBaselineForceOff.Add(renderer, renderer.forceRenderingOff);
                }
                foreach (Light light in entry.Lights)
                {
                    if (light != null && !_lightBaselineEnabled.ContainsKey(light)) _lightBaselineEnabled.Add(light, light.enabled);
                }
                foreach (DecalProjector decal in entry.Decals)
                {
                    if (decal != null && !_decalBaselineEnabled.ContainsKey(decal)) _decalBaselineEnabled.Add(decal, decal.enabled);
                }
            }
        }

        private void RestoreBaselineStates()
        {
            foreach (KeyValuePair<Renderer, bool> item in _rendererBaselineForceOff)
                if (item.Key != null) item.Key.forceRenderingOff = item.Value;
            foreach (KeyValuePair<Light, bool> item in _lightBaselineEnabled)
                if (item.Key != null) item.Key.enabled = item.Value;
            foreach (KeyValuePair<DecalProjector, bool> item in _decalBaselineEnabled)
                if (item.Key != null) item.Key.enabled = item.Value;
        }

        private static Vector3 GetPlayerDetectionPoint(Transform player)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) return controller.bounds.center;
            Collider collider = player.GetComponent<Collider>();
            if (collider != null) return collider.bounds.center;
            return player.position;
        }

        private void UpdateLeakDebug()
        {
            if (!_enableLeakDebugHotkey) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
            {
                _leakDebugEnabled = !_leakDebugEnabled;
                _lastDebugRenderer = null;
                Debug.Log($"[RoomVisibility] Leak debug {(_leakDebugEnabled ? "enabled" : "disabled")}. Point at a collider to inspect its Room Visibility membership.", this);
            }

            if (!_leakDebugEnabled || Time.unscaledTime < _nextDebugInspectionTime) return;
            _nextDebugInspectionTime = Time.unscaledTime + 0.15f;

            Mouse mouse = Mouse.current;
            Camera camera = Camera.main;
            if (camera == null || mouse == null) return;
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 250f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                _lastDebugRenderer = null;
                return;
            }

            Renderer renderer = FindRendererForCollider(hit.collider);
            if (renderer == null || renderer == _lastDebugRenderer) return;
            _lastDebugRenderer = renderer;

            if (TryGetVisibilityInfo(renderer, out string rooms, out string source, out bool visible))
            {
                Debug.Log($"[RoomVisibility] Inspect: {RoomVisibilityUtility.GetHierarchyPath(renderer.transform)} | Rooms={rooms} | Visibility Source={source} | ForceRenderingOff={renderer.forceRenderingOff} | Active={visible}", renderer);
            }
            else
            {
                Debug.Log($"[RoomVisibility] Inspect: {RoomVisibilityUtility.GetHierarchyPath(renderer.transform)} | Rooms=Unmanaged | Visibility Source=Global / Not baked | ForceRenderingOff={renderer.forceRenderingOff}", renderer);
            }
        }

        private static Renderer FindRendererForCollider(Collider collider)
        {
            if (collider == null) return null;
            Renderer renderer = collider.GetComponent<Renderer>();
            if (renderer != null) return renderer;
            renderer = collider.GetComponentInParent<Renderer>();
            if (renderer != null) return renderer;
            return collider.GetComponentInChildren<Renderer>();
        }

        /// <summary>Returns baked ownership and current visibility for a renderer used by the F9 leak inspector.</summary>
        public bool TryGetVisibilityInfo(Renderer renderer, out string rooms, out string source, out bool visible)
        {
            rooms = string.Empty;
            source = string.Empty;
            visible = false;
            if (renderer == null || _sceneData == null) return false;

            var roomIds = new List<string>();
            bool environment = false;
            bool gameplayVisual = false;
            foreach (RoomVisibilityRoomEntry entry in _sceneData.Rooms)
            {
                if (entry == null) continue;
                if (entry.Renderers.Contains(renderer))
                {
                    roomIds.Add(entry.RoomId);
                    environment = true;
                }
                if (entry.GameplayVisualRenderers.Contains(renderer))
                {
                    roomIds.Add(entry.RoomId);
                    gameplayVisual = true;
                }
            }

            if (roomIds.Count == 0) return false;
            roomIds.Sort(System.StringComparer.Ordinal);
            rooms = string.Join(", ", roomIds.Distinct());
            source = gameplayVisual ? "Enemy/Boss Visual Isolation" : environment ? "Environment Renderer" : "Unknown";
            visible = _visibleRenderers.Contains(renderer) && !renderer.forceRenderingOff;
            return true;
        }

    }
}
