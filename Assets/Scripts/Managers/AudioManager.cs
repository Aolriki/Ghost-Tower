using UnityEngine;
using UnityEngine.Audio;
using System;

// 1. Dicionário enumerado com os exatos sons da sua lista
public enum SFXType
{
    Passo,
    ColetarItem,
    AbrirDocumento,
    GirarCodeSlot,
    FalhaDestrancar,
    SucessoDestrancar,
    ConcluirFase
}

[Serializable]
public struct SoundEffect
{
    public SFXType type;
    public AudioClip[] clips; // Array para permitir variações
    [Range(0f, 1f)] public float volume;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource; // NOVO: Separado da música
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Mixer (Recomendado)")]
    [SerializeField] private AudioMixer mixer;

    [Header("Biblioteca de Sons")]
    [SerializeField] private SoundEffect[] sfxLibrary;

    [Header("Configurações")]
    [Range(0f, 0.5f)]
    [SerializeField] private float sfxPitchVariance = 0.1f;

    void Awake()
    {
        // Mantido do script original
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Sonoplastia Ambiente & Música ─────────────────────────────────────────

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    // NOVO: Controle dedicado para a "Musica" de ambiente (vento, caverna, etc)
    public void PlayAmbient(AudioClip clip, bool loop = true)
    {
        if (ambientSource == null || clip == null) return;
        if (ambientSource.clip == clip && ambientSource.isPlaying) return;

        ambientSource.clip = clip;
        ambientSource.loop = loop;
        ambientSource.Play();
    }

    // ── Feedback Sonoro (SFX) ─────────────────────────────────────────────────

    // NOVO: Toca pelo Enum, sem precisar referenciar AudioClip em outros scripts
    public void PlaySFX(SFXType type)
    {
        if (sfxSource == null) return;

        SoundEffect? soundEffect = Array.Find(sfxLibrary, s => s.type == type);

        if (soundEffect.HasValue && soundEffect.Value.clips.Length > 0)
        {
            // Escolhe um áudio aleatório caso haja mais de um (ótimo para passos)
            AudioClip clipToPlay = soundEffect.Value.clips[UnityEngine.Random.Range(0, soundEffect.Value.clips.Length)];

            // Randomiza levemente o pitch para não ficar robótico
            sfxSource.pitch = 1f + UnityEngine.Random.Range(-sfxPitchVariance, sfxPitchVariance);

            sfxSource.PlayOneShot(clipToPlay, soundEffect.Value.volume);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Som do tipo {type} não encontrado na biblioteca!");
        }
    }

    // ── Controle de Volume Profissional (Audio Mixer) ──────────────────────────

    // NOVO: Controle de volume via Mixer no lugar do volume direto do AudioSource
    public void SetMasterVolume(float volume) => SetMixerVolume("MasterVolume", volume);
    public void SetMusicVolume(float volume) => SetMixerVolume("MusicVolume", volume);
    public void SetAmbientVolume(float volume) => SetMixerVolume("AmbientVolume", volume);
    public void SetSfxVolume(float volume) => SetMixerVolume("SFXVolume", volume);

    private void SetMixerVolume(string parameterName, float volume)
    {
        if (mixer == null) return;

        // Converte de 0.0001 - 1 (linear) para decibéis (logarítmico)
        float decibels = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(parameterName, decibels);
    }
}