using System;
using UnityEngine;

namespace DiveProtocol.Rendering.SignalLikePixel
{
    /// <summary>
    /// Serializable settings for the SIGNALIS-like low-resolution render pipeline.
    /// Intended to be owned by SignalLikePixelRendererFeature in a URP Renderer Data asset.
    /// </summary>
    [Serializable]
    public sealed class SignalLikePixelSettings
    {
        [Tooltip("Enables the low-resolution scene render, outline, composite, and sharp upscale pass.")]
        [SerializeField] private bool _enabled = true;

        [Tooltip("Low-resolution buffer width. 640 is the recommended 1080p starting point.")]
        [SerializeField, Min(160)] private int _internalWidth = 640;

        [Tooltip("Low-resolution buffer height. 360 is the recommended 1080p starting point.")]
        [SerializeField, Min(90)] private int _internalHeight = 360;

        [Tooltip("Applies the feature to Scene View cameras for preview. Game cameras are always supported.")]
        [SerializeField] private bool _applyToSceneView = false;

        [Header("Outline")]
        [Tooltip("Enables a low-resolution depth/normal outline.")]
        [SerializeField] private bool _outlineEnabled = true;

        [Tooltip("Outline radius in low-resolution pixels.")]
        [SerializeField, Range(0.5f, 3f)] private float _outlineThickness = 1f;

        [Tooltip("Depth discontinuity required before an outline appears.")]
        [SerializeField, Range(0.0001f, 0.05f)] private float _outlineDepthThreshold = 0.005f;

        [Tooltip("Normal discontinuity required before an outline appears.")]
        [SerializeField, Range(0.01f, 1f)] private float _outlineNormalThreshold = 0.18f;

        [Tooltip("How strongly the outline is composited over the low-resolution scene.")]
        [SerializeField, Range(0f, 1f)] private float _outlineStrength = 0.65f;

        [Tooltip("Controlled dark outline color. Avoid pure black for a less cartoony look.")]
        [SerializeField] private Color _outlineColor = new(0.086f, 0.078f, 0.071f, 1f);

        [Header("Upscale")]
        [Tooltip("Sharp upscale amount. 0 is soft bilinear; 1 is crisper but can become harsher.")]
        [SerializeField, Range(0f, 1f)] private float _upscaleSharpness = 0.7f;

        [Tooltip("Uses HDR-compatible temporary color buffers when the camera is HDR.")]
        [SerializeField] private bool _useHdrWhenAvailable = true;

        public bool Enabled => _enabled;
        public int InternalWidth => Mathf.Max(160, _internalWidth);
        public int InternalHeight => Mathf.Max(90, _internalHeight);
        public bool ApplyToSceneView => _applyToSceneView;
        public bool OutlineEnabled => _outlineEnabled;
        public float OutlineThickness => Mathf.Max(0.5f, _outlineThickness);
        public float OutlineDepthThreshold => Mathf.Max(0.0001f, _outlineDepthThreshold);
        public float OutlineNormalThreshold => Mathf.Max(0.01f, _outlineNormalThreshold);
        public float OutlineStrength => Mathf.Clamp01(_outlineStrength);
        public Color OutlineColor => _outlineColor;
        public float UpscaleSharpness => Mathf.Clamp01(_upscaleSharpness);
        public bool UseHdrWhenAvailable => _useHdrWhenAvailable;
    }
}
