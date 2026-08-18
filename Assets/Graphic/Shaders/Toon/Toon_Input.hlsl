#ifndef TOON_INPUT_INCLUDED
#define TOON_INPUT_INCLUDED

// CBUFFER de material e samplers do shader unificado Toon Standard.
// Substitui Toon_Opaque_Input.hlsl e Toon_Metallic_Input.hlsl: agora
// existe apenas um CBUFFER com todos os campos usados pelos 3 modos
// de superficie (Opaque, Metallic, Transparent).

#include "Toon_Common.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _EmissionMap_ST;
    float4 _BaseColor;
    float4 _ShadeColor;
    float4 _EmissionColor;
    float4 _OutlineColor;
    float  _ShadeBlend;
    float  _ShadeSoftness;
    float  _SpecEnabled;
    float  _SpecIntensity;
    float  _SpecSmoothness;
    float  _SpecSoftness;
    float  _BackLightEnabled;
    float  _Cutoff;
    float  _OutlineThickness;
    // Usado apenas quando _SurfaceType = Transparent.
    float  _Opacity;
    // Usados apenas quando _SurfaceType = Metallic.
    float  _ReflectionIntensity;
    float  _ReflectionSmoothness;
CBUFFER_END

TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

#endif // TOON_INPUT_INCLUDED
