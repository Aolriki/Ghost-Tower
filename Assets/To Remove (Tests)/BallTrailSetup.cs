using UnityEngine;
using System;

public class BallTrailSetup : MonoBehaviour
{
    // ── Preset ───────────────────────────────────────────────────────────────
    [Header("Preset")]
    public TrailPreset preset = TrailPreset.Default;

    public enum TrailPreset { Default, Good, Bad, Fast, MidSpeed, Custom }

    // ── Cores (por preset) ───────────────────────────────────────────────────
    [Header("Cores do Rastro")]
    [Tooltip("Cor na cabeça do rastro, logo atrás da bola")]
    public Color headColor = new Color(0.85f, 1f, 1f, 1f);

    [Tooltip("Cor no meio do rastro")]
    public Color bodyColor = new Color(0.2f, 0.85f, 0.95f, 0.75f);

    [Tooltip("Cor na ponta da calda — mantenha alpha 0 para desaparecer")]
    public Color tailColor = new Color(0.1f, 0.6f, 0.85f, 0f);

    // ── Velocidade / Comprimento (global — não muda com o preset) ────────────
    [Header("Velocidade / Comprimento")]
    [Tooltip("Comprimento mínimo do rastro (bola parada ou lenta)")]
    public float minTrailTime = 0.08f;

    [Tooltip("Comprimento máximo do rastro (alta velocidade)")]
    public float maxTrailTime = 0.55f;

    [Tooltip("Velocidade de referência para atingir o comprimento máximo")]
    public float maxSpeedReference = 12f;

    [Tooltip("Suavidade da transição de comprimento")]
    [Range(0.5f, 20f)]
    public float smoothing = 7f;

    // ── Memória de cores por preset ──────────────────────────────────────────
    [Serializable]
    public class PresetData
    {
        public Color head;
        public Color body;
        public Color tail;
        public bool initialized;
    }

    [HideInInspector] public PresetData[] savedPresets = new PresetData[6];
    [HideInInspector] public TrailPreset lastPreset = (TrailPreset)(-1);

    // ─────────────────────────────────────────────────────────────────────────
    void OnValidate()
    {
        EnsureSavedPresets();

        // Salva cores do preset anterior antes de trocar
        if (lastPreset != (TrailPreset)(-1) && lastPreset != TrailPreset.Custom)
        {
            int prev = (int)lastPreset;
            savedPresets[prev].head = headColor;
            savedPresets[prev].body = bodyColor;
            savedPresets[prev].tail = tailColor;
            savedPresets[prev].initialized = true;
        }

        if (preset != lastPreset)
        {
            lastPreset = preset;

            if (preset == TrailPreset.Custom) return;

            int idx = (int)preset;

            if (savedPresets[idx].initialized)
            {
                // Restaura cores salvas para este preset
                headColor = savedPresets[idx].head;
                bodyColor = savedPresets[idx].body;
                tailColor = savedPresets[idx].tail;
            }
            else
            {
                // Primeira vez nesse preset: carrega os padrões de fábrica
                LoadDefaults(preset);
                savedPresets[idx].head = headColor;
                savedPresets[idx].body = bodyColor;
                savedPresets[idx].tail = tailColor;
                savedPresets[idx].initialized = true;
            }
        }
        else if (preset != TrailPreset.Custom)
        {
            // Mesmo preset, usuário editou as cores: salva
            int idx = (int)preset;
            savedPresets[idx].head = headColor;
            savedPresets[idx].body = bodyColor;
            savedPresets[idx].tail = tailColor;
            savedPresets[idx].initialized = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        BallTrail bt = GetComponent<BallTrail>();
        if (bt == null) bt = gameObject.AddComponent<BallTrail>();

        // Injeta cores
        bt.headColor = headColor;
        bt.bodyColor = bodyColor;
        bt.tailColor = tailColor;

        // Injeta configurações globais de velocidade
        bt.minTrailTime = minTrailTime;
        bt.maxTrailTime = maxTrailTime;
        bt.maxSpeedReference = maxSpeedReference;
        bt.smoothing = smoothing;

        TrailRenderer tr = GetComponent<TrailRenderer>();
        if (tr == null) tr = gameObject.AddComponent<TrailRenderer>();

        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.autodestruct = false;

        Debug.Log($"[BallTrailSetup] '{preset}' aplicado em {gameObject.name}");

        Destroy(this);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void EnsureSavedPresets()
    {
        if (savedPresets == null || savedPresets.Length < 6)
            savedPresets = new PresetData[6];

        for (int i = 0; i < 6; i++)
            if (savedPresets[i] == null)
                savedPresets[i] = new PresetData();
    }

    void LoadDefaults(TrailPreset p)
    {
        switch (p)
        {
            case TrailPreset.Default:
                headColor = new Color(0.85f, 1f, 1f, 1f);
                bodyColor = new Color(0.2f, 0.85f, 0.95f, 0.75f);
                tailColor = new Color(0.1f, 0.6f, 0.85f, 0f);
                break;
            case TrailPreset.Good:
                headColor = new Color(0.4f, 1f, 0.5f, 1f);
                bodyColor = new Color(0.1f, 0.8f, 0.2f, 0.75f);
                tailColor = new Color(0.05f, 0.5f, 0.1f, 0f);
                break;
            case TrailPreset.Bad:
                headColor = new Color(1f, 0.25f, 0.2f, 1f);
                bodyColor = new Color(0.85f, 0.1f, 0.05f, 0.8f);
                tailColor = new Color(0.5f, 0.05f, 0f, 0f);
                break;
            case TrailPreset.Fast:
                headColor = new Color(1f, 0.95f, 0.3f, 1f);
                bodyColor = new Color(1f, 0.4f, 0.05f, 0.8f);
                tailColor = new Color(0.6f, 0.1f, 0f, 0f);
                break;
            case TrailPreset.MidSpeed:
                headColor = new Color(0.9f, 0.6f, 1f, 1f);
                bodyColor = new Color(0.6f, 0.2f, 0.9f, 0.7f);
                tailColor = new Color(0.3f, 0.05f, 0.5f, 0f);
                break;
        }
    }
}