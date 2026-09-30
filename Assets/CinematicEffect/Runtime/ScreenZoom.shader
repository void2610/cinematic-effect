Shader "Hidden/CinematicEffect/ScreenZoom"
{
    // 描画済みの画面を注視点を中心に拡大する (カメラが寄って見える)。Screen Space - Camera の UI も含めて一枚絵として寄る。
    // AddBlitPass 経由で _BlitTexture にカメラカラーがバインドされる前提。
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "ScreenZoom"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // TEXTURE2D_X は URP の Core.hlsl が定義するため Blit.hlsl より先に include する (内蔵 PP シェーダと同順)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Zoom;         // 拡大率 (1=等倍)
            float2 _ViewCenter;  // 画面中央に映す元画面の位置 (UV)。画面外をサンプルしない範囲へ収めるのは C# 側が担う

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = _ViewCenter + (input.texcoord - 0.5) / max(_Zoom, 1.0);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
