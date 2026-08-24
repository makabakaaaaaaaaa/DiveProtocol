using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.Rendering.YoOutlineV2
{
#pragma warning disable CS0618
    /// <summary>
    /// Unity 6 URP Compatibility Mode renderer feature for screen-space depth/normal outline compositing.
    /// </summary>
    public sealed class YoOutlineRendererFeatureV2 : ScriptableRendererFeature
    {
        [System.Serializable]
        public sealed class Settings
        {
            public enum DebugMode
            {
                ExecutionTest = 0,
                DepthEdgeOnly = 1,
                NormalEdgeOnly = 2,
                Combined = 3,
                NoiseMask = 4,
                FinalMask = 5,
            }

            [Header("Execution")]
            [SerializeField] private bool _enabled = true;
            [SerializeField] private bool _applyToSceneView;
            [SerializeField] private bool _enableDebugLogs;
            [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            [SerializeField] private DebugMode _debugMode = DebugMode.Combined;

            [Header("Color")]
            [SerializeField] private Color _outlineColor = new(0.09f, 0.078f, 0.059f, 1f);
            [SerializeField, Range(0f, 1f)] private float _outlineOpacity = 0.7f;

            [Header("Edges")]
            [SerializeField, Min(0.00001f)] private float _depthThreshold = 0.008f;
            [SerializeField, Min(0.00001f)] private float _normalThreshold = 0.18f;
            [SerializeField, Min(0f)] private float _depthStrength = 1f;
            [SerializeField, Min(0f)] private float _normalStrength = 0.65f;

            [Header("Distance Thickness")]
            [SerializeField, Min(0.1f)] private float _nearThickness = 1.8f;
            [SerializeField, Min(0.1f)] private float _farThickness = 0.8f;
            [SerializeField, Min(0.01f)] private float _nearDistance = 2.5f;
            [SerializeField, Min(0.01f)] private float _farDistance = 18f;

            [Header("Broken Line Noise")]
            [SerializeField] private bool _noiseEnabled;
            [SerializeField] private Texture2D _noiseTexture;
            [SerializeField, Min(0.001f)] private float _noiseScale = 1.8f;
            [SerializeField, Range(0f, 1f)] private float _breakThreshold = 0.42f;
            [SerializeField, Range(0.001f, 1f)] private float _breakSoftness = 0.08f;
            [SerializeField, Range(0f, 1f)] private float _depthNoiseInfluence = 0.1f;
            [SerializeField, Range(0f, 1f)] private float _normalNoiseInfluence = 0.55f;

            [Header("Dark Area Control")]
            [SerializeField, Range(0f, 1f)] private float _darkAreaSuppression = 0.6f;
            [SerializeField, Range(0f, 1f)] private float _darkAreaStart = 0.02f;
            [SerializeField, Range(0f, 1f)] private float _darkAreaEnd = 0.16f;

            public bool Enabled => _enabled;
            public bool ApplyToSceneView => _applyToSceneView;
            public bool EnableDebugLogs => _enableDebugLogs;
            public RenderPassEvent RenderPassEvent => _renderPassEvent;
            public DebugMode CurrentDebugMode => _debugMode;
            public Color OutlineColor => _outlineColor;
            public float OutlineOpacity => _outlineOpacity;
            public float DepthThreshold => _depthThreshold;
            public float NormalThreshold => _normalThreshold;
            public float DepthStrength => _depthStrength;
            public float NormalStrength => _normalStrength;
            public float NearThickness => _nearThickness;
            public float FarThickness => _farThickness;
            public float NearDistance => _nearDistance;
            public float FarDistance => _farDistance;
            public bool NoiseEnabled => _noiseEnabled;
            public Texture2D NoiseTexture => _noiseTexture;
            public float NoiseScale => _noiseScale;
            public float BreakThreshold => _breakThreshold;
            public float BreakSoftness => _breakSoftness;
            public float DepthNoiseInfluence => _depthNoiseInfluence;
            public float NormalNoiseInfluence => _normalNoiseInfluence;
            public float DarkAreaSuppression => _darkAreaSuppression;
            public float DarkAreaStart => _darkAreaStart;
            public float DarkAreaEnd => _darkAreaEnd;
        }

        [SerializeField] private Settings _settings = new();

        [Tooltip("Assign Assets/_DiveProtocol/Rendering/Shaders/YoOutlineV2/YoOutlinePostProcessV2.shader here.")]
        [SerializeField] private Shader _outlineShader;

        private Material _outlineMaterial;
        private YoOutlineRenderPassV2 _outlinePass;
        private bool _loggedCreate;
        private bool _loggedMissingShader;
        private bool _loggedSetup;
        private bool _loggedEnqueue;

        public override void Create()
        {
            CoreUtils.Destroy(_outlineMaterial);

            Shader shader = _outlineShader != null
                ? _outlineShader
                : Shader.Find("Hidden/DiveProtocol/YoOutlineV2/PostProcess");

            _outlineMaterial = shader != null
                ? CoreUtils.CreateEngineMaterial(shader)
                : null;

            _outlinePass = new YoOutlineRenderPassV2(_outlineMaterial)
            {
                renderPassEvent = _settings != null
                    ? _settings.RenderPassEvent
                    : RenderPassEvent.BeforeRenderingPostProcessing
            };

            if (_outlineMaterial == null)
            {
                if (!_loggedMissingShader)
                {
                    Debug.LogError("[YoOutlineV2] Create failed: missing outline shader. Assign YoOutlinePostProcessV2.shader on the Renderer Feature.");
                    _loggedMissingShader = true;
                }
            }
            else if (!_loggedCreate)
            {
                Debug.Log($"[YoOutlineV2] Create: shader='{shader.name}', passEvent='{_outlinePass.renderPassEvent}'.");
                _loggedCreate = true;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData))
            {
                return;
            }

            _outlinePass.SetSettings(_settings);
            renderer.EnqueuePass(_outlinePass);

            if (_settings.EnableDebugLogs && !_loggedEnqueue)
            {
                Camera camera = renderingData.cameraData.camera;
                Debug.Log($"[YoOutlineV2] Enqueued: {camera.name} ({renderingData.cameraData.cameraType}, {renderingData.cameraData.renderType}).");
                _loggedEnqueue = true;
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData))
            {
                return;
            }

            _outlinePass.renderPassEvent = _settings.RenderPassEvent;
            _outlinePass.SetSettings(_settings);
            _outlinePass.SetSource(renderer.cameraColorTargetHandle);

            if (_settings.EnableDebugLogs && !_loggedSetup)
            {
                Camera camera = renderingData.cameraData.camera;
                Debug.Log($"[YoOutlineV2] SetupRenderPasses: {camera.name}, source='{renderer.cameraColorTargetHandle.name}'.");
                _loggedSetup = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _outlinePass?.Dispose();
            CoreUtils.Destroy(_outlineMaterial);
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            if (_settings == null ||
                !_settings.Enabled ||
                _outlinePass == null ||
                _outlineMaterial == null)
            {
                return false;
            }

            CameraData cameraData = renderingData.cameraData;

            if (cameraData.renderType != CameraRenderType.Base)
            {
                return false;
            }

            if (cameraData.isPreviewCamera ||
                cameraData.cameraType == CameraType.Reflection)
            {
                return false;
            }

            if (cameraData.isSceneViewCamera)
            {
                return _settings.ApplyToSceneView;
            }

            return true;
        }
    }
#pragma warning restore CS0618
}
