#ifndef TOON_FORWARD_PASS_INCLUDED
#define TOON_FORWARD_PASS_INCLUDED

// Vertex e fragment do pass principal, compartilhado entre Opaque, Transparent e Metallic.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "ToonLighting.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS   : TEXCOORD1;
    float2 uv         : TEXCOORD2;
    float4 screenPos  : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vertex(Attributes IN)
{
    Varyings OUT = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    VertexPositionInputs vertexInputs = GetVertexPositionInputs(IN.positionOS.xyz);
    VertexNormalInputs   normalInputs = GetVertexNormalInputs(IN.normalOS);

    OUT.positionCS = vertexInputs.positionCS;
    OUT.positionWS = vertexInputs.positionWS;
    OUT.normalWS   = normalInputs.normalWS;
    OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
    OUT.screenPos  = ComputeScreenPos(vertexInputs.positionCS);

    return OUT;
}

half4 Fragment(Varyings IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
    half4 albedo  = baseTex * _BaseColor;

    #ifdef _ALPHATEST_ON
        clip(albedo.a - _Cutoff);
    #endif

    half outputAlpha;
    #ifdef _TRANSPARENT_ON
        outputAlpha = albedo.a * _Opacity;
    #else
        outputAlpha = 1.0;
    #endif

    float3 normalWS  = normalize(IN.normalWS);
    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
    float4 screenPosNormalized = float4(IN.screenPos.xy / IN.screenPos.w, IN.screenPos.zw);

    // Parametros que mudam por shader.
    #ifdef _METALLIC_ON
        // Metal sempre tem specular ligado e nao usa camera fade.
        float specEnabledParam = 1.0;
        float cameraFadeParam  = 0.0;
    #else
        float specEnabledParam = _SpecEnabled;
        float cameraFadeParam  = _CameraFade;
    #endif

    float3 litColor;
    CalculateToonLighting_float(
        IN.positionWS,
        normalWS,
        viewDirWS,
        screenPosNormalized,
        albedo.rgb,
        _ShadeColor.rgb,
        _ShadeBlend,
        _ShadeSoftness,
        _SpecIntensity,
        _SpecSmoothness,
        _SpecSoftness,
        specEnabledParam,
        _BackLightEnabled,
        cameraFadeParam,
        litColor
    );

    // Reflexo de ambiente (reflection probe / skybox), apenas no Metallic.
    #ifdef _METALLIC_ON
        // Vetor de reflexo: direcao da camera refletida em torno da normal.
        float3 reflectVec = reflect(-viewDirWS, normalWS);

        // perceptualRoughness: 0 = espelho nitido, 1 = reflexo borrado.
        half perceptualRoughness = 1.0 - _ReflectionSmoothness;

        // Assinatura de 5 argumentos (URP 17): suporta reflection probe blending e box projection.
        half3 reflection = GlossyEnvironmentReflection(
            reflectVec,
            IN.positionWS,
            perceptualRoughness,
            1.0,
            screenPosNormalized.xy
        );

        // Fresnel: reflexo mais forte nas bordas (angulo rasante), como metal real.
        half fresnel = pow(saturate(1.0 - dot(normalWS, viewDirWS)), 4.0);

        // Tinta o reflexo com metade da base color para o tom metalico.
        float3 tintedReflection = reflection * lerp(float3(1, 1, 1), albedo.rgb, 0.5);

        litColor += tintedReflection * _ReflectionIntensity * fresnel;
    #endif

    float3 emission = float3(0, 0, 0);
    #ifdef _EMISSION_ON
        emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb;
    #endif

    return half4(litColor + emission, outputAlpha);
}

#endif // TOON_FORWARD_PASS_INCLUDED
