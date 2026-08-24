using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DiveProtocol.Rendering.HandDrawnOutline
{
    /// <summary>
    /// Tunable settings for the screen-space hand-drawn outline pass.
    /// </summary>
    [Serializable]
    public sealed class HandDrawnOutlineSettings
    {
        public enum OutlineDebugMode
        {
            Combined = 0,
            DepthEdgeOnly = 1,
            NormalEdgeOnly = 2,
            NoiseMaskOnly = 3,
            FinalOutlineMask = 4,
            ExecutionTest = 5,
        }

        [Header("Execution")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _applyToSceneView;
        [SerializeField] private bool _logDebug;
        [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [SerializeField] private OutlineDebugMode _debugMode = OutlineDebugMode.Combined;

        [Header("Color")]
        [SerializeField] private Color _outlineColor = new(0.09f, 0.078f, 0.059f, 1f);
        [SerializeField, Range(0f, 1f)] private float _outlineOpacity = 0.7f;

        [Header("Edges")]
        [SerializeField, Min(0.00001f)] private float _depthThreshold = 0.008f;
        [SerializeField, Min(0.00001f)] private float _normalThreshold = 0.18f;
        [SerializeField, Min(0f)] private float _depthEdgeStrength = 1f;
        [SerializeField, Min(0f)] private float _normalEdgeStrength = 0.65f;

        [Header("Distance Thickness")]
        [SerializeField, Min(0.1f)] private float _nearThickness = 1.8f;
        [SerializeField, Min(0.1f)] private float _farThickness = 0.8f;
        [SerializeField, Min(0.01f)] private float _nearDistance = 2.5f;
        [SerializeField, Min(0.01f)] private float _farDistance = 18f;

        [Header("Broken Line Noise")]
        [SerializeField] private bool _noiseEnabled = true;
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
        public bool LogDebug => _logDebug;
        public RenderPassEvent RenderPassEvent => _renderPassEvent;
        public OutlineDebugMode DebugMode => _debugMode;
        public Color OutlineColor => _outlineColor;
        public float OutlineOpacity => _outlineOpacity;
        public float DepthThreshold => _depthThreshold;
        public float NormalThreshold => _normalThreshold;
        public float DepthEdgeStrength => _depthEdgeStrength;
        public float NormalEdgeStrength => _normalEdgeStrength;
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
}
