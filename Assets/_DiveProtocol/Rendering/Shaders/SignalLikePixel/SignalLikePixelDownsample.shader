Shader "Hidden/DiveProtocol/SignalLikePixel/Downsample"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Bilinear Downsample"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 Fragment(Varyings input) : SV_Target
            {
                return FragBlit(input, sampler_LinearClamp);
            }
            ENDHLSL
        }
    }
}
