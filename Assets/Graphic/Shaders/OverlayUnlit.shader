Shader "Custom/OverlayUnlit"
{
    Properties
    {
        // _MainTex e o nome que o SpriteRenderer usa internamente
        _MainTex   ("Texture", 2D) = "white" {}
        _Color     ("Color Tint", Color) = (1,1,1,1)
        _Emission  ("Emission Color", Color) = (0,0,0,0)
        _AlphaClip ("Alpha Clip Threshold", Range(0,1)) = 0.01
    }
    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Overlay"
        }
        Pass
        {
            Name "OverlayUnlit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest  Always
            Blend  SrcAlpha OneMinusSrcAlpha
            Cull   Off
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _Emission;
                half   _AlphaClip;
            CBUFFER_END
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;   // SpriteRenderer passa cor e alpha aqui
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : COLOR;
            };
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                // vertex color carrega a cor do SpriteRenderer; _Color serve como tint adicional
                half4 col = tex * IN.color * _Color;
                col.rgb  += _Emission.rgb;
                // descarta pixels com alpha abaixo do threshold
                clip(col.a - _AlphaClip);
                return col;
            }
            ENDHLSL
        }
    }
}