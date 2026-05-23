using UnityEngine;

/// <summary>
/// Singleton global (DontDestroyOnLoad).
/// Esqueleto do sistema de áudio. Expande conforme o projeto crescer.
///
/// Uso básico:
///   AudioManager.Instance.PlayMusic(clip);
///   AudioManager.Instance.PlaySFX(clip);
///   AudioManager.Instance.SetMusicVolume(0.8f);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Default Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultSfxVolume = 1f;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource != null) musicSource.volume = defaultMusicVolume;
        if (sfxSource   != null) sfxSource.volume   = defaultSfxVolume;
    }

    // ── Music ─────────────────────────────────────────────────────────────────

    /// <summary>Toca uma música em loop. Ignora se já é a mesma.</summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource?.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(volume);
    }

    // ── SFX ───────────────────────────────────────────────────────────────────

    /// <summary>Toca um SFX one-shot (não interrompe outros sons).</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void SetSfxVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp01(volume);
    }
}
