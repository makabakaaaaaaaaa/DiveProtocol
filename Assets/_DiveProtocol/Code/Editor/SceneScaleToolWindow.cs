using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace DiveProtocol.Editor
{
    /// <summary>
    /// Maintenance tool for applying a world-space scale pass to the currently open gameplay scene.
    /// </summary>
    public sealed class SceneScaleToolWindow : EditorWindow
    {
        private const string MenuPath = "Dive Protocol/Tools/Scene Scale Tool";

        private enum PivotMode
        {
            WorldOrigin,
            SceneBoundsCenter,
            CustomTransform,
            CustomPosition
        }

        private enum ScaleCategory
        {
            Rooms,
            Connections,
            Doors,
            Triggers,
            PatrolPoints,
            SpawnPoints,
            Pickups,
            Lighting,
            NavigationHelpers,
            Gameplay,
            Environment
        }

        [SerializeField] private float _scaleFactor = 1.15f;
        [SerializeField] private PivotMode _pivotMode = PivotMode.SceneBoundsCenter;
        [SerializeField] private Transform _customPivotTransform;
        [SerializeField] private Vector3 _customPivotPosition;
        [SerializeField] private bool _dryRunOnly = true;

        [SerializeField] private bool _includeRooms = true;
        [SerializeField] private bool _includeConnections = true;
        [SerializeField] private bool _includeGameplayPickups = true;
        [SerializeField] private bool _includeDoors = true;
        [SerializeField] private bool _includeTriggers = true;
        [SerializeField] private bool _includeSpawnPoints = true;
        [SerializeField] private bool _includePatrolPoints = true;
        [SerializeField] private bool _includeLighting;
        [SerializeField] private bool _includeNavigationHelpers;

        [SerializeField] private bool _excludeEnemies = true;
        [SerializeField] private bool _excludePlayer = true;
        [SerializeField] private bool _excludeCameras = true;
        [SerializeField] private bool _excludeUi = true;
        [SerializeField] private bool _excludeSystems = true;
        [SerializeField] private bool _excludeAudio = true;
        [SerializeField] private bool _excludePixelDisplayCanvas = true;
        [SerializeField] private bool _excludeDebug = true;

        [SerializeField] private bool _scaleMarkerObjectSize;
        [SerializeField] private bool _scaleLightRange;

        private Vector2 _scrollPosition;
        private string _lastReport = "Run Dry Run to preview scene scaling.";

        [MenuItem(MenuPath)]
        public static void Open()
        {
            SceneScaleToolWindow window = GetWindow<SceneScaleToolWindow>("Scene Scale Tool");
            window.minSize = new Vector2(460f, 560f);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Scene Scale Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scales selected gameplay-space scene objects in the currently open scene. It does not modify prefab assets directly and records Undo for applied changes.",
                MessageType.Info);

            DrawSceneInfo();
            DrawBaseParameters();
            DrawIncludeOptions();
            DrawExcludeOptions();
            DrawAdvancedOptions();
            DrawActions();
            DrawLastReport();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSceneInfo()
        {
            Scene scene = SceneManager.GetActiveScene();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Scene", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Name", scene.IsValid() ? scene.name : "<invalid>");
            EditorGUILayout.LabelField("Path", string.IsNullOrEmpty(scene.path) ? "<unsaved scene>" : scene.path);
        }

        private void DrawBaseParameters()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Parameters", EditorStyles.boldLabel);
            _scaleFactor = EditorGUILayout.FloatField("Scale Factor", _scaleFactor);
            _pivotMode = (PivotMode)EditorGUILayout.EnumPopup("Pivot Mode", _pivotMode);

            using (new EditorGUI.DisabledScope(_pivotMode != PivotMode.CustomTransform))
            {
                _customPivotTransform = (Transform)EditorGUILayout.ObjectField("Custom Pivot Transform", _customPivotTransform, typeof(Transform), true);
            }

            using (new EditorGUI.DisabledScope(_pivotMode != PivotMode.CustomPosition))
            {
                _customPivotPosition = EditorGUILayout.Vector3Field("Custom Pivot Position", _customPivotPosition);
            }

            _dryRunOnly = EditorGUILayout.Toggle("Dry Run Only", _dryRunOnly);
        }

        private void DrawIncludeOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Include", EditorStyles.boldLabel);
            _includeRooms = EditorGUILayout.Toggle("Include Rooms", _includeRooms);
            _includeConnections = EditorGUILayout.Toggle("Include Connections", _includeConnections);
            _includeGameplayPickups = EditorGUILayout.Toggle("Include Gameplay Pickups", _includeGameplayPickups);
            _includeDoors = EditorGUILayout.Toggle("Include Doors", _includeDoors);
            _includeTriggers = EditorGUILayout.Toggle("Include Triggers", _includeTriggers);
            _includeSpawnPoints = EditorGUILayout.Toggle("Include Spawn Points", _includeSpawnPoints);
            _includePatrolPoints = EditorGUILayout.Toggle("Include Patrol Points", _includePatrolPoints);
            _includeLighting = EditorGUILayout.Toggle("Include Lighting", _includeLighting);
            _includeNavigationHelpers = EditorGUILayout.Toggle("Include Navigation Helpers", _includeNavigationHelpers);
        }

        private void DrawExcludeOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Exclude", EditorStyles.boldLabel);
            _excludeEnemies = EditorGUILayout.Toggle("Exclude Enemies", _excludeEnemies);
            _excludePlayer = EditorGUILayout.Toggle("Exclude Player", _excludePlayer);
            _excludeCameras = EditorGUILayout.Toggle("Exclude Cameras", _excludeCameras);
            _excludeUi = EditorGUILayout.Toggle("Exclude UI", _excludeUi);
            _excludeSystems = EditorGUILayout.Toggle("Exclude Systems", _excludeSystems);
            _excludeAudio = EditorGUILayout.Toggle("Exclude Audio", _excludeAudio);
            _excludePixelDisplayCanvas = EditorGUILayout.Toggle("Exclude PixelDisplayCanvas", _excludePixelDisplayCanvas);
            _excludeDebug = EditorGUILayout.Toggle("Exclude Debug", _excludeDebug);
        }

        private void DrawAdvancedOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            _scaleMarkerObjectSize = EditorGUILayout.Toggle("Scale Marker Object Size", _scaleMarkerObjectSize);
            _scaleLightRange = EditorGUILayout.Toggle("Scale Light Range", _scaleLightRange);
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Dry Run / Preview Report", GUILayout.Height(28f)))
                {
                    RunScalePass(true);
                }

                if (GUILayout.Button("Apply Scale", GUILayout.Height(28f)))
                {
                    if (_dryRunOnly)
                    {
                        RunScalePass(true);
                        return;
                    }

                    bool confirmed = EditorUtility.DisplayDialog(
                        "Apply Scene Scale",
                        "This will modify objects in the currently open scene and record Undo. It will not save the scene automatically.\n\nContinue?",
                        "Apply",
                        "Cancel");

                    if (confirmed)
                    {
                        RunScalePass(false);
                    }
                }
            }
        }

        private void DrawLastReport()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Report", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_lastReport, GUILayout.MinHeight(240f));
        }

        private void RunScalePass(bool dryRun)
        {
            if (_scaleFactor <= 0f)
            {
                _lastReport = "Scene Scale Tool failed: Scale Factor must be greater than 0.";
                Debug.LogError(_lastReport);
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                _lastReport = "Scene Scale Tool failed: no valid active scene is loaded.";
                Debug.LogError(_lastReport);
                return;
            }

            List<ScaleEntry> entries = CollectEntries(scene, out ReportCounters excluded, out List<string> skipped);
            Vector3 pivot = ResolvePivot(entries);

            if (!dryRun)
            {
                ApplyEntries(entries, pivot);
                EditorSceneManager.MarkSceneDirty(scene);
            }

            _lastReport = BuildReport(scene, pivot, entries, excluded, skipped, dryRun);
            Debug.Log(_lastReport);
        }

        private List<ScaleEntry> CollectEntries(Scene scene, out ReportCounters excluded, out List<string> skipped)
        {
            excluded = new ReportCounters();
            skipped = new List<string>();

            List<ScaleEntry> entries = new List<ScaleEntry>();
            HashSet<Transform> compositeRoots = new HashSet<Transform>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    Transform transform = transforms[j];
                    string path = GetPath(transform);

                    if (IsDescendantOfAny(transform, compositeRoots))
                    {
                        skipped.Add(path + " | child of a composite object handled as one unit");
                        continue;
                    }

                    if (TryGetExcludedCategory(transform, out string excludedCategory))
                    {
                        excluded.Increment(excludedCategory);
                        continue;
                    }

                    if (!TryCreateEntry(transform, path, out ScaleEntry entry))
                    {
                        continue;
                    }

                    entries.Add(entry);

                    if (entry.IsCompositeRoot)
                    {
                        compositeRoots.Add(transform);
                    }
                }
            }

            return entries;
        }

        private bool TryCreateEntry(Transform transform, string path, out ScaleEntry entry)
        {
            entry = default;
            string rootName = transform.root.name;
            bool underRooms = IsRoot(rootName, "_Rooms") || IsRoot(rootName, "FLOOR_01") || IsRoot(rootName, "FLOOR_02");
            bool underConnections = IsRoot(rootName, "_Connections");
            bool underGameplay = IsRoot(rootName, "_Gameplay");
            bool underNavigation = IsRoot(rootName, "_Navigation");
            bool underLighting = IsRoot(rootName, "_Lighting");
            bool underEnvironment = IsRoot(rootName, "_Environment");

            bool isDoor = IsDoor(transform);
            bool isTrigger = IsTrigger(transform);
            bool isSpawnPoint = IsSpawnPoint(transform);
            bool isPatrolPoint = IsPatrolPoint(transform);
            bool isPickup = IsPickup(transform);
            bool isLight = transform.GetComponent<Light>() != null || transform.GetComponent<ReflectionProbe>() != null;
            bool isNavigationHelper = underNavigation || ContainsAny(transform.name, "NavMesh", "Navigation");

            ScaleCategory category;
            if (isDoor && _includeDoors)
            {
                category = ScaleCategory.Doors;
            }
            else if (isTrigger && _includeTriggers)
            {
                category = ScaleCategory.Triggers;
            }
            else if (isSpawnPoint && _includeSpawnPoints)
            {
                category = ScaleCategory.SpawnPoints;
            }
            else if (isPatrolPoint && _includePatrolPoints)
            {
                category = ScaleCategory.PatrolPoints;
            }
            else if (isPickup && _includeGameplayPickups)
            {
                category = ScaleCategory.Pickups;
            }
            else if (isLight && _includeLighting)
            {
                category = ScaleCategory.Lighting;
            }
            else if (isNavigationHelper && _includeNavigationHelpers)
            {
                category = ScaleCategory.NavigationHelpers;
            }
            else if (underRooms && _includeRooms)
            {
                category = ScaleCategory.Rooms;
            }
            else if (underConnections && _includeConnections)
            {
                category = ScaleCategory.Connections;
            }
            else if (underEnvironment && _includeRooms)
            {
                category = ScaleCategory.Environment;
            }
            else if (underGameplay && (isDoor || isTrigger || isSpawnPoint || isPatrolPoint || isPickup))
            {
                category = ScaleCategory.Gameplay;
            }
            else
            {
                return false;
            }

            bool isMarker = isSpawnPoint || isPatrolPoint || IsMarker(transform);
            bool hasScalableShape = HasScalableShape(transform);
            bool scaleSize = !isMarker || _scaleMarkerObjectSize;
            if (!hasScalableShape && !isDoor && !isTrigger && !isPickup)
            {
                scaleSize = false;
            }

            bool isComposite = isDoor && IsDoorRoot(transform);
            entry = new ScaleEntry(transform, path, category, scaleSize, isLight, isComposite);
            return true;
        }

        private Vector3 ResolvePivot(List<ScaleEntry> entries)
        {
            switch (_pivotMode)
            {
                case PivotMode.WorldOrigin:
                    return Vector3.zero;
                case PivotMode.CustomTransform:
                    return _customPivotTransform != null ? _customPivotTransform.position : Vector3.zero;
                case PivotMode.CustomPosition:
                    return _customPivotPosition;
                case PivotMode.SceneBoundsCenter:
                default:
                    return ResolveBoundsCenter(entries);
            }
        }

        private Vector3 ResolveBoundsCenter(List<ScaleEntry> entries)
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0; i < entries.Count; i++)
            {
                Transform transform = entries[i].Transform;
                Renderer renderer = transform.GetComponent<Renderer>();
                Collider collider = transform.GetComponent<Collider>();

                if (renderer != null)
                {
                    Encapsulate(ref bounds, renderer.bounds, ref hasBounds);
                }

                if (collider != null)
                {
                    Encapsulate(ref bounds, collider.bounds, ref hasBounds);
                }
            }

            return hasBounds ? bounds.center : Vector3.zero;
        }

        private void ApplyEntries(List<ScaleEntry> entries, Vector3 pivot)
        {
            List<UnityEngine.Object> undoObjects = new List<UnityEngine.Object>();
            for (int i = 0; i < entries.Count; i++)
            {
                undoObjects.Add(entries[i].Transform);
                if (_scaleLightRange && entries[i].IsLight)
                {
                    Light light = entries[i].Transform.GetComponent<Light>();
                    if (light != null)
                    {
                        undoObjects.Add(light);
                    }
                }
            }

            Undo.RecordObjects(undoObjects.ToArray(), "Scale Gameplay Scene Space");

            for (int i = 0; i < entries.Count; i++)
            {
                ScaleEntry entry = entries[i];
                if (entry.ScaleObjectSize)
                {
                    entry.Transform.localScale *= _scaleFactor;
                }

                if (_scaleLightRange && entry.IsLight)
                {
                    Light light = entry.Transform.GetComponent<Light>();
                    if (light != null)
                    {
                        light.range *= _scaleFactor;
                    }
                }
            }

            entries.Sort((left, right) => GetDepth(left.Transform).CompareTo(GetDepth(right.Transform)));
            for (int i = 0; i < entries.Count; i++)
            {
                Transform transform = entries[i].Transform;
                transform.position = pivot + (transform.position - pivot) * _scaleFactor;
                EditorUtility.SetDirty(transform);
            }
        }

        private string BuildReport(Scene scene, Vector3 pivot, List<ScaleEntry> entries, ReportCounters excluded, List<string> skipped, bool dryRun)
        {
            ReportCounters processed = new ReportCounters();
            for (int i = 0; i < entries.Count; i++)
            {
                processed.Increment(entries[i].Category.ToString());
            }

            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine("Scene Scale Tool Report");
            builder.AppendLine($"Mode: {(dryRun ? "Dry Run" : "Applied")}");
            builder.AppendLine($"Scene: {scene.name}");
            builder.AppendLine($"Scale Factor: {_scaleFactor:0.###}");
            builder.AppendLine($"Pivot: {pivot.x:0.###}, {pivot.y:0.###}, {pivot.z:0.###}");
            builder.AppendLine();

            builder.AppendLine("Processed:");
            processed.AppendLines(builder, "Rooms");
            processed.AppendLines(builder, "Connections");
            processed.AppendLines(builder, "Doors");
            processed.AppendLines(builder, "Triggers");
            processed.AppendLines(builder, "PatrolPoints");
            processed.AppendLines(builder, "SpawnPoints");
            processed.AppendLines(builder, "Pickups");
            processed.AppendLines(builder, "Lighting");
            processed.AppendLines(builder, "NavigationHelpers");
            processed.AppendLines(builder, "Gameplay");
            processed.AppendLines(builder, "Environment");
            builder.AppendLine($"- Total: {entries.Count}");
            builder.AppendLine();

            builder.AppendLine("Excluded:");
            excluded.AppendAll(builder);
            builder.AppendLine();

            builder.AppendLine("Skipped / Need Manual Check:");
            if (skipped.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                int count = Mathf.Min(skipped.Count, 80);
                for (int i = 0; i < count; i++)
                {
                    builder.AppendLine("- " + skipped[i]);
                }

                if (skipped.Count > count)
                {
                    builder.AppendLine($"- ... {skipped.Count - count} more");
                }
            }

            if (!dryRun)
            {
                builder.AppendLine();
                builder.AppendLine("Scene marked dirty. Save manually after inspection. Re-bake NavMesh before gameplay testing.");
            }

            return builder.ToString();
        }

        private bool TryGetExcludedCategory(Transform transform, out string category)
        {
            category = string.Empty;

            if (_excludePixelDisplayCanvas && HasSelfOrAncestorName(transform, "PixelDisplayCanvas", "GameplayDisplay", "PixelBackground"))
            {
                category = "PixelDisplay";
                return true;
            }

            if (_excludeCameras && (transform.GetComponent<Camera>() != null || HasSelfOrAncestorName(transform, "_Cameras", "Main Camera", "DisplayCamera", "Cinemachine")))
            {
                category = "Cameras";
                return true;
            }

            if (_excludeUi && (transform.GetComponent<Canvas>() != null || transform.GetComponent<RectTransform>() != null || transform.GetComponent<EventSystem>() != null || HasSelfOrAncestorName(transform, "_UI", "Canvas", "EventSystem", "GameplayStatusHUD", "InteractionPromptUI", "HUD")))
            {
                category = "UI";
                return true;
            }

            if (_excludeAudio && (transform.GetComponent<AudioSource>() != null || transform.GetComponent<AudioListener>() != null || HasSelfOrAncestorName(transform, "_Audio", "Audio")))
            {
                category = "Audio";
                return true;
            }

            if (_excludeDebug && HasSelfOrAncestorName(transform, "Debug", "DebugOverlay"))
            {
                category = "Debug";
                return true;
            }

            if (_excludePlayer && !IsSpawnPoint(transform) && (HasComponentNamed(transform, "PlayerMovement", "PlayerInteractor", "PlayerHitscanWeapon", "PlayerDeathController") || ContainsAny(transform.name, "PF_Player", "Player_Basic")))
            {
                category = "Player";
                return true;
            }

            if (_excludeEnemies && !IsSpawnPoint(transform) && !IsPatrolPoint(transform) && (HasComponentNamed(transform, "EnemyChaseController", "EnemyPatrolChaseController", "EnemyContactAttack", "EnemyWaveSpawner") || ContainsAny(transform.name, "Enemy_", "PF_Enemy", "RuntimeEnemies", "_Enemy", "Enemies")))
            {
                category = "Enemies";
                return true;
            }

            if (_excludeSystems && (HasComponentNamed(transform, "AppRoot", "RunManager", "SceneLoader", "GameStateMachine", "MultiFloorVisibilityController") || HasSelfOrAncestorName(transform, "_Systems", "Systems", "AppRoot", "Bootstrap", "FlowController")))
            {
                category = "Systems";
                return true;
            }

            return false;
        }

        private static bool IsDoor(Transform transform)
        {
            return HasComponentNamed(transform, "DoorController", "DoorInteractable", "AutomaticSlidingDoor", "AutomaticSlidingDoorTrigger", "SurvivalUnlockDoorInteractable")
                   || ContainsAny(transform.name, "DoorRoot", "Door_", "HingePivot", "DoorLeaf", "OneWayUnlockSideMarker", "AllowedSideReference");
        }

        private static bool IsDoorRoot(Transform transform)
        {
            return HasComponentNamed(transform, "DoorController", "DoorInteractable", "AutomaticSlidingDoor")
                   || ContainsAny(transform.name, "DoorRoot", "Door_");
        }

        private static bool IsTrigger(Transform transform)
        {
            Collider collider = transform.GetComponent<Collider>();
            return collider != null && collider.isTrigger
                   || HasComponentNamed(transform, "LevelExitTrigger", "FloorTransitionVolume", "SurvivalUnlockDoorInteractable", "RequiredItemInteractable", "AutomaticSlidingDoorTrigger")
                   || ContainsAny(transform.name, "Trigger", "Volume");
        }

        private static bool IsSpawnPoint(Transform transform)
        {
            return ContainsAny(transform.name, "SpawnPoint", "PlayerSpawnPoint", "EnemySpawnPoint", "WaveSpawnPoint");
        }

        private static bool IsPatrolPoint(Transform transform)
        {
            return ContainsAny(transform.name, "PatrolPoint", "PatrolRoute");
        }

        private static bool IsPickup(Transform transform)
        {
            return HasComponentNamed(transform, "PickupInteractable", "ResourcePickupInteractable")
                   || ContainsAny(transform.name, "Pickup", "Keycard", "Fuse", "Medkit", "AmmoPack", "BuildPart");
        }

        private static bool IsMarker(Transform transform)
        {
            return ContainsAny(transform.name, "Marker", "Point", "Anchor", "Reference", "LevelExit");
        }

        private static bool HasScalableShape(Transform transform)
        {
            return transform.GetComponent<Renderer>() != null
                   || transform.GetComponent<Collider>() != null
                   || transform.GetComponent<NavMeshObstacle>() != null
                   || transform.GetComponent<Light>() != null;
        }

        private static bool HasComponentNamed(Transform transform, params string[] typeNames)
        {
            Component[] components = transform.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                string componentName = component.GetType().Name;
                for (int j = 0; j < typeNames.Length; j++)
                {
                    if (string.Equals(componentName, typeNames[j], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < needles.Length; i++)
            {
                if (value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSelfOrAncestorName(Transform transform, params string[] names)
        {
            Transform current = transform;
            while (current != null)
            {
                if (ContainsAny(current.name, names))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsDescendantOfAny(Transform transform, HashSet<Transform> ancestors)
        {
            Transform current = transform.parent;
            while (current != null)
            {
                if (ancestors.Contains(current))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            Transform current = transform.parent;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static string GetPath(Transform transform)
        {
            Stack<string> names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return "/" + string.Join("/", names.ToArray());
        }

        private static bool IsRoot(string rootName, string expected)
        {
            return string.Equals(rootName, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static void Encapsulate(ref Bounds bounds, Bounds value, ref bool hasBounds)
        {
            if (!hasBounds)
            {
                bounds = value;
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(value);
        }

        private readonly struct ScaleEntry
        {
            public Transform Transform { get; }
            public string Path { get; }
            public ScaleCategory Category { get; }
            public bool ScaleObjectSize { get; }
            public bool IsLight { get; }
            public bool IsCompositeRoot { get; }

            public ScaleEntry(Transform transform, string path, ScaleCategory category, bool scaleObjectSize, bool isLight, bool isCompositeRoot)
            {
                Transform = transform;
                Path = path;
                Category = category;
                ScaleObjectSize = scaleObjectSize;
                IsLight = isLight;
                IsCompositeRoot = isCompositeRoot;
            }
        }

        private sealed class ReportCounters
        {
            private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

            public void Increment(string key)
            {
                if (_counts.ContainsKey(key))
                {
                    _counts[key]++;
                    return;
                }

                _counts.Add(key, 1);
            }

            public void AppendLines(StringBuilder builder, string key)
            {
                _counts.TryGetValue(key, out int count);
                builder.AppendLine($"- {key}: {count}");
            }

            public void AppendAll(StringBuilder builder)
            {
                if (_counts.Count == 0)
                {
                    builder.AppendLine("- None");
                    return;
                }

                foreach (KeyValuePair<string, int> pair in _counts)
                {
                    builder.AppendLine($"- {pair.Key}: {pair.Value}");
                }
            }
        }
    }
}
