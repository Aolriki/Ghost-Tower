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
    ConcluirFase,
    Vento
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

    public void PlaySFX(SFXType type, int clipIndex = -1)
    {
        if (sfxSource == null) return;

        // O jeito mais seguro de buscar struct em array é procurando a posição dele
        int index = Array.FindIndex(sfxLibrary, s => s.type == type);

        // Se o index for diferente de -1, significa que o som existe na lista
        if (index != -1)
        {
            // Agora pegamos o struct diretamente pela posição na lista
            SoundEffect soundEffect = sfxLibrary[index];

            if (soundEffect.clips != null && soundEffect.clips.Length > 0)
            {
                AudioClip clipToPlay;

                // Se o índice for válido (0, 1, 2...), toca exatamente ele
                if (clipIndex >= 0 && clipIndex < soundEffect.clips.Length)
                {
                    clipToPlay = soundEffect.clips[clipIndex];
                }
                // Se for -1 (ou um número inválido), sorteia aleatoriamente
                else
                {
                    int randomIndex = UnityEngine.Random.Range(0, soundEffect.clips.Length);
                    clipToPlay = soundEffect.clips[randomIndex];
                }

                sfxSource.PlayOneShot(clipToPlay); // Exemplo de como estava antes
            }
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