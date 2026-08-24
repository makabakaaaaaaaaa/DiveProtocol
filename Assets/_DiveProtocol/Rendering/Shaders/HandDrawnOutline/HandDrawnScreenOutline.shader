Shader "Hidden/DiveProtocol/HandDrawnOutline/ScreenOutline"
{
    Properties
    {
        _HDNoiseTexture ("Noise Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "HandDrawnScreenOutline"
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

            TEXTURE2D(_HDNoiseTexture);
            SAMPLER(sampler_HDNoiseTexture);

            float4 _HDOutlineColor;
            float _HDOutlineOpacity;
            float _HDDepthThreshold;
            float _HDNormalThreshold;
            float _HDDepthStrength;
            float _HDNormalStrength;
            float _HDNearThickness;
            float _HDFarThickness;
            float _HDNearDistance;
            float _HDFarDistance;
            float _HDNoiseEnabled;
            float _HDHasNoiseTexture;
            float _HDNoiseScale;
            float _HDBreakThreshold;
            float _HDBreakSoftness;
            float _HDDepthNoiseInfluence;
            float _HDNormalNoiseInfluence;
            float _HDDarkAreaSuppression;
            float _HDDarkAreaStart;
            float _HDDarkAreaEnd;
            float _HDDebugMode;

            float SafeLinearEyeDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return max(LinearEyeDepth(rawDepth, _ZBufferParams), 0.0001);
            }

            float3 SafeSceneNormal(float2 uv)
            {
                float3 normalWS = SampleSceneNormals(uv);

                // URP forward DepthNormals generally stores normals in 0..1 space,
                // while some paths can provide already-decoded -1..1 values.
                // Support both so the feature stays usable across renderer modes.
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

            float SampleTextureNoiseTriplanar(float3 positionWS, float3 normalWS)
            {
                float3 weights = abs(normalWS);
                weights = max(weights, 0.001);
                weights /= max(dot(weights, 1.0), 0.001);

                float2 uvX = positionWS.zy * _HDNoiseScale;
                float2 uvY = positionWS.xz * _HDNoiseScale;
                float2 uvZ = positionWS.xy * _HDNoiseScale;

                float nx = SAMPLE_TEXTURE2D(_HDNoiseTexture, sampler_HDNoiseTexture, uvX).r;
                float ny = SAMPLE_TEXTURE2D(_HDNoiseTexture, sampler_HDNoiseTexture, uvY).r;
                float nz = SAMPLE_TEXTURE2D(_HDNoiseTexture, sampler_HDNoiseTexture, uvZ).r;
                return nx * weights.x + ny * weights.y + nz * weights.z;
            }

            float SampleWorldNoise(float3 positionWS, float3 normalWS)
            {
                if (_HDNoiseEnabled < 0.5)
                {
                    return 1.0;
                }

                if (_HDHasNoiseTexture > 0.5)
                {
                    return SampleTextureNoiseTriplanar(positionWS, normalWS);
                }

                float scale = max(_HDNoiseScale, 0.001);
                float baseNoise = ValueNoise3D(positionWS * scale);
                float detailNoise = ValueNoise3D(positionWS * scale * 2.17 + 19.37);
                return saturate(baseNoise * 0.7 + detailNoise * 0.3);
            }

            float RobertsDepthEdge(float2 uv, float2 offset)
            {
                float d0 = SafeLinearEyeDepth(uv + float2(-offset.x, -offset.y));
                float d1 = SafeLinearEyeDepth(uv + float2( offset.x,  offset.y));
                float d2 = SafeLinearEyeDepth(uv + float2( offset.x, -offset.y));
                float d3 = SafeLinearEyeDepth(uv + float2(-offset.x,  offset.y));

                float center = SafeLinearEyeDepth(uv);
                float edge = length(float2(d0 - d1, d2 - d3));
                return edge / max(center, 0.0001);
            }

            float RobertsNormalEdge(float2 uv, float2 offset)
            {
                float3 n0 = SafeSceneNormal(uv + float2(-offset.x, -offset.y));
                float3 n1 = SafeSceneNormal(uv + float2( offset.x,  offset.y));
                float3 n2 = SafeSceneNormal(uv + float2( offset.x, -offset.y));
                float3 n3 = SafeSceneNormal(uv + float2(-offset.x,  offset.y));

                float a = length(n0 - n1);
                float b = length(n2 - n3);
                return max(a, b);
            }

            float ComputeThickness(float eyeDepth)
            {
                float nearD = max(_HDNearDistance, 0.001);
                float farD = max(_HDFarDistance, nearD + 0.001);
                float t = saturate((eyeDepth - nearD) / (farD - nearD));
                return lerp(_HDNearThickness, _HDFarThickness, t);
            }

            float ComputeDarkSuppression(float3 sceneColor)
            {
                float luminance = dot(sceneColor, float3(0.2126, 0.7152, 0.0722));
                float visibleInDark = smoothstep(_HDDarkAreaStart, max(_HDDarkAreaEnd, _HDDarkAreaStart + 0.001), luminance);
                return lerp(1.0 - _HDDarkAreaSuppression, 1.0, visibleInDark);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (_HDDebugMode >= 4.5 && _HDDebugMode < 5.5)
                {
                    return float4(1.0, 0.0, 1.0, sceneColor.a);
                }

                float rawDepth = SampleSceneDepth(uv);
                float eyeDepth = max(LinearEyeDepth(rawDepth, _ZBufferParams), 0.0001);
                float thickness = ComputeThickness(eyeDepth);
                float2 texel = _BlitTexture_TexelSize.xy * thickness;

                float depthRawEdge = RobertsDepthEdge(uv, texel);
                float normalRawEdge = RobertsNormalEdge(uv, texel);

                float depthEdge = smoothstep(_HDDepthThreshold, _HDDepthThreshold * 2.0, depthRawEdge) * _HDDepthStrength;
                float normalEdge = smoothstep(_HDNormalThreshold, _HDNormalThreshold * 1.5, normalRawEdge) * _HDNormalStrength;
                depthEdge = saturate(depthEdge);
                normalEdge = saturate(normalEdge);

                float3 normalWS = SafeSceneNormal(uv);
                float3 positionWS = ReconstructWorldPosition(uv, rawDepth);
                float noiseValue = SampleWorldNoise(positionWS, normalWS);

                float breakMask = _HDNoiseEnabled > 0.5
                    ? smoothstep(_HDBreakThreshold - _HDBreakSoftness, _HDBreakThreshold + _HDBreakSoftness, noiseValue)
                    : 1.0;

                float depthNoiseMask = lerp(1.0, breakMask, _HDDepthNoiseInfluence);
                float normalNoiseMask = lerp(1.0, breakMask, _HDNormalNoiseInfluence);

                float depthOutlined = depthEdge * depthNoiseMask;
                float normalOutlined = normalEdge * normalNoiseMask;
                float outlineMask = saturate(max(depthOutlined, normalOutlined));
                outlineMask *= ComputeDarkSuppression(sceneColor.rgb);

                float finalMask = saturate(outlineMask * _HDOutlineOpacity);
                float3 finalColor = lerp(sceneColor.rgb, _HDOutlineColor.rgb, finalMask);

                if (_HDDebugMode > 0.5 && _HDDebugMode < 1.5)
                {
                    return float4(depthEdge.xxx, sceneColor.a);
                }

                if (_HDDebugMode >= 1.5 && _HDDebugMode < 2.5)
                {
                    return float4(normalEdge.xxx, sceneColor.a);
                }

                if (_HDDebugMode >= 2.5 && _HDDebugMode < 3.5)
                {
                    return float4(breakMask.xxx, sceneColor.a);
                }

                if (_HDDebugMode >= 3.5 && _HDDebugMode < 4.5)
                {
                    return float4(outlineMask.xxx, sceneColor.a);
                }

                return float4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
