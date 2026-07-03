#ifndef TOON_COMMON_INCLUDED
#define TOON_COMMON_INCLUDED

// Helpers de amostragem e alpha compartilhados pelos Inputs dos shaders toon.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Funcoes esperadas pelos passes built-in da URP (ShadowCaster, DepthOnly, DepthNormals).
half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv);
}

half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    half alpha = albedoAlpha * color.a;
    #ifdef _ALPHATEST_ON
        clip(alpha - cutoff);
    #endif
    return alpha;
}

#endif // TOON_COMMON_INCLUDED
