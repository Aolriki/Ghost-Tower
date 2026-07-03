// Shader toon transparente com controle de opacidade e outline.
Shader "Toon/Transparent"
{
    Properties
    {
        [Header(Base)] [Space(5)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Transparency)] [Space(5)]
        _Opacity("Opacity", Range(0, 1)) = 1

        [Header(Shading)] [Space(5)]
        _ShadeColor("Shade Color", Color) = (0.3, 0.2, 0.45, 1)
        _ShadeBlend("Shade Blend", Range(0, 1)) = 0.7
        _ShadeSoftness("Shade Softness", Range(0, 1)) = 0.1

        [Header(Specular)] [Space(5)]
        [ToggleUI] _SpecEnabled("Enable Specular", Float) = 0
        _SpecIntensity("Specular Intensity", Range(0, 5)) = 1
        _SpecSmoothness("Specular Smoothness", Range(0, 1)) = 0.5
        _SpecSoftness("Specular Softness", Range(0, 1)) = 0.05

        [Header(Back Light)] [Space(5)]
        [ToggleUI] _BackLightEnabled("Enable Back Light", Float) = 0

        [Header(Emission)] [Space(5)]
        [Toggle(_EMISSION_ON)] _EmissionOn("Enable Emission", Float) = 0
        _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)

        [Header(Rendering)] [Space(5)]
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull("Render Face", Float) = 2

        [Header(Outline)] [Space(5)]
        [Toggle(_OUTLINE_ON)] _OutlineOn("Enable Outline", Float) = 0
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness("Outline Thickness", Range(0, 10)) = 1

        // Propriedades internas necessarias para compatibilidade de CBUFFER
        [HideInInspector] _CameraFade("Camera Fade", Float) = 0
        [HideInInspector] _Cutoff("Alpha Cutoff", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"       = "Transparent"
            "RenderPipeline"   = "UniversalPipeline"
            "Queue"            = "Transparent"
            "IgnoreProjector"  = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _LIGHT_LAYERS

            #pragma shader_feature_local _EMISSION_ON

            #pragma multi_compile_instancing

            #define _TRANSPARENT_ON
            #include "Toon_Opaque_Input.hlsl"
            #include "Toon_ForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile_instancing

            #include "Toon_Opaque_Input.hlsl"
            #include "Toon_OutlinePass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
