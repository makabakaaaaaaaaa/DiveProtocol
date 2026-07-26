using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.Rendering.SignalLikePixel
{
#pragma warning disable CS0672
    /// <summary>
    /// Performs full-res camera color downsample, low-res outline, low-res composite, and sharp upscale.
    /// </summary>
    public sealed class SignalLikePixelRenderPass : ScriptableRenderPass
    {
        private static readonly int LowTexelSizeId = Shader.PropertyToID("_SignalLikePixelLowTexelSize");
        private static readonly int OutlineEnabledId = Shader.PropertyToID("_SignalLikePixelOutlineEnabled");
        private static readonly int OutlineThicknessId = Shader.PropertyToID("_SignalLikePixelOutlineThickness");
        private static readonly int OutlineDepthThresholdId = Shader.PropertyToID("_SignalLikePixelOutlineDepthThreshold");
        private static readonly int OutlineNormalThresholdId = Shader.PropertyToID("_SignalLikePixelOutlineNormalThreshold");
        private static readonly int OutlineStrengthId = Shader.PropertyToID("_SignalLikePixelOutlineStrength");
        private static readonly int OutlineColorId = Shader.PropertyToID("_SignalLikePixelOutlineColor");
        private static readonly int OutlineTexId = Shader.PropertyToID("_SignalLikePixelOutlineTex");
        private static readonly int UpscaleSharpnessId = Shader.PropertyToID("_SignalLikePixelUpscaleSharpness");

        private readonly ProfilingSampler _profilingSampler = new("SignalLikePixel");
        private readonly Material _downsampleMaterial;
        private readonly Material _outlineMaterial;
        private readonly Material _compositeMaterial;
        private readonly Material _upscaleMaterial;

        private RTHandle _source;
        private RTHandle _lowColor;
        private RTHandle _outline;
        private RTHandle _composite;
        private RTHandle _upscaleTemp;
        private SignalLikePixelSettings _settings;

        public SignalLikePixelRenderPass(
            Material downsampleMaterial,
            Material outlineMaterial,
            Material compositeMaterial,
            Material upscaleMaterial)
        {
            _downsampleMaterial = downsampleMaterial;
            _outlineMaterial = outlineMaterial;
            _compositeMaterial = compositeMaterial;
            _upscaleMaterial = upscaleMaterial;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public void Setup(RTHandle source, SignalLikePixelSettings settings)
        {
            _source = source;
            _settings = settings;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_settings == null)
            {
                return;
            }

            RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraDescriptor.msaaSamples = 1;
            cameraDescriptor.depthBufferBits = 0;
            cameraDescriptor.useMipMap = false;
            cameraDescriptor.autoGenerateMips = false;

            if (!_settings.UseHdrWhenAvailable)
            {
                cameraDescriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            }

            RenderTextureDescriptor lowDescriptor = cameraDescriptor;
            lowDescriptor.width = _settings.InternalWidth;
            lowDescriptor.height = _settings.InternalHeight;

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref _lowColor,
                lowDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_SignalLikePixel_LowColor");

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref _outline,
                lowDescriptor,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                name: "_SignalLikePixel_Outline");

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref _composite,
                lowDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_SignalLikePixel_Composite");

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref _upscaleTemp,
                cameraDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_SignalLikePixel_UpscaleTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_source == null ||
                _settings == null ||
                _downsampleMaterial == null ||
                _outlineMaterial == null ||
                _compositeMaterial == null ||
                _upscaleMaterial == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                Vector4 lowTexelSize = new(
                    1f / _settings.InternalWidth,
                    1f / _settings.InternalHeight,
                    _settings.InternalWidth,
                    _settings.InternalHeight);

                _outlineMaterial.SetVector(LowTexelSizeId, lowTexelSize);
                _outlineMaterial.SetFloat(OutlineEnabledId, _settings.OutlineEnabled ? 1f : 0f);
                _outlineMaterial.SetFloat(OutlineThicknessId, _settings.OutlineThickness);
                _outlineMaterial.SetFloat(OutlineDepthThresholdId, _settings.OutlineDepthThreshold);
                _outlineMaterial.SetFloat(OutlineNormalThresholdId, _settings.OutlineNormalThreshold);
                _outlineMaterial.SetFloat(OutlineStrengthId, _settings.OutlineStrength);
                _outlineMaterial.SetColor(OutlineColorId, _settings.OutlineColor);

                _compositeMaterial.SetTexture(OutlineTexId, _outline);
                _upscaleMaterial.SetVector(LowTexelSizeId, lowTexelSize);
                _upscaleMaterial.SetFloat(UpscaleSharpnessId, _settings.UpscaleSharpness);

                Blitter.BlitCameraTexture(cmd, _source, _lowColor, _downsampleMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _lowColor, _outline, _outlineMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _lowColor, _composite, _compositeMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _composite, _upscaleTemp, _upscaleMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _upscaleTemp, _source);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _lowColor?.Release();
            _outline?.Release();
            _composite?.Release();
            _upscaleTemp?.Release();
        }
    }
#pragma warning restore CS0672
}
