Shader "Hidden/DiveProtocol/YoOutlineV2/PostProcess"
{
    Properties
    {
        _YoNoiseTexture ("Noise Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "YoOutlineV2"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            TEXTURE2D(_YoNoiseTexture);
            SAMPLER(sampler_YoNoiseTexture);

            float _YoDebugMode;
            float4 _YoOutlineColor;
            float _YoOutlineOpacity;
            float _YoDepthThreshold;
            float _YoNormalThreshold;
            float _YoDepthStrength;
            float _YoNormalStrength;
            float _YoNearThickness;
            float _YoFarThickness;
            float _YoNearDistance;
            float _YoFarDistance;
            float _YoNoiseEnabled;
            float _YoHasNoiseTexture;
            float _YoNoiseScale;
            float _YoBreakThreshold;
            float _YoBreakSoftness;
            float _YoDepthNoiseInfluence;
            float _YoNormalNoiseInfluence;
            float _YoDarkAreaSuppression;
            float _YoDarkAreaStart;
            float _YoDarkAreaEnd;

            float SampleEyeDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return max(LinearEyeDepth(rawDepth, _ZBufferParams), 0.0001);
            }

            float3 SampleNormalWS(float2 uv)
            {
                float3 normalWS = SampleSceneNormals(uv);

                if (normalWS.x >= 0.0 && normalWS.y >= 0.0 && normalWS.z >= 0.0)
                {
                    normalWS = normalWS * 2.0 - 1.0;
                }

                return normalize(normalWS + 1e-5);
            }

            float3 ReconstructWorldPosition(float2 uv, float rawDepth)
            {
                #if UNITY_REVERSED_Z
                    real depth = rawDepth;
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif

                return ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0, 0, 0));
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            float TextureNoiseTriplanar(float3 positionWS, float3 normalWS)
            {
                float3 weights = abs(normalWS);
                weights = max(weights, 0.001);
                weights /= max(dot(weights, 1.0), 0.001);

                float scale = max(_YoNoiseScale, 0.001);
                float nx = SAMPLE_TEXTURE2D(_YoNoiseTexture, sampler_YoNoiseTexture, positionWS.zy * scale).r;
                float ny = SAMPLE_TEXTURE2D(_YoNoiseTexture, sampler_YoNoiseTexture, positionWS.xz * scale).r;
                float nz = SAMPLE_TEXTURE2D(_YoNoiseTexture, sampler_YoNoiseTexture, positionWS.xy * scale).r;
                return nx * weights.x + ny * weights.y + nz * weights.z;
            }

            float WorldNoise(float3 positionWS, float3 normalWS)
            {
                if (_YoNoiseEnabled < 0.5)
                {
                    return 1.0;
                }

                if (_YoHasNoiseTexture > 0.5)
                {
                    return TextureNoiseTriplanar(positionWS, normalWS);
                }

                float scale = max(_YoNoiseScale, 0.001);
                float baseNoise = ValueNoise3D(positionWS * scale);
                float detailNoise = ValueNoise3D(positionWS * scale * 2.17 + 19.37);
                return saturate(baseNoise * 0.7 + detailNoise * 0.3);
            }

            float ComputeThickness(float eyeDepth)
            {
                float nearDistance = max(_YoNearDistance, 0.001);
                float farDistance = max(_YoFarDistance, nearDistance + 0.001);
                float t = saturate((eyeDepth - nearDistance) / (farDistance - nearDistance));
                return lerp(_YoNearThickness, _YoFarThickness, t);
            }

            float DepthEdgeFourTap(float2 uv, float2 texel)
            {
                float center = SampleEyeDepth(uv);
                float left = SampleEyeDepth(uv + float2(-texel.x, 0.0));
                float right = SampleEyeDepth(uv + float2(texel.x, 0.0));
                float down = SampleEyeDepth(uv + float2(0.0, -texel.y));
                float up = SampleEyeDepth(uv + float2(0.0, texel.y));

                float horizontal = abs(left - center) + abs(right - center);
                float vertical = abs(down - center) + abs(up - center);
                return max(horizontal, vertical) / max(center, 0.0001);
            }

            float NormalEdgeFourTap(float2 uv, float2 texel)
            {
                float3 center = SampleNormalWS(uv);
                float3 left = SampleNormalWS(uv + float2(-texel.x, 0.0));
                float3 right = SampleNormalWS(uv + float2(texel.x, 0.0));
                float3 down = SampleNormalWS(uv + float2(0.0, -texel.y));
                float3 up = SampleNormalWS(uv + float2(0.0, texel.y));

                float horizontal = max(length(left - center), length(right - center));
                float vertical = max(length(down - center), length(up - center));
                return max(horizontal, vertical);
            }

            float DarkSuppression(float3 color)
            {
                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                float visible = smoothstep(_YoDarkAreaStart, max(_YoDarkAreaEnd, _YoDarkAreaStart + 0.001), luminance);
                return lerp(1.0 - _YoDarkAreaSuppression, 1.0, visible);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Debug mode 0: prove the pass executes and writes back camera color.
                // This path intentionally does not read depth, normals, or noise.
                if (_YoDebugMode >= -0.5 && _YoDebugMode < 0.5)
                {
                    return float4(1.0, 0.0, 1.0, sceneColor.a);
                }

                float rawDepth = SampleSceneDepth(uv);
                float eyeDepth = max(LinearEyeDepth(rawDepth, _ZBufferParams), 0.0001);
                float thickness = ComputeThickness(eyeDepth);
                float2 texel = _BlitTexture_TexelSize.xy * thickness;

                float depthRaw = DepthEdgeFourTap(uv, texel);
                float normalRaw = NormalEdgeFourTap(uv, texel);

                float depthEdge = saturate(smoothstep(_YoDepthThreshold, _YoDepthThreshold * 2.0, depthRaw) * _YoDepthStrength);
                float normalEdge = saturate(smoothstep(_YoNormalThreshold, _YoNormalThreshold * 1.5, normalRaw) * _YoNormalStrength);

                float3 normalWS = SampleNormalWS(uv);
                float3 positionWS = ReconstructWorldPosition(uv, rawDepth);
                float noiseValue = WorldNoise(positionWS, normalWS);
                float breakMask = _YoNoiseEnabled > 0.5
                    ? smoothstep(_YoBreakThreshold - _YoBreakSoftness, _YoBreakThreshold + _YoBreakSoftness, noiseValue)
                    : 1.0;

                float depthMask = depthEdge * lerp(1.0, breakMask, _YoDepthNoiseInfluence);
                float normalMask = normalEdge * lerp(1.0, breakMask, _YoNormalNoiseInfluence);
                float finalMask = saturate(max(depthMask, normalMask));
                finalMask *= DarkSuppression(sceneColor.rgb);

                if (_YoDebugMode >= 0.5 && _YoDebugMode < 1.5)
                {
                    return float4(depthEdge.xxx, sceneColor.a);
                }

                if (_YoDebugMode >= 1.5 && _YoDebugMode < 2.5)
                {
                    return float4(normalEdge.xxx, sceneColor.a);
                }

                if (_YoDebugMode >= 3.5 && _YoDebugMode < 4.5)
                {
                    return float4(breakMask.xxx, sceneColor.a);
                }

                if (_YoDebugMode >= 4.5 && _YoDebugMode < 5.5)
                {
                    return float4(finalMask.xxx, sceneColor.a);
                }

                float outlineAmount = saturate(finalMask * _YoOutlineOpacity);
                float3 color = lerp(sceneColor.rgb, _YoOutlineColor.rgb, outlineAmount);
                return float4(color, sceneColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
