using System;
using System.Collections.Generic;
using System.Linq;
using DiveProtocol.RoomVisibility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DiveProtocol.Editor.RoomVisibility
{
    /// <summary>Bakes scene-instance room ownership and provides non-persistent editor preview commands.</summary>
    public static class RoomVisibilityBaker
    {
        private const float SharedScoreRatio = 0.98f;
        private const float SharedScoreEpsilon = 0.25f;
        private const int MinimumSharedContainedCorners = 2;
        private const float CrossRoomLargeRendererSize = 5f;
        private static readonly Dictionary<Renderer, bool> PreviewRendererStates = new();
        private static readonly Dictionary<Light, bool> PreviewLightStates = new();
        private static readonly Dictionary<DecalProjector, bool> PreviewDecalStates = new();

        static RoomVisibilityBaker()
        {
            AssemblyReloadEvents.beforeAssemblyReload += RestorePreview;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorSceneManager.sceneClosing += HandleSceneClosing;
        }

        [MenuItem("Tools/Dive Protocol/Room Visibility/Bake Current Scene")]
        public static void BakeCurrentScene()
        {
            RestorePreview();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[RoomVisibility] No loaded active scene to bake.");
                return;
            }

            List<RoomVolume> volumes = FindAndPrepareVolumes(scene);
            if (volumes.Count == 0)
            {
                Debug.LogError($"[RoomVisibility] '{scene.name}' has no ROOM_*_VOLUME objects with BoxCollider components.");
                return;
            }

            if (volumes.Any(volume => string.IsNullOrWhiteSpace(volume.RoomId)))
            {
                Debug.LogError("[RoomVisibility] Room IDs must be non-empty before baking.");
                return;
            }

            RoomVisibilitySceneData data = GetOrCreateSceneData(scene);
            Undo.RecordObject(data, "Bake Room Visibility");
            data.ConfigureRooms(volumes);

            var rendererMembers = data.Rooms.ToDictionary(entry => entry, _ => new HashSet<Renderer>());
            var gameplayVisualMembers = data.Rooms.ToDictionary(entry => entry, _ => new HashSet<Renderer>());
            var lightMembers = data.Rooms.ToDictionary(entry => entry, _ => new HashSet<Light>());
            var decalMembers = data.Rooms.ToDictionary(entry => entry, _ => new HashSet<DecalProjector>());
            var sharedRenderers = new Dictionary<Renderer, List<RoomVolume>>();
            var unassigned = new List<Renderer>();
            var unassignedGameplayVisuals = new List<Renderer>();
            var excludedGameplay = new List<Renderer>();
            var crossRoomLarge = new List<Renderer>();
            var mixedOrBakedLights = new List<Light>();

            foreach (Renderer renderer in FindSceneComponents<Renderer>(scene))
            {
                if (RoomVisibilityUtility.TryGetEnemyOrBossRoot(renderer, out Transform gameplayRoot))
                {
                    List<RoomVolume> gameplayMembers = NormalizeRoomMembership(ResolvePointMembership(gameplayRoot.position, volumes));
                    if (gameplayMembers.Count == 0) gameplayMembers = NormalizeRoomMembership(ResolveRendererMembership(renderer, volumes));
                    if (gameplayMembers.Count == 0)
                    {
                        unassignedGameplayVisuals.Add(renderer);
                        continue;
                    }

                    foreach (RoomVolume member in gameplayMembers)
                    {
                        RoomVisibilityRoomEntry entry = data.FindEntry(member);
                        if (entry != null) gameplayVisualMembers[entry].Add(renderer);
                    }

                    continue;
                }

                if (RoomVisibilityUtility.IsGameplayObject(renderer))
                {
                    excludedGameplay.Add(renderer);
                    continue;
                }

                List<RoomVolume> members = NormalizeRoomMembership(ResolveRendererMembership(renderer, volumes));
                if (members.Count == 0)
                {
                    unassigned.Add(renderer);
                    continue;
                }

                foreach (RoomVolume member in members)
                {
                    RoomVisibilityRoomEntry entry = data.FindEntry(member);
                    if (entry != null) rendererMembers[entry].Add(renderer);
                }
                if (members.Count > 1)
                {
                    sharedRenderers.Add(renderer, members);
                    if (renderer.bounds.size.magnitude >= CrossRoomLargeRendererSize) crossRoomLarge.Add(renderer);
                }
            }

            foreach (Light light in FindSceneComponents<Light>(scene))
            {
                if (!RoomVisibilityUtility.IsRoomLocalLight(light)) continue;
                List<RoomVolume> members = NormalizeRoomMembership(ResolvePointMembership(light.transform.position, volumes));
                foreach (RoomVolume member in members)
                {
                    RoomVisibilityRoomEntry entry = data.FindEntry(member);
                    if (entry != null) lightMembers[entry].Add(light);
                }
                if (light.lightmapBakeType != LightmapBakeType.Realtime) mixedOrBakedLights.Add(light);
            }

            foreach (DecalProjector decal in FindSceneComponents<DecalProjector>(scene))
            {
                if (RoomVisibilityUtility.IsGameplayObject(decal)) continue;
                List<RoomVolume> members = NormalizeRoomMembership(ResolvePointMembership(decal.transform.position, volumes));
                foreach (RoomVolume member in members)
                {
                    RoomVisibilityRoomEntry entry = data.FindEntry(member);
                    if (entry != null) decalMembers[entry].Add(decal);
                }
            }

            foreach (RoomVisibilityRoomEntry entry in data.Rooms)
            {
                entry.SetBakedMembers(rendererMembers[entry], gameplayVisualMembers[entry], lightMembers[entry], decalMembers[entry]);
            }

            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            LogAudit(scene, data, volumes, unassigned, unassignedGameplayVisuals, sharedRenderers, crossRoomLarge, excludedGameplay, mixedOrBakedLights);
        }

        /// <summary>Batch-mode validation entry point that opens only the L02 scene before using the normal bake path.</summary>
        public static void BakeContainmentForCommandLine()
        {
            const string scenePath = "Assets/_DiveProtocol/Scenes/Levels/SCN_L02_Containment.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BakeCurrentScene();
        }

        /// <summary>Batch-mode entry point for baking one explicitly supplied scene with the shared Room Visibility pipeline.</summary>
        public static void BakeSceneForCommandLine()
        {
            string scenePath = GetCommandLineScenePath();
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BakeCurrentScene();
        }

        [MenuItem("Tools/Dive Protocol/Room Visibility/Preview Selected Room")]
        public static void PreviewSelectedRoom()
        {
            RoomVolume[] selected = GetSelectedVolumes();
            if (selected.Length != 1)
            {
                Debug.LogWarning("[RoomVisibility] Select exactly one RoomVolume for single-room preview.");
                return;
            }

            Preview(selected);
        }

        [MenuItem("Tools/Dive Protocol/Room Visibility/Preview Selected Rooms")]
        public static void PreviewSelectedRooms()
        {
            RoomVolume[] selected = GetSelectedVolumes();
            if (selected.Length == 0)
            {
                Debug.LogWarning("[RoomVisibility] Select one or more RoomVolume objects for preview.");
                return;
            }

            Preview(selected);
        }

        [MenuItem("Tools/Dive Protocol/Room Visibility/Restore Preview")]
        public static void RestorePreview()
        {
            foreach ((Renderer renderer, bool state) in PreviewRendererStates)
                if (renderer != null) renderer.forceRenderingOff = state;
            foreach ((Light light, bool state) in PreviewLightStates)
                if (light != null) light.enabled = state;
            foreach ((DecalProjector decal, bool state) in PreviewDecalStates)
                if (decal != null) decal.enabled = state;
            PreviewRendererStates.Clear();
            PreviewLightStates.Clear();
            PreviewDecalStates.Clear();
        }

        private static void Preview(IReadOnlyCollection<RoomVolume> selected)
        {
            RestorePreview();
            RoomVisibilitySceneData data = UnityEngine.Object.FindFirstObjectByType<RoomVisibilitySceneData>();
            if (data == null)
            {
                Debug.LogWarning("[RoomVisibility] Bake the current scene before using preview.");
                return;
            }

            var visibleRenderers = new HashSet<Renderer>();
            var visibleLights = new HashSet<Light>();
            var visibleDecals = new HashSet<DecalProjector>();
            foreach (RoomVisibilityRoomEntry entry in data.Rooms)
            {
                if (entry == null || !entry.Volumes.Any(selected.Contains)) continue;
                foreach (Renderer renderer in entry.Renderers) if (renderer != null) visibleRenderers.Add(renderer);
                foreach (Renderer renderer in entry.GameplayVisualRenderers) if (renderer != null) visibleRenderers.Add(renderer);
                foreach (Light light in entry.Lights) if (light != null) visibleLights.Add(light);
                foreach (DecalProjector decal in entry.Decals) if (decal != null) visibleDecals.Add(decal);
            }

            foreach (RoomVisibilityRoomEntry entry in data.Rooms)
            {
                if (entry == null) continue;
                foreach (Renderer renderer in entry.Renderers)
                {
                    if (renderer == null || PreviewRendererStates.ContainsKey(renderer)) continue;
                    PreviewRendererStates.Add(renderer, renderer.forceRenderingOff);
                    renderer.forceRenderingOff = !visibleRenderers.Contains(renderer);
                }
                foreach (Renderer renderer in entry.GameplayVisualRenderers)
                {
                    if (renderer == null || PreviewRendererStates.ContainsKey(renderer)) continue;
                    PreviewRendererStates.Add(renderer, renderer.forceRenderingOff);
                    renderer.forceRenderingOff = !visibleRenderers.Contains(renderer);
                }
                foreach (Light light in entry.Lights)
                {
                    if (light == null || PreviewLightStates.ContainsKey(light)) continue;
                    PreviewLightStates.Add(light, light.enabled);
                    light.enabled = visibleLights.Contains(light) && PreviewLightStates[light];
                }
                foreach (DecalProjector decal in entry.Decals)
                {
                    if (decal == null || PreviewDecalStates.ContainsKey(decal)) continue;
                    PreviewDecalStates.Add(decal, decal.enabled);
                    decal.enabled = visibleDecals.Contains(decal) && PreviewDecalStates[decal];
                }
            }
        }

        private static List<RoomVolume> FindAndPrepareVolumes(Scene scene)
        {
            var volumes = new List<RoomVolume>();
            var roomVolumeCandidates = new List<Transform>();
            foreach (Transform transform in FindSceneComponents<Transform>(scene))
            {
                if (transform.GetComponent<BoxCollider>() != null && IsUnderRoomVolumesRoot(transform))
                {
                    roomVolumeCandidates.Add(transform);
                }

                string normalizedName = GetCanonicalVolumeName(transform.name);
                if (!normalizedName.StartsWith("ROOM_", StringComparison.OrdinalIgnoreCase) ||
                    !normalizedName.EndsWith("_VOLUME", StringComparison.OrdinalIgnoreCase) ||
                    transform.GetComponent<BoxCollider>() == null)
                {
                    continue;
                }

                RoomVolume volume = transform.GetComponent<RoomVolume>();
                if (volume == null) volume = Undo.AddComponent<RoomVolume>(transform.gameObject);
                Undo.RecordObject(volume, "Set Room Volume ID");
                volume.EnsureRoomIdFromName();
                EditorUtility.SetDirty(volume);

                MeshRenderer markerRenderer = transform.GetComponent<MeshRenderer>();
                if (markerRenderer != null && markerRenderer.enabled)
                {
                    Undo.RecordObject(markerRenderer, "Hide Room Volume Marker");
                    markerRenderer.enabled = false;
                    EditorUtility.SetDirty(markerRenderer);
                }

                volumes.Add(volume);
            }

            volumes.Sort((left, right) => string.CompareOrdinal(left.RoomId, right.RoomId));
            foreach (IGrouping<string, RoomVolume> group in volumes.GroupBy(volume => volume.RoomId).Where(group => group.Count() > 1))
            {
                Debug.Log($"[RoomVisibility] Room '{group.Key}' uses {group.Count()} authored BoxCollider fragments.");
            }
            if (roomVolumeCandidates.Count != volumes.Count)
            {
                foreach (Transform candidate in roomVolumeCandidates)
                {
                    Debug.Log($"[RoomVisibility] Volume candidate: {RoomVisibilityUtility.GetHierarchyPath(candidate)} | Name={candidate.name}", candidate);
                }
            }
            return volumes;
        }

        private static string GetCommandLineScenePath()
        {
            const string sceneOption = "-roomVisibilityScene";
            string[] commandLine = System.Environment.GetCommandLineArgs();
            int optionIndex = Array.IndexOf(commandLine, sceneOption);
            if (optionIndex < 0 || optionIndex + 1 >= commandLine.Length)
            {
                throw new InvalidOperationException($"[RoomVisibility] Supply a scene path with {sceneOption} <Assets/...unity>.");
            }

            return commandLine[optionIndex + 1];
        }

        private static RoomVisibilitySceneData GetOrCreateSceneData(Scene scene)
        {
            GameObject root = FindSceneComponents<Transform>(scene)
                .Select(transform => transform.gameObject)
                .FirstOrDefault(gameObject => gameObject.name == "_RoomVolumes");
            if (root == null)
            {
                throw new InvalidOperationException("[RoomVisibility] Expected _RoomVolumes scene root was not found.");
            }

            RoomVisibilitySceneData data = root.GetComponent<RoomVisibilitySceneData>();
            if (data == null) data = Undo.AddComponent<RoomVisibilitySceneData>(root);
            if (root.GetComponent<RoomVisibilityManager>() == null) Undo.AddComponent<RoomVisibilityManager>(root);
            return data;
        }

        private static List<RoomVolume> ResolveRendererMembership(Renderer renderer, IReadOnlyList<RoomVolume> volumes)
        {
            var centerRooms = new List<RoomVolume>();
            var intersectingRooms = new List<RoomVolume>();
            foreach (RoomVolume volume in volumes)
            {
                if (volume.ContainsWorldPoint(renderer.bounds.center)) centerRooms.Add(volume);
                if (RoomVisibilityUtility.IntersectsBounds(volume, renderer.bounds)) intersectingRooms.Add(volume);
            }

            if (centerRooms.Count == 1) return centerRooms;
            if (centerRooms.Count == 0 && intersectingRooms.Count == 1) return intersectingRooms;
            return ResolveScoredMembership(intersectingRooms, renderer.bounds);
        }

        private static List<RoomVolume> ResolvePointMembership(Vector3 point, IReadOnlyList<RoomVolume> volumes)
        {
            var contained = volumes.Where(volume => volume.ContainsWorldPoint(point)).ToList();
            if (contained.Count <= 1) return contained;
            contained.Sort((left, right) =>
                Vector3.SqrMagnitude(left.BoxCollider.bounds.center - point)
                    .CompareTo(Vector3.SqrMagnitude(right.BoxCollider.bounds.center - point)));
            float first = Vector3.SqrMagnitude(contained[0].BoxCollider.bounds.center - point);
            float second = Vector3.SqrMagnitude(contained[1].BoxCollider.bounds.center - point);
            return Mathf.Approximately(first, second) ? contained.Take(2).ToList() : new List<RoomVolume> { contained[0] };
        }

        private static List<RoomVolume> ResolveScoredMembership(IReadOnlyList<RoomVolume> candidates, Bounds bounds)
        {
            if (candidates.Count <= 1) return candidates.ToList();
            var scored = candidates
                .Select(volume => new KeyValuePair<RoomVolume, float>(volume, RoomVisibilityUtility.GetMembershipScore(volume, bounds)))
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key.RoomId, StringComparer.Ordinal)
                .ToList();
            if (scored[1].Value > 0f &&
                scored[1].Value / scored[0].Value >= SharedScoreRatio &&
                Mathf.Abs(scored[0].Value - scored[1].Value) <= SharedScoreEpsilon &&
                RoomVisibilityUtility.GetContainedCornerCount(scored[0].Key, bounds) >= MinimumSharedContainedCorners &&
                RoomVisibilityUtility.GetContainedCornerCount(scored[1].Key, bounds) >= MinimumSharedContainedCorners)
            {
                return new List<RoomVolume> { scored[0].Key, scored[1].Key };
            }

            return new List<RoomVolume> { scored[0].Key };
        }

        private static List<RoomVolume> NormalizeRoomMembership(IEnumerable<RoomVolume> volumes)
        {
            return volumes
                .Where(volume => volume != null)
                .GroupBy(volume => volume.RoomId, StringComparer.Ordinal)
                .Select(group => group.OrderBy(volume => RoomVisibilityUtility.GetHierarchyPath(volume.transform), StringComparer.Ordinal).First())
                .ToList();
        }

        private static IEnumerable<T> FindSceneComponents<T>(Scene scene) where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>().Where(component => component != null && component.gameObject.scene == scene);
        }

        private static bool IsUnderRoomVolumesRoot(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (current.name == "_RoomVolumes") return true;
            }

            return false;
        }

        private static string GetCanonicalVolumeName(string objectName)
        {
            string normalized = objectName.Trim();
            int cloneSuffix = normalized.LastIndexOf(" (", StringComparison.Ordinal);
            if (cloneSuffix >= 0 && normalized.EndsWith(")", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(0, cloneSuffix).Trim();
            }

            return normalized;
        }

        private static RoomVolume[] GetSelectedVolumes()
        {
            return Selection.GetFiltered<RoomVolume>(SelectionMode.Editable).Distinct().ToArray();
        }

        private static void LogAudit(
            Scene scene,
            RoomVisibilitySceneData data,
            IReadOnlyList<RoomVolume> volumes,
            IReadOnlyList<Renderer> unassigned,
            IReadOnlyList<Renderer> unassignedGameplayVisuals,
            IReadOnlyDictionary<Renderer, List<RoomVolume>> shared,
            IReadOnlyList<Renderer> crossRoomLarge,
            IReadOnlyList<Renderer> excludedGameplay,
            IReadOnlyList<Light> mixedOrBakedLights)
        {
            Debug.Log($"[RoomVisibility] Bake audit: Scene={scene.name}, LogicalRooms={data.Rooms.Count}, PhysicalRoomVolumes={volumes.Count}, AssignedEnvironmentRenderers={data.Rooms.Sum(entry => entry.Renderers.Count)}, AssignedGameplayVisuals={data.Rooms.Sum(entry => entry.GameplayVisualRenderers.Count)}, Unassigned={unassigned.Count}, UnassignedGameplayVisuals={unassignedGameplayVisuals.Count}, Shared={shared.Count}, CrossRoomLarge={crossRoomLarge.Count}, ExcludedGameplay={excludedGameplay.Count}, MixedOrBakedLocalLights={mixedOrBakedLights.Count}.");
            foreach (RoomVisibilityRoomEntry entry in data.Rooms)
            {
                Debug.Log($"[RoomVisibility] {entry.RoomId}: Renderer={entry.Renderers.Count}, GameplayVisual={entry.GameplayVisualRenderers.Count}, Light={entry.Lights.Count}, Decal={entry.Decals.Count}.");
            }

            foreach (KeyValuePair<Renderer, List<RoomVolume>> item in shared)
            {
                if (crossRoomLarge.Contains(item.Key)) continue;
                Debug.LogWarning($"[RoomVisibility] Shared renderer review: {RoomVisibilityUtility.GetHierarchyPath(item.Key.transform)} | Rooms={string.Join(", ", item.Value.Select(volume => volume.RoomId))}", item.Key);
            }
            foreach (Renderer renderer in crossRoomLarge)
            {
                string membership = string.Join("; ", shared[renderer].Select(volume =>
                    $"{volume.RoomId}(Center={volume.ContainsWorldPoint(renderer.bounds.center)}, Corners={RoomVisibilityUtility.GetContainedCornerCount(volume, renderer.bounds)}, Score={RoomVisibilityUtility.GetMembershipScore(volume, renderer.bounds):F3})"));
                Debug.LogWarning($"[RoomVisibility] Cross-Room Large Renderer: {RoomVisibilityUtility.GetHierarchyPath(renderer.transform)} | Size={renderer.bounds.size} | Membership={membership}", renderer);
            }
            foreach (Renderer renderer in unassigned.Take(30))
            {
                Debug.LogWarning($"[RoomVisibility] Unassigned environment renderer: {RoomVisibilityUtility.GetHierarchyPath(renderer.transform)} | Bounds={renderer.bounds.size}", renderer);
            }
            foreach (Renderer renderer in unassignedGameplayVisuals.Take(30))
            {
                Debug.LogWarning($"[RoomVisibility] Unassigned enemy/boss visual: {RoomVisibilityUtility.GetHierarchyPath(renderer.transform)} | Bounds={renderer.bounds.size}", renderer);
            }
            foreach (Light light in mixedOrBakedLights)
            {
                Debug.LogWarning($"[RoomVisibility] Baked Lighting Isolation Warning: {RoomVisibilityUtility.GetHierarchyPath(light.transform)} | Mode={light.lightmapBakeType}", light);
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode) RestorePreview();
        }

        private static void HandleSceneClosing(Scene scene, bool removingScene)
        {
            RestorePreview();
        }
    }
}
