Shader "Hidden/DiveProtocol/SignalLikePixel/Outline"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Low Resolution Depth Normal Outline"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _SignalLikePixelLowTexelSize;
            float _SignalLikePixelOutlineEnabled;
            float _SignalLikePixelOutlineThickness;
            float _SignalLikePixelOutlineDepthThreshold;
            float _SignalLikePixelOutlineNormalThreshold;
            float _SignalLikePixelOutlineStrength;
            float4 _SignalLikePixelOutlineColor;

            float SampleLinearDepth(float2 uv)
            {
                return Linear01Depth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                if (_SignalLikePixelOutlineEnabled < 0.5)
                {
                    return 0;
                }

                float2 uv = input.texcoord;
                float2 texel = _SignalLikePixelLowTexelSize.xy * _SignalLikePixelOutlineThickness;

                float centerDepth = SampleLinearDepth(uv);
                float3 centerNormal = normalize(SampleSceneNormals(uv) * 2.0 - 1.0);

                float2 offsets[4] =
                {
                    float2(texel.x, 0.0),
                    float2(-texel.x, 0.0),
                    float2(0.0, texel.y),
                    float2(0.0, -texel.y)
                };

                float depthEdge = 0.0;
                float normalEdge = 0.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float2 sampleUv = saturate(uv + offsets[i]);
                    float sampleDepth = SampleLinearDepth(sampleUv);
                    float3 sampleNormal = normalize(SampleSceneNormals(sampleUv) * 2.0 - 1.0);

                    depthEdge = max(depthEdge, abs(centerDepth - sampleDepth));
                    normalEdge = max(normalEdge, 1.0 - saturate(dot(centerNormal, sampleNormal)));
                }

                float depthMask = smoothstep(_SignalLikePixelOutlineDepthThreshold, _SignalLikePixelOutlineDepthThreshold * 2.0, depthEdge);
                float normalMask = smoothstep(_SignalLikePixelOutlineNormalThreshold, _SignalLikePixelOutlineNormalThreshold * 1.5, normalEdge);
                float edge = saturate(max(depthMask, normalMask) * _SignalLikePixelOutlineStrength);

                return float4(_SignalLikePixelOutlineColor.rgb, edge);
            }
            ENDHLSL
        }
    }
}
