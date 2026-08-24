using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.Rendering.HandDrawnOutline
{
#pragma warning disable CS0618
    /// <summary>
    /// URP Compatibility Mode renderer feature for screen-space hand-drawn broken outlines.
    /// </summary>
    public sealed class HandDrawnOutlineRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private HandDrawnOutlineSettings _settings = new();

        [Tooltip("Assign Assets/_DiveProtocol/Rendering/Shaders/HandDrawnOutline/HandDrawnScreenOutline.shader here to avoid build stripping.")]
        [SerializeField] private Shader _outlineShader;

        private Material _outlineMaterial;
        private HandDrawnOutlineRenderPass _outlinePass;
        private bool _loggedCreate;
        private bool _loggedMissingShader;
        private bool _loggedSetup;
        private bool _loggedEnqueue;

        public override void Create()
        {
            CoreUtils.Destroy(_outlineMaterial);

            Shader shader = _outlineShader != null
                ? _outlineShader
                : Shader.Find("Hidden/DiveProtocol/HandDrawnOutline/ScreenOutline");

            _outlineMaterial = shader != null
                ? CoreUtils.CreateEngineMaterial(shader)
                : null;

            _outlinePass = new HandDrawnOutlineRenderPass(_outlineMaterial);

            if (_settings != null)
            {
                _outlinePass.renderPassEvent = _settings.RenderPassEvent;
            }

            if (_outlineMaterial == null)
            {
                if (!_loggedMissingShader)
                {
                    Debug.LogError("[HandDrawnOutline] Create failed: outline shader is missing. Assign HandDrawnScreenOutline.shader on the Renderer Feature.");
                    _loggedMissingShader = true;
                }
            }
            else if (!_loggedCreate)
            {
                Debug.Log($"[HandDrawnOutline] Create succeeded. Shader='{shader.name}', PassEvent='{_outlinePass.renderPassEvent}'.");
                _loggedCreate = true;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CanRender(in renderingData))
            {
                return;
            }

            _outlinePass.SetSettings(_settings);
            renderer.EnqueuePass(_outlinePass);

            if (_settings.LogDebug && !_loggedEnqueue)
            {
                Camera camera = renderingData.cameraData.camera;
                Debug.Log($"[HandDrawnOutline] AddRenderPasses enqueued for camera '{camera.name}' ({renderingData.cameraData.cameraType}, {renderingData.cameraData.renderType}).");
                _loggedEnqueue = true;
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (!CanRender(in renderingData))
            {
                return;
            }

            _outlinePass.renderPassEvent = _settings.RenderPassEvent;
            _outlinePass.SetSettings(_settings);
            _outlinePass.SetSource(renderer.cameraColorTargetHandle);

            if (_settings.LogDebug && !_loggedSetup)
            {
                Camera camera = renderingData.cameraData.camera;
                Debug.Log($"[HandDrawnOutline] SetupRenderPasses received camera '{camera.name}' and cameraColorTargetHandle '{renderer.cameraColorTargetHandle.name}'.");
                _loggedSetup = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _outlinePass?.Dispose();
            CoreUtils.Destroy(_outlineMaterial);
        }

        private bool CanRender(in RenderingData renderingData)
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

            // Keep the filter intentionally broad for gameplay cameras. Do not require
            // post processing, a specific camera name/tag, or a non-null targetTexture.
            return true;
        }
    }
#pragma warning restore CS0618
}
