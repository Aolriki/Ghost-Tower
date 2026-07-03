#ifndef TOON_OUTLINE_PASS_INCLUDED
#define TOON_OUTLINE_PASS_INCLUDED

// Pass de outline por inverted hull, extrudando vertices ao longo da normal em world space.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct OutlineAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

OutlineVaryings OutlineVertex(OutlineAttributes IN)
{
    OutlineVaryings OUT = (OutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

#ifdef _OUTLINE_ON
    float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
    float dist = length(positionWS - _WorldSpaceCameraPos);
    positionWS += normalWS * _OutlineThickness * 0.005 * dist;
    OUT.positionCS = TransformWorldToHClip(positionWS);
#else
    OUT.positionCS = float4(2, 2, 2, 1);
#endif

    return OUT;
}

half4 OutlineFragment(OutlineVaryings IN) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
    return _OutlineColor;
}

#endif // TOON_OUTLINE_PASS_INCLUDED
