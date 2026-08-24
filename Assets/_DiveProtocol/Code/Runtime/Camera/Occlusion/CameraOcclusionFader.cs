using System.Collections.Generic;
using DiveProtocol.Bosses;
using DiveProtocol.Doors;
using DiveProtocol.Enemies.CorpseReanimation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.CameraSystem
{
    /// <summary>
    /// Fades environment renderers that physically obstruct the camera's view of the player.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraOcclusionFader : MonoBehaviour
    {
        private const float ChestHeightRatio = 0.65f;
        private const float FullyVisibleAlpha = 1f;
        private const float RestoreEpsilon = 0.001f;

        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private bool _autoFindPlayerByTag = true;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField, Min(0.05f)] private float _targetSearchInterval = 0.25f;

        [Header("Occlusion Cast")]
        [Tooltip("0 derives a radius from the player's actual collider or renderer height.")]
        [SerializeField, Min(0f)] private float _castRadius;
        [SerializeField] private LayerMask _occluderLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(1)] private int _maximumRenderersPerRoot = 32;
        [Tooltip("Small world-space expansion used to find wallpaper or panel renderers attached to a wall.")]
        [SerializeField, Range(0.05f, 0.15f)] private float _companionBoundsPadding = 0.1f;
        [SerializeField, Min(0.1f)] private float _floorLikeMinimumSpan = 2.5f;
        [SerializeField, Range(0.01f, 0.2f)] private float _floorLikeMaximumThicknessRatio = 0.08f;

        [Header("Fade")]
        [SerializeField, Range(0.05f, 1f)] private float _fadeAmount = 0.22f;
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.18f;

        private readonly RaycastHit[] _castHits = new RaycastHit[64];
        private readonly HashSet<Renderer> _currentOccluders = new HashSet<Renderer>();
        private readonly HashSet<DecalProjector> _currentDecalOccluders = new HashSet<DecalProjector>();
        private readonly Dictionary<Renderer, FadeRendererState> _activeFades =
            new Dictionary<Renderer, FadeRendererState>();
        private readonly Dictionary<DecalProjector, FadeDecalState> _activeDecalFades =
            new Dictionary<DecalProjector, FadeDecalState>();
        private readonly Dictionary<Material, FadeMaterialVariant> _materialVariants =
            new Dictionary<Material, FadeMaterialVariant>();
        private readonly List<Renderer> _releaseBuffer = new List<Renderer>();
        private readonly List<DecalProjector> _decalReleaseBuffer = new List<DecalProjector>();

        private Camera _camera;
        private FixedAngleFollowCamera _followCamera;
        private float _nextTargetSearchTime;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _followCamera = GetComponent<FixedAngleFollowCamera>();
        }

        private void LateUpdate()
        {
            ResolveTarget();
            if (_camera == null || _target == null)
            {
                UpdateActiveFades();
                return;
            }

            _currentOccluders.Clear();
            _currentDecalOccluders.Clear();
            CollectOccluders();
            UpdateActiveFades();
        }

        private void OnDisable()
        {
            RestoreAllFades();
        }

        private void OnDestroy()
        {
            RestoreAllFades();
        }

        private void ResolveTarget()
        {
            if (_target != null)
            {
                return;
            }

            if (_followCamera != null && _followCamera.Target != null)
            {
                _target = _followCamera.Target;
                return;
            }

            if (!_autoFindPlayerByTag || Time.unscaledTime < _nextTargetSearchTime ||
                string.IsNullOrWhiteSpace(_playerTag))
            {
                return;
            }

            _nextTargetSearchTime = Time.unscaledTime + _targetSearchInterval;
            try
            {
                GameObject player = GameObject.FindGameObjectWithTag(_playerTag);
                if (player != null)
                {
                    _target = player.transform;
                }
            }
            catch (UnityException)
            {
                _autoFindPlayerByTag = false;
            }
        }

        private void CollectOccluders()
        {
            Vector3 chestTarget = GetChestTarget(_target);
            Vector3 castVector = chestTarget - _camera.transform.position;
            float castDistance = castVector.magnitude;
            if (castDistance <= 0.01f)
            {
                return;
            }

            float radius = _castRadius > 0f ? _castRadius : GetDerivedCastRadius();
            int hitCount = Physics.SphereCastNonAlloc(
                _camera.transform.position,
                radius,
                castVector / castDistance,
                _castHits,
                castDistance,
                _occluderLayers,
                QueryTriggerInteraction.Ignore);

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider hitCollider = _castHits[hitIndex].collider;
                if (hitCollider == null || IsExcludedGameplayObject(hitCollider))
                {
                    continue;
                }

                Transform occluderRoot = FindOccluderRoot(hitCollider);
                if (occluderRoot != null)
                {
                    CollectRootRenderers(occluderRoot);

                    Renderer primaryRenderer = FindPrimaryRenderer(hitCollider, occluderRoot);
                    if (primaryRenderer != null)
                    {
                        CollectLocalCompanions(primaryRenderer);
                    }
                }
            }
        }

        private void UpdateActiveFades()
        {
            foreach (Renderer renderer in _currentOccluders)
            {
                if (!_activeFades.TryGetValue(renderer, out FadeRendererState state))
                {
                    state = CreateFadeState(renderer);
                    if (state == null)
                    {
                        continue;
                    }

                    _activeFades.Add(renderer, state);
                }

                state.SetTarget(_fadeAmount);
            }

            _releaseBuffer.Clear();
            foreach (KeyValuePair<Renderer, FadeRendererState> entry in _activeFades)
            {
                FadeRendererState state = entry.Value;
                if (!_currentOccluders.Contains(entry.Key))
                {
                    state.SetTarget(FullyVisibleAlpha);
                }

                if (state.Tick(_fadeDuration))
                {
                    _releaseBuffer.Add(entry.Key);
                }
            }

            foreach (Renderer renderer in _releaseBuffer)
            {
                FadeRendererState state = _activeFades[renderer];
                state.Restore();
                ReleaseVariants(state);
                _activeFades.Remove(renderer);
            }

            UpdateActiveDecalFades();
        }

        private void UpdateActiveDecalFades()
        {
            foreach (DecalProjector decal in _currentDecalOccluders)
            {
                if (!_activeDecalFades.TryGetValue(decal, out FadeDecalState state))
                {
                    state = FadeDecalState.Create(decal);
                    if (state == null)
                    {
                        continue;
                    }

                    _activeDecalFades.Add(decal, state);
                }

                state.SetTarget(_fadeAmount);
            }

            _decalReleaseBuffer.Clear();
            foreach (KeyValuePair<DecalProjector, FadeDecalState> entry in _activeDecalFades)
            {
                FadeDecalState state = entry.Value;
                if (!_currentDecalOccluders.Contains(entry.Key))
                {
                    state.SetTarget(FullyVisibleAlpha);
                }

                if (state.Tick(_fadeDuration))
                {
                    _decalReleaseBuffer.Add(entry.Key);
                }
            }

            foreach (DecalProjector decal in _decalReleaseBuffer)
            {
                FadeDecalState state = _activeDecalFades[decal];
                state.Restore();
                _activeDecalFades.Remove(decal);
            }
        }

        private FadeRendererState CreateFadeState(Renderer renderer)
        {
            if (renderer == null || !renderer.enabled)
            {
                return null;
            }

            Material[] originalMaterials = renderer.sharedMaterials;
            if (originalMaterials == null || originalMaterials.Length == 0)
            {
                return null;
            }

            var variants = new FadeMaterialVariant[originalMaterials.Length];
            for (int materialIndex = 0; materialIndex < originalMaterials.Length; materialIndex++)
            {
                if (!TryAcquireVariant(originalMaterials[materialIndex], out FadeMaterialVariant variant))
                {
                    ReleaseVariants(variants);
                    return FadeRendererState.CreateFallback(renderer);
                }

                variants[materialIndex] = variant;
            }

            return FadeRendererState.CreateMaterialFade(renderer, originalMaterials, variants);
        }

        private bool TryAcquireVariant(Material source, out FadeMaterialVariant variant)
        {
            variant = null;
            if (source == null || !IsSupportedUrpMaterial(source, out int colorProperty))
            {
                return false;
            }

            if (!_materialVariants.TryGetValue(source, out variant))
            {
                var material = new Material(source)
                {
                    name = source.name + " (Camera Occlusion Runtime)",
                    hideFlags = HideFlags.DontSave
                };

                ConfigureTransparentUrpMaterial(material);
                variant = new FadeMaterialVariant(source, material, colorProperty, source.GetColor(colorProperty));
                _materialVariants.Add(source, variant);
            }

            variant.ReferenceCount++;
            return true;
        }

        private void ReleaseVariants(FadeRendererState state)
        {
            ReleaseVariants(state.Variants);
        }

        private void ReleaseVariants(IEnumerable<FadeMaterialVariant> variants)
        {
            if (variants == null)
            {
                return;
            }

            foreach (FadeMaterialVariant variant in variants)
            {
                if (variant == null)
                {
                    continue;
                }

                variant.ReferenceCount--;
                if (variant.ReferenceCount > 0)
                {
                    continue;
                }

                _materialVariants.Remove(variant.Source);
                Destroy(variant.Material);
            }
        }

        private void CollectRootRenderers(Transform occluderRoot)
        {
            Renderer[] renderers = occluderRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 || renderers.Length > _maximumRenderersPerRoot)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (IsFadeableRenderer(renderer) &&
                    renderer.enabled &&
                    !IsFloorLikeRenderer(renderer) &&
                    !IsExcludedGameplayObject(renderer))
                {
                    _currentOccluders.Add(renderer);
                }
            }
        }

        private Renderer FindPrimaryRenderer(Collider hitCollider, Transform occluderRoot)
        {
            Renderer[] localRenderers = hitCollider.GetComponentsInParent<Renderer>(true);
            foreach (Renderer renderer in localRenderers)
            {
                if (IsFadeableRenderer(renderer) &&
                    renderer.enabled &&
                    !IsFloorLikeRenderer(renderer) &&
                    !IsExcludedGameplayObject(renderer))
                {
                    return renderer;
                }
            }

            Renderer rootRenderer = occluderRoot.GetComponent<Renderer>();
            return IsFadeableRenderer(rootRenderer) &&
                   rootRenderer.enabled &&
                   !IsFloorLikeRenderer(rootRenderer) &&
                   !IsExcludedGameplayObject(rootRenderer)
                ? rootRenderer
                : null;
        }

        private void CollectLocalCompanions(Renderer primaryRenderer)
        {
            CollectCompanionAt(primaryRenderer.transform, primaryRenderer);
            CollectDirectChildCompanions(primaryRenderer.transform, primaryRenderer);

            Transform parent = primaryRenderer.transform.parent;
            if (parent == null)
            {
                return;
            }

            // Only inspect direct siblings. Do not climb to a Room or Environment root.
            for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                Transform sibling = parent.GetChild(childIndex);
                CollectCompanionAt(sibling, primaryRenderer);
                CollectDirectChildCompanions(sibling, primaryRenderer);
            }
        }

        private void CollectDirectChildCompanions(Transform root, Renderer primaryRenderer)
        {
            for (int childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                CollectCompanionAt(root.GetChild(childIndex), primaryRenderer);
            }
        }

        private void CollectCompanionAt(Transform candidateTransform, Renderer primaryRenderer)
        {
            Renderer candidateRenderer = candidateTransform.GetComponent<Renderer>();
            if (IsCompanionRenderer(primaryRenderer, candidateRenderer))
            {
                _currentOccluders.Add(candidateRenderer);
            }

            DecalProjector candidateDecal = candidateTransform.GetComponent<DecalProjector>();
            if (IsCompanionDecal(primaryRenderer, candidateDecal))
            {
                _currentDecalOccluders.Add(candidateDecal);
            }
        }

        private bool IsCompanionRenderer(Renderer primaryRenderer, Renderer candidateRenderer)
        {
            return candidateRenderer != null &&
                   candidateRenderer != primaryRenderer &&
                   IsFadeableRenderer(candidateRenderer) &&
                   candidateRenderer.enabled &&
                   !IsFloorLikeRenderer(candidateRenderer) &&
                   !IsExcludedGameplayObject(candidateRenderer) &&
                   BoundsAreNear(primaryRenderer.bounds, candidateRenderer.bounds);
        }

        private bool IsCompanionDecal(Renderer primaryRenderer, DecalProjector candidateDecal)
        {
            if (candidateDecal == null ||
                !candidateDecal.enabled ||
                IsExcludedGameplayObject(candidateDecal))
            {
                return false;
            }

            Bounds expandedBounds = primaryRenderer.bounds;
            expandedBounds.Expand(_companionBoundsPadding * 2f);
            return expandedBounds.SqrDistance(candidateDecal.transform.position) <=
                   _companionBoundsPadding * _companionBoundsPadding;
        }

        private bool BoundsAreNear(Bounds primaryBounds, Bounds candidateBounds)
        {
            primaryBounds.Expand(_companionBoundsPadding * 2f);
            return primaryBounds.Intersects(candidateBounds);
        }

        private Transform FindOccluderRoot(Collider hitCollider)
        {
            LODGroup lodGroup = hitCollider.GetComponentInParent<LODGroup>();
            if (lodGroup != null)
            {
                return lodGroup.transform;
            }

            Transform candidate = hitCollider.transform;
            for (int depth = 0; candidate != null && depth < 4; depth++, candidate = candidate.parent)
            {
                int rendererCount = candidate.GetComponentsInChildren<Renderer>(true).Length;
                if (rendererCount > 0 && rendererCount <= _maximumRenderersPerRoot)
                {
                    return candidate;
                }
            }

            return null;
        }

        private Vector3 GetChestTarget(Transform target)
        {
            CharacterController characterController = target.GetComponentInChildren<CharacterController>();
            if (characterController != null)
            {
                float height = characterController.height * Mathf.Abs(characterController.transform.lossyScale.y);
                Vector3 center = characterController.transform.TransformPoint(characterController.center);
                return center + Vector3.up * height * (ChestHeightRatio - 0.5f);
            }

            CapsuleCollider capsule = target.GetComponentInChildren<CapsuleCollider>();
            if (capsule != null)
            {
                float height = capsule.height * Mathf.Abs(capsule.transform.lossyScale.y);
                Vector3 center = capsule.transform.TransformPoint(capsule.center);
                return center + Vector3.up * height * (ChestHeightRatio - 0.5f);
            }

            if (TryGetRendererBounds(target, out Bounds bounds))
            {
                return new Vector3(
                    bounds.center.x,
                    bounds.min.y + bounds.size.y * ChestHeightRatio,
                    bounds.center.z);
            }

            return target.position;
        }

        private float GetDerivedCastRadius()
        {
            float height = GetTargetHeight();
            return Mathf.Clamp(height * 0.1f, 0.1f, 0.25f);
        }

        private float GetTargetHeight()
        {
            CharacterController characterController = _target.GetComponentInChildren<CharacterController>();
            if (characterController != null)
            {
                return characterController.height * Mathf.Abs(characterController.transform.lossyScale.y);
            }

            CapsuleCollider capsule = _target.GetComponentInChildren<CapsuleCollider>();
            if (capsule != null)
            {
                return capsule.height * Mathf.Abs(capsule.transform.lossyScale.y);
            }

            return TryGetRendererBounds(_target, out Bounds bounds) ? bounds.size.y : 1f;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds combinedBounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool foundRenderer = false;
            combinedBounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    combinedBounds = renderer.bounds;
                    foundRenderer = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            return foundRenderer;
        }

        private static bool IsExcludedGameplayObject(Component component)
        {
            Transform transform = component.transform;
            return transform.CompareTag("Player") ||
                   transform.GetComponentInParent<PlayerMovement>() != null ||
                   transform.GetComponentInParent<PlayerInputReader>() != null ||
                   transform.GetComponentInParent<EnemyChaseController>() != null ||
                   transform.GetComponentInParent<EnemyPatrolChaseController>() != null ||
                   transform.GetComponentInParent<EnemyContactAttack>() != null ||
                   transform.GetComponentInParent<ReanimatingCorpseEnemy>() != null ||
                   transform.GetComponentInParent<StationaryHalfBuriedBossController>() != null ||
                   transform.GetComponentInParent<DoorController>() != null ||
                   transform.GetComponentInParent<AutomaticSlidingDoor>() != null ||
                   transform.GetComponentInParent<DoorInteractable>() != null ||
                   transform.GetComponentInParent<SurvivalUnlockDoorInteractable>() != null;
        }

        private static bool IsFadeableRenderer(Renderer renderer)
        {
            return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
        }

        private bool IsFloorLikeRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            Bounds bounds = renderer.bounds;
            float horizontalSpan = Mathf.Max(bounds.size.x, bounds.size.z);
            return horizontalSpan >= _floorLikeMinimumSpan &&
                   bounds.size.y <= horizontalSpan * _floorLikeMaximumThicknessRatio;
        }

        private static bool IsSupportedUrpMaterial(Material material, out int colorProperty)
        {
            colorProperty = -1;
            if (material.shader == null ||
                (material.shader.name != "Universal Render Pipeline/Lit" &&
                 material.shader.name != "Universal Render Pipeline/Unlit"))
            {
                return false;
            }

            if (material.HasProperty(BaseColorProperty))
            {
                colorProperty = BaseColorProperty;
                return true;
            }

            if (material.HasProperty(ColorProperty))
            {
                colorProperty = ColorProperty;
                return true;
            }

            return false;
        }

        private static void ConfigureTransparentUrpMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private void RestoreAllFades()
        {
            foreach (FadeRendererState state in _activeFades.Values)
            {
                state.Restore();
                ReleaseVariants(state);
            }

            _activeFades.Clear();
            _currentOccluders.Clear();

            foreach (FadeDecalState state in _activeDecalFades.Values)
            {
                state.Restore();
            }

            _activeDecalFades.Clear();
            _currentDecalOccluders.Clear();

            foreach (FadeMaterialVariant variant in _materialVariants.Values)
            {
                if (variant.Material != null)
                {
                    Destroy(variant.Material);
                }
            }

            _materialVariants.Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _targetSearchInterval = Mathf.Max(0.05f, _targetSearchInterval);
            _maximumRenderersPerRoot = Mathf.Max(1, _maximumRenderersPerRoot);
            _companionBoundsPadding = Mathf.Clamp(_companionBoundsPadding, 0.05f, 0.15f);
            _floorLikeMinimumSpan = Mathf.Max(0.1f, _floorLikeMinimumSpan);
            _floorLikeMaximumThicknessRatio = Mathf.Clamp(_floorLikeMaximumThicknessRatio, 0.01f, 0.2f);
            _fadeAmount = Mathf.Clamp(_fadeAmount, 0.05f, 1f);
            _fadeDuration = Mathf.Max(0.01f, _fadeDuration);
        }
#endif

        private sealed class FadeMaterialVariant
        {
            public FadeMaterialVariant(Material source, Material material, int colorProperty, Color originalColor)
            {
                Source = source;
                Material = material;
                ColorProperty = colorProperty;
                OriginalColor = originalColor;
            }

            public Material Source { get; }
            public Material Material { get; }
            public int ColorProperty { get; }
            public Color OriginalColor { get; }
            public int ReferenceCount { get; set; }
        }

        private sealed class FadeRendererState
        {
            private readonly Renderer _renderer;
            private readonly Material[] _originalMaterials;
            private readonly MaterialPropertyBlock[] _propertyBlocks;
            private readonly ShadowCastingMode _originalShadowCastingMode;
            private readonly bool _originalForceRenderingOff;
            private readonly bool _usesFallback;
            private float _currentAlpha = FullyVisibleAlpha;
            private float _targetAlpha = FullyVisibleAlpha;

            private FadeRendererState(Renderer renderer, Material[] originalMaterials, FadeMaterialVariant[] variants, bool usesFallback)
            {
                _renderer = renderer;
                _originalMaterials = originalMaterials;
                Variants = variants;
                _usesFallback = usesFallback;
                _originalShadowCastingMode = renderer.shadowCastingMode;
                _originalForceRenderingOff = renderer.forceRenderingOff;
                _propertyBlocks = variants == null ? null : new MaterialPropertyBlock[variants.Length];
            }

            public FadeMaterialVariant[] Variants { get; }

            public static FadeRendererState CreateMaterialFade(Renderer renderer, Material[] originals, FadeMaterialVariant[] variants)
            {
                renderer.sharedMaterials = BuildFadeMaterials(variants);
                return new FadeRendererState(renderer, originals, variants, false);
            }

            public static FadeRendererState CreateFallback(Renderer renderer)
            {
                return new FadeRendererState(renderer, null, null, true);
            }

            public void SetTarget(float alpha)
            {
                _targetAlpha = alpha;
            }

            public bool Tick(float fadeDuration)
            {
                _currentAlpha = Mathf.MoveTowards(
                    _currentAlpha,
                    _targetAlpha,
                    Time.deltaTime / fadeDuration);

                if (_usesFallback)
                {
                    _renderer.forceRenderingOff = _originalForceRenderingOff || _currentAlpha < FullyVisibleAlpha - RestoreEpsilon;
                }
                else
                {
                    ApplyMaterialPropertyBlocks();
                    _renderer.shadowCastingMode = _currentAlpha < 0.9f
                        ? ShadowCastingMode.Off
                        : _originalShadowCastingMode;
                }

                return _targetAlpha >= FullyVisibleAlpha && _currentAlpha >= FullyVisibleAlpha - RestoreEpsilon;
            }

            public void Restore()
            {
                if (_renderer == null)
                {
                    return;
                }

                if (_usesFallback)
                {
                    _renderer.forceRenderingOff = _originalForceRenderingOff;
                    return;
                }

                _renderer.sharedMaterials = _originalMaterials;
                _renderer.shadowCastingMode = _originalShadowCastingMode;
                for (int materialIndex = 0; materialIndex < Variants.Length; materialIndex++)
                {
                    _renderer.SetPropertyBlock(null, materialIndex);
                }
            }

            private void ApplyMaterialPropertyBlocks()
            {
                for (int materialIndex = 0; materialIndex < Variants.Length; materialIndex++)
                {
                    FadeMaterialVariant variant = Variants[materialIndex];
                    MaterialPropertyBlock propertyBlock = _propertyBlocks[materialIndex] ??
                                                          (_propertyBlocks[materialIndex] = new MaterialPropertyBlock());
                    propertyBlock.Clear();
                    _renderer.GetPropertyBlock(propertyBlock, materialIndex);
                    Color color = variant.OriginalColor;
                    color.a *= _currentAlpha;
                    propertyBlock.SetColor(variant.ColorProperty, color);
                    _renderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }

            private static Material[] BuildFadeMaterials(FadeMaterialVariant[] variants)
            {
                var materials = new Material[variants.Length];
                for (int materialIndex = 0; materialIndex < variants.Length; materialIndex++)
                {
                    materials[materialIndex] = variants[materialIndex].Material;
                }

                return materials;
            }
        }

        private sealed class FadeDecalState
        {
            private readonly DecalProjector _decal;
            private readonly float _originalFadeFactor;
            private float _currentAlpha = FullyVisibleAlpha;
            private float _targetAlpha = FullyVisibleAlpha;

            private FadeDecalState(DecalProjector decal)
            {
                _decal = decal;
                _originalFadeFactor = decal.fadeFactor;
            }

            public static FadeDecalState Create(DecalProjector decal)
            {
                return decal != null && decal.enabled ? new FadeDecalState(decal) : null;
            }

            public void SetTarget(float alpha)
            {
                _targetAlpha = alpha;
            }

            public bool Tick(float fadeDuration)
            {
                if (_decal == null)
                {
                    return true;
                }

                _currentAlpha = Mathf.MoveTowards(
                    _currentAlpha,
                    _targetAlpha,
                    Time.deltaTime / fadeDuration);
                _decal.fadeFactor = _originalFadeFactor * _currentAlpha;
                return _targetAlpha >= FullyVisibleAlpha &&
                       _currentAlpha >= FullyVisibleAlpha - RestoreEpsilon;
            }

            public void Restore()
            {
                if (_decal != null)
                {
                    _decal.fadeFactor = _originalFadeFactor;
                }
            }
        }
    }
}
