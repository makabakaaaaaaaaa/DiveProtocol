using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.Rendering.YoOutlineV2
{
#pragma warning disable CS0618
    /// <summary>
    /// Copies camera color into a temporary RTHandle, applies Yo-style outline compositing, then writes back to camera color.
    /// </summary>
    public sealed class YoOutlineRenderPassV2 : ScriptableRenderPass
    {
        private static readonly int DebugModeId = Shader.PropertyToID("_YoDebugMode");
        private static readonly int OutlineColorId = Shader.PropertyToID("_YoOutlineColor");
        private static readonly int OutlineOpacityId = Shader.PropertyToID("_YoOutlineOpacity");
        private static readonly int DepthThresholdId = Shader.PropertyToID("_YoDepthThreshold");
        private static readonly int NormalThresholdId = Shader.PropertyToID("_YoNormalThreshold");
        private static readonly int DepthStrengthId = Shader.PropertyToID("_YoDepthStrength");
        private static readonly int NormalStrengthId = Shader.PropertyToID("_YoNormalStrength");
        private static readonly int NearThicknessId = Shader.PropertyToID("_YoNearThickness");
        private static readonly int FarThicknessId = Shader.PropertyToID("_YoFarThickness");
        private static readonly int NearDistanceId = Shader.PropertyToID("_YoNearDistance");
        private static readonly int FarDistanceId = Shader.PropertyToID("_YoFarDistance");
        private static readonly int NoiseEnabledId = Shader.PropertyToID("_YoNoiseEnabled");
        private static readonly int NoiseTextureId = Shader.PropertyToID("_YoNoiseTexture");
        private static readonly int HasNoiseTextureId = Shader.PropertyToID("_YoHasNoiseTexture");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_YoNoiseScale");
        private static readonly int BreakThresholdId = Shader.PropertyToID("_YoBreakThreshold");
        private static readonly int BreakSoftnessId = Shader.PropertyToID("_YoBreakSoftness");
        private static readonly int DepthNoiseInfluenceId = Shader.PropertyToID("_YoDepthNoiseInfluence");
        private static readonly int NormalNoiseInfluenceId = Shader.PropertyToID("_YoNormalNoiseInfluence");
        private static readonly int DarkAreaSuppressionId = Shader.PropertyToID("_YoDarkAreaSuppression");
        private static readonly int DarkAreaStartId = Shader.PropertyToID("_YoDarkAreaStart");
        private static readonly int DarkAreaEndId = Shader.PropertyToID("_YoDarkAreaEnd");

        private readonly ProfilingSampler _profilingSampler = new("YoOutlineV2");
        private readonly Material _outlineMaterial;

        private RTHandle _source;
        private RTHandle _temporaryColor;
        private YoOutlineRendererFeatureV2.Settings _settings;
        private bool _loggedExecute;
        private bool _loggedInvalidResources;

        public YoOutlineRenderPassV2(Material outlineMaterial)
        {
            _outlineMaterial = outlineMaterial;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public void SetSource(RTHandle sourceHandle)
        {
            _source = sourceHandle;
        }

        public void SetSettings(YoOutlineRendererFeatureV2.Settings settings)
        {
            _settings = settings;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;

            if (descriptor.graphicsFormat == GraphicsFormat.None)
            {
                descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            }

            RenderingUtils.ReAllocateIfNeeded(
                ref _temporaryColor,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_YoOutlineV2_TemporaryColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_source == null ||
                _temporaryColor == null ||
                _outlineMaterial == null ||
                _settings == null)
            {
                if (_settings != null && _settings.EnableDebugLogs && !_loggedInvalidResources)
                {
                    Debug.LogWarning($"[YoOutlineV2] Execute skipped: source={_source != null}, temporary={_temporaryColor != null}, material={_outlineMaterial != null}, settings={_settings != null}.");
                    _loggedInvalidResources = true;
                }

                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("YoOutlineV2");

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                ApplyMaterialProperties();
                Blitter.BlitCameraTexture(cmd, _source, _temporaryColor);
                Blitter.BlitCameraTexture(cmd, _temporaryColor, _source, _outlineMaterial, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (_settings.EnableDebugLogs && !_loggedExecute)
            {
                Camera camera = renderingData.cameraData.camera;
                Debug.Log($"[YoOutlineV2] Execute: {camera.name}, source='{_source.name}', temporary='{_temporaryColor.name}'.");
                _loggedExecute = true;
            }
        }

        public void Dispose()
        {
            _temporaryColor?.Release();
            _temporaryColor = null;
        }

        private void ApplyMaterialProperties()
        {
            _outlineMaterial.SetFloat(DebugModeId, (float)_settings.CurrentDebugMode);
            _outlineMaterial.SetColor(OutlineColorId, _settings.OutlineColor);
            _outlineMaterial.SetFloat(OutlineOpacityId, _settings.OutlineOpacity);
            _outlineMaterial.SetFloat(DepthThresholdId, _settings.DepthThreshold);
            _outlineMaterial.SetFloat(NormalThresholdId, _settings.NormalThreshold);
            _outlineMaterial.SetFloat(DepthStrengthId, _settings.DepthStrength);
            _outlineMaterial.SetFloat(NormalStrengthId, _settings.NormalStrength);
            _outlineMaterial.SetFloat(NearThicknessId, _settings.NearThickness);
            _outlineMaterial.SetFloat(FarThicknessId, _settings.FarThickness);
            _outlineMaterial.SetFloat(NearDistanceId, _settings.NearDistance);
            _outlineMaterial.SetFloat(FarDistanceId, _settings.FarDistance);
            _outlineMaterial.SetFloat(NoiseEnabledId, _settings.NoiseEnabled ? 1f : 0f);
            _outlineMaterial.SetFloat(HasNoiseTextureId, _settings.NoiseTexture != null ? 1f : 0f);
            _outlineMaterial.SetFloat(NoiseScaleId, _settings.NoiseScale);
            _outlineMaterial.SetFloat(BreakThresholdId, _settings.BreakThreshold);
            _outlineMaterial.SetFloat(BreakSoftnessId, _settings.BreakSoftness);
            _outlineMaterial.SetFloat(DepthNoiseInfluenceId, _settings.DepthNoiseInfluence);
            _outlineMaterial.SetFloat(NormalNoiseInfluenceId, _settings.NormalNoiseInfluence);
            _outlineMaterial.SetFloat(DarkAreaSuppressionId, _settings.DarkAreaSuppression);
            _outlineMaterial.SetFloat(DarkAreaStartId, _settings.DarkAreaStart);
            _outlineMaterial.SetFloat(DarkAreaEndId, _settings.DarkAreaEnd);

            if (_settings.NoiseTexture != null)
            {
                _outlineMaterial.SetTexture(NoiseTextureId, _settings.NoiseTexture);
            }
        }
    }
#pragma warning restore CS0618
}
