#ifndef TOON_METALLIC_INPUT_INCLUDED
#define TOON_METALLIC_INPUT_INCLUDED

// CBUFFER de material e samplers do Toon_Metallic, compartilhados por todos os seus passes.

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
    float  _SpecIntensity;
    float  _SpecSmoothness;
    float  _SpecSoftness;
    float  _BackLightEnabled;
    float  _Cutoff;
    float  _OutlineThickness;
    float  _ReflectionIntensity;
    float  _ReflectionSmoothness;
CBUFFER_END

TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

#endif // TOON_METALLIC_INPUT_INCLUDED
