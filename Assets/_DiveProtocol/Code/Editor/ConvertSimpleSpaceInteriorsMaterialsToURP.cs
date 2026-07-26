using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DiveProtocol.Editor
{
    /// <summary>
    /// One-off converter for the imported SimpleSpaceInteriors material pack.
    /// It intentionally only touches assets under Assets/SimpleSpaceInteriors.
    /// </summary>
    public static class ConvertSimpleSpaceInteriorsMaterialsToURP
    {
        private const string RootPath = "Assets/SimpleSpaceInteriors";
        private const string MaterialsPath = RootPath + "/Materials";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        [MenuItem("Dive Protocol/Tools/Convert SimpleSpaceInteriors Materials To URP")]
        public static void ConvertFromMenu()
        {
            ConvertMaterials();
        }

        /// <summary>
        /// Batchmode entry point for converting SimpleSpaceInteriors materials to URP/Lit.
        /// </summary>
        public static void ConvertMaterials()
        {
            Shader litShader = Shader.Find(LitShaderName);
            if (litShader == null)
            {
                Debug.LogError($"[SimpleSpaceInteriors] Could not find shader '{LitShaderName}'. Is URP installed?");
                return;
            }

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsPath });
            int convertedCount = 0;
            int alreadyUrpCount = 0;
            int skippedCount = 0;
            List<string> reviewMaterials = new List<string>();

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith(RootPath))
                {
                    skippedCount++;
                    continue;
                }

                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    skippedCount++;
                    continue;
                }

                Shader originalShader = material.shader;
                string originalShaderName = originalShader != null ? originalShader.name : "<missing>";
                if (originalShaderName.StartsWith("Universal Render Pipeline/"))
                {
                    alreadyUrpCount++;
                    continue;
                }

                MaterialSnapshot snapshot = MaterialSnapshot.Capture(material);
                Undo.RecordObject(material, "Convert SimpleSpaceInteriors Material To URP");

                material.shader = litShader;
                snapshot.ApplyToUrpLit(material);
                ConfigureSurfaceOptions(material, snapshot, path, reviewMaterials);

                EditorUtility.SetDirty(material);
                convertedCount++;
                Debug.Log($"[SimpleSpaceInteriors] Converted {path}: {originalShaderName} -> {LitShaderName}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[SimpleSpaceInteriors] URP conversion complete. Converted={convertedCount}, AlreadyURP={alreadyUrpCount}, Skipped={skippedCount}, Review={reviewMaterials.Count}");

            for (int i = 0; i < reviewMaterials.Count; i++)
            {
                Debug.LogWarning($"[SimpleSpaceInteriors] Please review material settings manually: {reviewMaterials[i]}");
            }
        }

        private static void ConfigureSurfaceOptions(Material material, MaterialSnapshot snapshot, string path, List<string> reviewMaterials)
        {
            bool transparentByName = path.ToLowerInvariant().Contains("water") || path.ToLowerInvariant().Contains("bubble");
            bool transparentByAlpha = snapshot.Color.a < 0.99f;
            bool shouldBeTransparent = transparentByName || transparentByAlpha;

            if (shouldBeTransparent)
            {
                SetFloatIfPresent(material, "_Surface", 1f);
                SetFloatIfPresent(material, "_Blend", 0f);
                SetFloatIfPresent(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                SetFloatIfPresent(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                SetFloatIfPresent(material, "_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                reviewMaterials.Add(path);
                return;
            }

            SetFloatIfPresent(material, "_Surface", 0f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            SetFloatIfPresent(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            SetFloatIfPresent(material, "_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = -1;
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            if (texture != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetColorIfPresent(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private readonly struct MaterialSnapshot
        {
            private readonly Texture _mainTexture;
            private readonly Texture _normalMap;
            private readonly Texture _emissionMap;
            private readonly float _metallic;
            private readonly float _smoothness;
            private readonly Color _emissionColor;

            public Color Color { get; }

            private MaterialSnapshot(
                Texture mainTexture,
                Texture normalMap,
                Texture emissionMap,
                Color color,
                float metallic,
                float smoothness,
                Color emissionColor)
            {
                _mainTexture = mainTexture;
                _normalMap = normalMap;
                _emissionMap = emissionMap;
                Color = color;
                _metallic = metallic;
                _smoothness = smoothness;
                _emissionColor = emissionColor;
            }

            public static MaterialSnapshot Capture(Material material)
            {
                Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                Texture normalMap = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
                Texture emissionMap = material.HasProperty("_EmissionMap") ? material.GetTexture("_EmissionMap") : null;
                Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
                float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.5f;
                Color emissionColor = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;

                return new MaterialSnapshot(mainTexture, normalMap, emissionMap, color, metallic, smoothness, emissionColor);
            }

            public void ApplyToUrpLit(Material material)
            {
                SetTextureIfPresent(material, "_BaseMap", _mainTexture);
                SetColorIfPresent(material, "_BaseColor", Color);
                SetTextureIfPresent(material, "_BumpMap", _normalMap);
                SetTextureIfPresent(material, "_EmissionMap", _emissionMap);
                SetColorIfPresent(material, "_EmissionColor", _emissionColor);
                SetFloatIfPresent(material, "_Metallic", _metallic);
                SetFloatIfPresent(material, "_Smoothness", _smoothness);

                if (_normalMap != null)
                {
                    material.EnableKeyword("_NORMALMAP");
                }

                if (_emissionMap != null || _emissionColor.maxColorComponent > 0.001f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }
        }
    }
}
