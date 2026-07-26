using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.Rendering.SignalLikePixel
{
#pragma warning disable CS0618
    /// <summary>
    /// URP Renderer Feature that applies a SIGNALIS-like low-resolution scene pass.
    /// UI rendered by Screen Space - Overlay canvases remains full-resolution.
    /// </summary>
    public sealed class SignalLikePixelRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private SignalLikePixelSettings _settings = new();

        private SignalLikePixelRenderPass _pass;
        private Material _downsampleMaterial;
        private Material _outlineMaterial;
        private Material _compositeMaterial;
        private Material _upscaleMaterial;

        public override void Create()
        {
            _downsampleMaterial = CoreUtils.CreateEngineMaterial("Hidden/DiveProtocol/SignalLikePixel/Downsample");
            _outlineMaterial = CoreUtils.CreateEngineMaterial("Hidden/DiveProtocol/SignalLikePixel/Outline");
            _compositeMaterial = CoreUtils.CreateEngineMaterial("Hidden/DiveProtocol/SignalLikePixel/Composite");
            _upscaleMaterial = CoreUtils.CreateEngineMaterial("Hidden/DiveProtocol/SignalLikePixel/SharpUpscale");

            _pass = new SignalLikePixelRenderPass(
                _downsampleMaterial,
                _outlineMaterial,
                _compositeMaterial,
                _upscaleMaterial)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_settings == null || !_settings.Enabled || _pass == null)
            {
                return;
            }

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && !(cameraType == CameraType.SceneView && _settings.ApplyToSceneView))
            {
                return;
            }

            if (_downsampleMaterial == null ||
                _outlineMaterial == null ||
                _compositeMaterial == null ||
                _upscaleMaterial == null)
            {
                return;
            }

            renderer.EnqueuePass(_pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (_settings == null || !_settings.Enabled || _pass == null)
            {
                return;
            }

            _pass.Setup(renderer.cameraColorTargetHandle, _settings);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            CoreUtils.Destroy(_downsampleMaterial);
            CoreUtils.Destroy(_outlineMaterial);
            CoreUtils.Destroy(_compositeMaterial);
            CoreUtils.Destroy(_upscaleMaterial);
        }
    }
#pragma warning restore CS0618
}
