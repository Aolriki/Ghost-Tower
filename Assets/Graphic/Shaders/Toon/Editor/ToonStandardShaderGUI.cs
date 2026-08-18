using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Inspector customizado do shader Toon/Standard.
public class ToonStandardShaderGUI : ShaderGUI
{
    // Precisa bater com o Enum declarado em _SurfaceType no shader.
    private enum SurfaceType
    {
        Opaque = 0,
        Metallic = 1,
        Transparent = 2
    }

    private const string KeywordMetallic = "_SURFACE_METALLIC";
    private const string KeywordTransparent = "_SURFACE_TRANSPARENT";

    private MaterialEditor materialEditor;
    private MaterialProperty[] properties;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        properties = props;

        MaterialProperty surfaceTypeProp = FindProperty("_SurfaceType", props);

        EditorGUI.BeginChangeCheck();
        materialEditor.ShaderProperty(surfaceTypeProp, "Surface Type");
        bool surfaceTypeChanged = EditorGUI.EndChangeCheck();

        SurfaceType surfaceType = (SurfaceType)surfaceTypeProp.floatValue;

        EditorGUILayout.Space();
        DrawTextureAndColor("_BaseMap", "Base Map", "_BaseColor");

        if (surfaceType == SurfaceType.Transparent)
        {
            EditorGUILayout.Space();
            DrawProperty("_Opacity", "Opacity");
        }

        if (surfaceType == SurfaceType.Metallic)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Metallic", EditorStyles.boldLabel);
            DrawProperty("_ReflectionIntensity", "Reflection Intensity");
            DrawProperty("_ReflectionSmoothness", "Reflection Smoothness");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shading", EditorStyles.boldLabel);
        DrawProperty("_ShadeColor", "Shade Color");
        DrawProperty("_ShadeBlend", "Shade Blend");
        DrawProperty("_ShadeSoftness", "Shade Softness");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
        if (surfaceType == SurfaceType.Metallic)
        {
            EditorGUILayout.HelpBox("Specular esta sempre ligado no modo Metallic.", MessageType.None);
        }
        else
        {
            DrawProperty("_SpecEnabled", "Enable Specular");
        }
        DrawProperty("_SpecIntensity", "Specular Intensity");
        DrawProperty("_SpecSmoothness", "Specular Smoothness");
        DrawProperty("_SpecSoftness", "Specular Softness");

        EditorGUILayout.Space();
        DrawProperty("_BackLightEnabled", "Enable Back Light");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Emission", EditorStyles.boldLabel);
        DrawProperty("_EmissionOn", "Enable Emission");
        DrawTextureAndColor("_EmissionMap", "Emission Map", "_EmissionColor");

        if (surfaceType != SurfaceType.Transparent)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Alpha Clip", EditorStyles.boldLabel);
            DrawProperty("_AlphaClip", "Enable Alpha Clip");
            DrawProperty("_Cutoff", "Alpha Cutoff");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
        DrawProperty("_Cull", "Render Face");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);
        DrawProperty("_OutlineOn", "Enable Outline");
        DrawProperty("_OutlineColor", "Outline Color");
        DrawProperty("_OutlineThickness", "Outline Thickness");

        if (surfaceTypeChanged)
        {
            foreach (Material target in Array.ConvertAll(editor.targets, item => (Material)item))
            {
                ApplySurfaceType(target, surfaceType);
            }
        }
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        MaterialProperty[] assignedProps = MaterialEditor.GetMaterialProperties(new UnityEngine.Object[] { material });
        MaterialProperty surfaceTypeProp = FindProperty("_SurfaceType", assignedProps, false);
        SurfaceType surfaceType = surfaceTypeProp != null ? (SurfaceType)surfaceTypeProp.floatValue : SurfaceType.Opaque;
        ApplySurfaceType(material, surfaceType);
    }

    private void DrawProperty(string propertyName, string label)
    {
        MaterialProperty property = FindProperty(propertyName, properties, false);
        if (property != null)
        {
            materialEditor.ShaderProperty(property, label);
        }
    }

    private void DrawTextureAndColor(string texturePropertyName, string label, string colorPropertyName)
    {
        MaterialProperty textureProp = FindProperty(texturePropertyName, properties, false);
        MaterialProperty colorProp = FindProperty(colorPropertyName, properties, false);
        if (textureProp != null && colorProp != null)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProp, colorProp);
        }
    }

    private static void ApplySurfaceType(Material material, SurfaceType surfaceType)
    {
        bool isMetallic = surfaceType == SurfaceType.Metallic;
        bool isTransparent = surfaceType == SurfaceType.Transparent;

        SetKeyword(material, KeywordMetallic, isMetallic);
        SetKeyword(material, KeywordTransparent, isTransparent);

        if (isTransparent)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)RenderQueue.Geometry;
        }

        // Transparent nao projeta sombra nem entra nos prepasses de depth,
        // igual ao antigo Toon/Transparent (que nao tinha esses passes).
        material.SetShaderPassEnabled("ShadowCaster", !isTransparent);
        material.SetShaderPassEnabled("DepthOnly", !isTransparent);
        material.SetShaderPassEnabled("DepthNormals", !isTransparent);
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }
}
