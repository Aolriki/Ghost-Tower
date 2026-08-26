#ifndef TOON_FORWARD_PASS_INCLUDED
#define TOON_FORWARD_PASS_INCLUDED

// Vertex e fragment do pass principal do shader unificado Toon Standard.
// O comportamento por modo de superficie (Opaque, Metallic, Transparent)
// e controlado pelas keywords _SURFACE_METALLIC e _SURFACE_TRANSPARENT,
// que sao ligadas pelo ToonStandardShaderGUI de acordo com _SurfaceType.
// Normal Map (_NORMALMAP_ON) e Tiling Global (_WORLD_TILING_ON) sao
// controlados por toggles proprios, independentes do Surface Type.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "ToonLighting.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2; // xyz = tangente, w = sinal da bitangente
    float2 uv : TEXCOORD3;
    float4 screenPos : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

    Varyings Vertex(Attributes IN)
    {
        Varyings OUT = (Varyings) 0;
        UNITY_SETUP_INSTANCE_ID(IN);
        UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

        VertexPositionInputs vertexInputs = GetVertexPositionInputs(IN.positionOS.xyz);
        VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

        OUT.positionCS = vertexInputs.positionCS;
        OUT.positionWS = vertexInputs.positionWS;
        OUT.normalWS = normalInputs.normalWS;

    // Sinal da bitangente, corrigido para malhas com escala negativa.
        real tangentSign = IN.tangentOS.w * GetOddNegativeScale();
        OUT.tangentWS = float4(normalInputs.tangentWS, tangentSign);

        OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
        OUT.screenPos = ComputeScreenPos(vertexInputs.positionCS);

        return OUT;
    }

    half4 Fragment(Varyings IN) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(IN);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    // UV base: da malha por padrao, ou da posicao no mundo (plano XZ)
    // quando o Tiling Global esta ligado. Isso evita textura quebrada
    // em pecas de chao rotacionadas, pois o UV nao depende mais da
    // orientacao/UV da malha. Base, Normal e Emission usam o mesmo UV.
        float2 uv = IN.uv;
#ifdef _WORLD_TILING_ON
        uv = IN.positionWS.xz * _WorldTilingScale;
#endif

        half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
        half4 albedo = baseTex * _BaseColor;

#ifdef _ALPHATEST_ON
        clip(albedo.a - _Cutoff);
#endif

        half outputAlpha;
#ifdef _SURFACE_TRANSPARENT
        outputAlpha = albedo.a * _Opacity;
#else
        outputAlpha = 1.0;
#endif

        float3 normalWS = normalize(IN.normalWS);
        float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
        float4 screenPosNormalized = float4(IN.screenPos.xy / IN.screenPos.w, IN.screenPos.zw);

    // Normal Map: substitui a normal do vertice pela normal detalhada
    // do tangent space, escalada por _NormalIntensity.
#ifdef _NORMALMAP_ON
        float3 tangentWS   = normalize(IN.tangentWS.xyz);
        float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentWS.w;
        float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);

        half4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv);
        half3 normalTS = UnpackNormalScale(normalSample, _NormalIntensity);
        normalWS = normalize(mul(normalTS, tangentToWorld));
#endif

    // Parametros que mudam por modo de superficie.
#ifdef _SURFACE_METALLIC
        // Metallic sempre tem specular ligado.
        float specEnabledParam = 1.0;
#else
        float specEnabledParam = _SpecEnabled;
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
        litColor
    );

    // Reflexo de ambiente (reflection probe / skybox), apenas no modo Metallic.
#ifdef _SURFACE_METALLIC
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
        emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif

        return half4(litColor + emission, outputAlpha);
    }

#endif // TOON_FORWARD_PASS_INCLUDED
