Shader "Hidden/DiveProtocol/SignalLikePixel/Composite"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Composite Low Resolution Scene And Outline"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_SignalLikePixelOutlineTex);
            SAMPLER(sampler_SignalLikePixelOutlineTex);

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 sceneColor = FragBlit(input, sampler_LinearClamp);
                float4 outline = SAMPLE_TEXTURE2D_X(_SignalLikePixelOutlineTex, sampler_PointClamp, input.texcoord);

                sceneColor.rgb = lerp(sceneColor.rgb, outline.rgb, saturate(outline.a));
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
