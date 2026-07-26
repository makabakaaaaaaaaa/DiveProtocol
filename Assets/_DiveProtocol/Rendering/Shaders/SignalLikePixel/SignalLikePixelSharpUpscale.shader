Shader "Hidden/DiveProtocol/SignalLikePixel/SharpUpscale"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Sharp Upscale"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _SignalLikePixelLowTexelSize;
            float _SignalLikePixelUpscaleSharpness;

            float4 SampleLow(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 texel = _SignalLikePixelLowTexelSize.xy;

                float4 center = SampleLow(uv);
                float4 average =
                    SampleLow(uv + float2(texel.x, 0.0)) +
                    SampleLow(uv + float2(-texel.x, 0.0)) +
                    SampleLow(uv + float2(0.0, texel.y)) +
                    SampleLow(uv + float2(0.0, -texel.y));
                average *= 0.25;

                float sharpness = saturate(_SignalLikePixelUpscaleSharpness) * 0.75;
                float4 sharpened = center + (center - average) * sharpness;
                sharpened.a = center.a;
                return saturate(sharpened);
            }
            ENDHLSL
        }
    }
}
