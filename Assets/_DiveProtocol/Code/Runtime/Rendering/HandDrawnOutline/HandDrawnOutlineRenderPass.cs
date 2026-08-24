using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.Rendering.HandDrawnOutline
{
#pragma warning disable CS0672
    /// <summary>
    /// Copies camera color to a temporary RTHandle, then composites depth/normal hand-drawn outlines back to camera color.
    /// </summary>
    public sealed class HandDrawnOutlineRenderPass : ScriptableRenderPass
    {
        private static readonly int OutlineColorId = Shader.PropertyToID("_HDOutlineColor");
        private static readonly int OutlineOpacityId = Shader.PropertyToID("_HDOutlineOpacity");
        private static readonly int DepthThresholdId = Shader.PropertyToID("_HDDepthThreshold");
        private static readonly int NormalThresholdId = Shader.PropertyToID("_HDNormalThreshold");
        private static readonly int DepthStrengthId = Shader.PropertyToID("_HDDepthStrength");
        private static readonly int NormalStrengthId = Shader.PropertyToID("_HDNormalStrength");
        private static readonly int NearThicknessId = Shader.PropertyToID("_HDNearThickness");
        private static readonly int FarThicknessId = Shader.PropertyToID("_HDFarThickness");
        private static readonly int NearDistanceId = Shader.PropertyToID("_HDNearDistance");
        private static readonly int FarDistanceId = Shader.PropertyToID("_HDFarDistance");
        private static readonly int NoiseEnabledId = Shader.PropertyToID("_HDNoiseEnabled");
        private static readonly int NoiseTextureId = Shader.PropertyToID("_HDNoiseTexture");
        private static readonly int HasNoiseTextureId = Shader.PropertyToID("_HDHasNoiseTexture");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_HDNoiseScale");
        private static readonly int BreakThresholdId = Shader.PropertyToID("_HDBreakThreshold");
        private static readonly int BreakSoftnessId = Shader.PropertyToID("_HDBreakSoftness");
        private static readonly int DepthNoiseInfluenceId = Shader.PropertyToID("_HDDepthNoiseInfluence");
        private static readonly int NormalNoiseInfluenceId = Shader.PropertyToID("_HDNormalNoiseInfluence");
        private static readonly int DarkAreaSuppressionId = Shader.PropertyToID("_HDDarkAreaSuppression");
        private static readonly int DarkAreaStartId = Shader.PropertyToID("_HDDarkAreaStart");
        private static readonly int DarkAreaEndId = Shader.PropertyToID("_HDDarkAreaEnd");
        private static readonly int DebugModeId = Shader.PropertyToID("_HDDebugMode");

        private readonly ProfilingSampler _profilingSampler = new("HandDrawnOutline");
        private readonly Material _outlineMaterial;

        private RTHandle _source;
        private RTHandle _temporaryColor;
        private HandDrawnOutlineSettings _settings;
        private bool _loggedExecute;
        private bool _loggedInvalidResources;

        public HandDrawnOutlineRenderPass(Material outlineMaterial)
        {
            _outlineMaterial = outlineMaterial;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public void SetSource(RTHandle sourceHandle)
        {
            _source = sourceHandle;
        }

        public void SetSettings(HandDrawnOutlineSettings settings)
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

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref _temporaryColor,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_HandDrawnOutline_TemporaryColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_source == null ||
                _temporaryColor == null ||
                _outlineMaterial == null ||
                _settings == null)
            {
                if (_settings != null && _settings.LogDebug && !_loggedInvalidResources)
                {
                    Debug.LogWarning($"[HandDrawnOutline] Execute skipped. Source valid={_source != null}, Temporary valid={_temporaryColor != null}, Material valid={_outlineMaterial != null}, Settings valid={_settings != null}.");
                    _loggedInvalidResources = true;
                }

                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                ApplyMaterialProperties();
                Blitter.BlitCameraTexture(cmd, _source, _temporaryColor);
                Blitter.BlitCameraTexture(cmd, _temporaryColor, _source, _outlineMaterial, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (_settings.LogDebug && !_loggedExecute)
            {
                Camera camera = renderingData.cameraData.camera;
                Debug.Log($"[HandDrawnOutline] Execute ran for camera '{camera.name}'. Source='{_source.name}', Temporary='{_temporaryColor.name}'.");
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
            _outlineMaterial.SetColor(OutlineColorId, _settings.OutlineColor);
            _outlineMaterial.SetFloat(OutlineOpacityId, _settings.OutlineOpacity);
            _outlineMaterial.SetFloat(DepthThresholdId, _settings.DepthThreshold);
            _outlineMaterial.SetFloat(NormalThresholdId, _settings.NormalThreshold);
            _outlineMaterial.SetFloat(DepthStrengthId, _settings.DepthEdgeStrength);
            _outlineMaterial.SetFloat(NormalStrengthId, _settings.NormalEdgeStrength);
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
            _outlineMaterial.SetFloat(DebugModeId, (float)_settings.DebugMode);

            if (_settings.NoiseTexture != null)
            {
                _outlineMaterial.SetTexture(NoiseTextureId, _settings.NoiseTexture);
            }
        }
    }
#pragma warning restore CS0672
}
