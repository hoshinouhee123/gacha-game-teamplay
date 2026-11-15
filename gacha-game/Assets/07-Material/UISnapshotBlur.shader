
Shader "Custom/UISnapshotBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_TexelSize;
            float _BlurSize;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 d = _MainTex_TexelSize.xy * _BlurSize;

                half4 color =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * 4.0;

                color += SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, uv + float2(d.x, 0)) * 2.0;
                color += SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, uv - float2(d.x, 0)) * 2.0;
                color += SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, uv + float2(0, d.y)) * 2.0;
                color += SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, uv - float2(0, d.y)) * 2.0;

                color += SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, uv + d);
                color += SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, uv - d);
                color += SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex,
                    uv + float2(d.x, -d.y));
                color += SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex,
                    uv + float2(-d.x, d.y));

                return color / 16.0;
            }
            ENDHLSL
        }
    }
}
