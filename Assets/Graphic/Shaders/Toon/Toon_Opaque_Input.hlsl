#ifndef TOON_OPAQUE_INPUT_INCLUDED
#define TOON_OPAQUE_INPUT_INCLUDED

// CBUFFER de material e samplers, compartilhados entre Toon_Opaque e Toon_Transparent.

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
    float  _CameraFade;
    float  _Cutoff;
    float  _OutlineThickness;
    // Usado apenas pelo Toon_Transparent.
    float  _Opacity;
CBUFFER_END

TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

#endif // TOON_OPAQUE_INPUT_INCLUDED
