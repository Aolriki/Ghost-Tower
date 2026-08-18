#ifndef TOON_LIGHTING_INCLUDED
#define TOON_LIGHTING_INCLUDED

// Iluminacao toon para URP 17 com main light, additional lights e back light.

float4 _BackLightDirWS;
float4 _BackLightColor;
float _BackLightPower;
float _BackLightIntensity;

// ----------------------------------------------------------------
// Ramp toon parametrico. Espera input em [-1, 1].
// softness = 0: corte hard; softness = 1: quase lambert.
// ----------------------------------------------------------------
float ToonRamp(float value, float softness)
{
    float halfWidth = max(softness * 0.5, 0.001);
    return smoothstep(0.5 - halfWidth, 0.5 + halfWidth, value * 0.5 + 0.5);
}

// Ramp para valores ja em [0, 1] (sombra projetada).
float ToonRamp01(float value, float softness)
{
    float halfWidth = max(softness * 0.5, 0.001);
    return smoothstep(0.5 - halfWidth, 0.5 + halfWidth, value);
}

float ToonSpecularRamp(float value, float softness)
{
    float halfWidth = max(softness * 0.5, 0.001);
    return smoothstep(0.5 - halfWidth, 0.5 + halfWidth, value);
}

// ----------------------------------------------------------------
// Contribuicao de UMA luz toon.
// Retorna a cor que esta luz adiciona (diffuse difuso + specular).
//
// Parametros de atenuacao separados:
//  shadowAtten   = sombra projetada (0 = na sombra, 1 = iluminado), pelo ramp
//  distanceAtten = falloff de distancia/range (0..1), linear
// ----------------------------------------------------------------
float3 ToonLightContribution(
    float3 lightDirWS,
    float3 lightColor,
    float shadowAtten,
    float distanceAtten,
    float3 normalWS,
    float3 viewDirWS,
    float3 litColor, // cor do material quando totalmente iluminado
    float shadeSoftness,
    float specIntensity,
    float specSmoothness,
    float specSoftness,
    bool specEnabled)
{
    // 1) Auto-sombra: NdotL pelo ramp toon.
    float NdotL = dot(normalWS, lightDirWS);
    float diffuseRamp = ToonRamp(NdotL, shadeSoftness);

    // 2) Sombra projetada: shadowAtten pelo ramp toon (mesma softness).
    //    Isso da a borda estilizada E suave para sombras de oclusao.
    float shadowRamp = ToonRamp01(shadowAtten, shadeSoftness);

    // 3) Fator de luz "toonizado" (auto-sombra * sombra projetada).
    float toonFactor = diffuseRamp * shadowRamp;

    // 4) Diffuse: cor iluminada modulada pelo fator toon e pela cor da luz.
    //    distanceAtten multiplica LINEARMENTE no fim, preserva o range.
    float3 diffuse = litColor * toonFactor * lightColor * distanceAtten;

    float3 result = diffuse;

    // 5) Specular Blinn-Phong toon.
    //    NAO depende da base color: mascarado apenas por toonFactor
    //    (luz/sombra) e distanceAtten. Assim funciona mesmo com base preta,
    //    igual para main e additional lights.
    if (specEnabled)
    {
        float3 halfDir = normalize(lightDirWS + viewDirWS);
        float NdotH = saturate(dot(normalWS, halfDir));
        float specPower = exp2(specSmoothness * 10.0 + 1.0);
        float specRaw = pow(NdotH, specPower);
        float specToon = ToonSpecularRamp(specRaw, specSoftness);

        result += specToon * specIntensity * lightColor * toonFactor * distanceAtten;
    }

    return result;
}

// ----------------------------------------------------------------
// FUNCAO PRINCIPAL
// ----------------------------------------------------------------
void CalculateToonLighting_float(
    float3 PositionWS,
    float3 NormalWS,
    float3 ViewDirWS,
    float4 ScreenPos,
    float3 BaseColor,
    float3 ShadeColor,
    float ShadeBlend,
    float ShadeSoftness,
    float SpecIntensity,
    float SpecSmoothness,
    float SpecSoftness,
    float SpecEnabled,
    float BackLightEnabled,
    out float3 Color)
{
#ifdef SHADERGRAPH_PREVIEW
    float3 shadowedColor = lerp(BaseColor, ShadeColor, ShadeBlend);
    float fakeNdotL = dot(normalize(NormalWS), normalize(float3(0.5, 0.8, -0.3)));
    float ramp = ToonRamp(fakeNdotL, ShadeSoftness);
    Color = lerp(shadowedColor, BaseColor, ramp);
    return;
#else

    NormalWS = normalize(NormalWS);
    ViewDirWS = normalize(ViewDirWS);
    bool specBool = SpecEnabled > 0.5;

    // shadeColor = cor nas areas de sombra (floor ambiente).
    // litColor   = quanto cada luz pode "revelar" da base color por cima.
    float3 shadowedColor = lerp(BaseColor, ShadeColor, ShadeBlend);
    float3 litColor = BaseColor - shadowedColor;

    // Comeca no floor de sombra. Cada luz adiciona seu delta iluminado.
    float3 accum = shadowedColor;

    // ---- Main directional light ----
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
    Light mainLight = GetMainLight(shadowCoord);

    accum += ToonLightContribution(
        mainLight.direction, mainLight.color,
        mainLight.shadowAttenuation, // sombra projetada, pelo ramp
        mainLight.distanceAttenuation, // range, linear
        NormalWS, ViewDirWS, litColor, ShadeSoftness,
        SpecIntensity, SpecSmoothness, SpecSoftness, specBool);

    // ---- Additional lights ----
    InputData inputData = (InputData) 0;
    inputData.positionWS = PositionWS;
    inputData.normalWS = NormalWS;
    inputData.viewDirectionWS = ViewDirWS;
    inputData.shadowCoord = shadowCoord;
    inputData.positionCS = TransformWorldToHClip(PositionWS);
    inputData.normalizedScreenSpaceUV = ScreenPos.xy;

    uint meshRenderingLayers = GetMeshRenderingLayer();

#ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light addLight = GetAdditionalLight(lightIndex, PositionWS, half4(1,1,1,1));

#ifdef _LIGHT_LAYERS
        if (!IsMatchingLightLayer(addLight.layerMask, meshRenderingLayers))
            continue;
#endif

        // Mesmos parametros separados da main: sombra projetada pelo ramp,
        // distanceAttenuation linear. Garante comportamento identico.
        accum += ToonLightContribution(
            addLight.direction, addLight.color,
            addLight.shadowAttenuation,
            addLight.distanceAttenuation,
            NormalWS, ViewDirWS, litColor, ShadeSoftness,
            SpecIntensity, SpecSmoothness, SpecSoftness, specBool);
    LIGHT_LOOP_END
#endif

    // ---- Back Light ----
    if (BackLightEnabled > 0.5)
    {
        float rim = pow(saturate(1.0 - dot(NormalWS, ViewDirWS)), max(_BackLightPower, 0.01));
        float facing = saturate(dot(NormalWS, -_BackLightDirWS.xyz));
        accum += _BackLightColor.rgb * rim * facing * _BackLightIntensity;
    }

    Color = accum;
#endif
}

#endif // TOON_LIGHTING_INCLUDED