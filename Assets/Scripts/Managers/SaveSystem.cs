using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton global (DontDestroyOnLoad).
/// Salva e carrega dois blocos de dados via PlayerPrefs:
///   - Progresso de fase (int)
///   - Conquistas desbloqueadas (lista de IDs string)
///
/// Conquistas são usadas pelo painel de Extras no Menu para saber
/// quais artes exibir. Props chamam UnlockAchievement() direto no
/// seu UnityEvent OnSolved.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    // Chaves do PlayerPrefs
    private const string KEY_PROGRESS    = "save_progress";
    private const string KEY_ALIGNMENT   = "save_alignment";
    private const string KEY_ACHIEVEMENTS = "save_achievements";

    // Cache em memória (carregado no Awake)
    private int _currentProgress;
    private int _alignment;
    private HashSet<string> _achievements = new HashSet<string>();

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ── Progresso de fase ─────────────────────────────────────────────────────

    /// <summary>
    /// Retorna o índice da cena/fase que o botão "Continuar" deve carregar.
    /// 0 = nunca jogou (vai para o início).
    /// </summary>
    public int GetProgress() => _currentProgress;

    /// <summary>
    /// Avança o progresso para a fase indicada, se for maior que o atual.
    /// Chame ao fim de cada fase.
    /// </summary>
    public void SetProgress(int fase)
    {
        if (fase <= _currentProgress) return;
        _currentProgress = fase;
        Save();
        Debug.Log($"[SaveSystem] Progresso salvo → fase {fase}");
    }

    // ── Alinhamento ───────────────────────────────────────────────────────────

    /// <summary>
    /// Valor acumulado de escolhas do jogador.
    /// Positivo = alinhamento A, negativo = alinhamento B.
    /// O GameManager lê isso para decidir qual final exibir.
    /// </summary>
    public int GetAlignment() => _alignment;

    public void AddAlignment(int delta)
    {
        _alignment += delta;
        Save();
    }

    // ── Conquistas / Extras ───────────────────────────────────────────────────

    /// <summary>
    /// Desbloqueia uma conquista pelo ID. Chamado por props via UnityEvent.
    /// Exemplo de ID: "fase1_cofre_resolvido", "fase2_npc_cecilia_final"
    /// </summary>
    public void UnlockAchievement(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (_achievements.Contains(id)) return;

        _achievements.Add(id);
        Save();
        Debug.Log($"[SaveSystem] Conquista desbloqueada → {id}");
    }

    /// <summary>
    /// Retorna true se a conquista com esse ID já foi desbloqueada.
    /// Usado pelo painel de Extras para exibir ou ocultar cada arte.
    /// </summary>
    public bool IsUnlocked(string id) => _achievements.Contains(id);

    // ── Reset (útil para testes no Editor) ───────────────────────────────────

    [ContextMenu("Apagar Save")]
    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(KEY_PROGRESS);
        PlayerPrefs.DeleteKey(KEY_ALIGNMENT);
        PlayerPrefs.DeleteKey(KEY_ACHIEVEMENTS);
        PlayerPrefs.Save();

        _currentProgress = 0;
        _alignment = 0;
        _achievements.Clear();

        Debug.Log("[SaveSystem] Save apagado.");
    }

    // ── Serialização interna ──────────────────────────────────────────────────

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_PROGRESS,  _currentProgress);
        PlayerPrefs.SetInt(KEY_ALIGNMENT, _alignment);
        PlayerPrefs.SetString(KEY_ACHIEVEMENTS, SerializeAchievements());
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _currentProgress = PlayerPrefs.GetInt(KEY_PROGRESS,  0);
        _alignment       = PlayerPrefs.GetInt(KEY_ALIGNMENT, 0);
        DeserializeAchievements(PlayerPrefs.GetString(KEY_ACHIEVEMENTS, ""));

        Debug.Log($"[SaveSystem] Save carregado → fase {_currentProgress}, " +
                  $"alinhamento {_alignment}, conquistas {_achievements.Count}");
    }

    // Conquistas são serializadas como IDs separados por '|'
    private string SerializeAchievements()
    {
        return string.Join("|", _achievements);
    }

    private void DeserializeAchievements(string raw)
    {
        _achievements.Clear();
        if (string.IsNullOrEmpty(raw)) return;

        foreach (string id in raw.Split('|'))
            if (!string.IsNullOrEmpty(id))
                _achievements.Add(id);
    }
}
