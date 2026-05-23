using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton global (DontDestroyOnLoad).
/// Responsabilidades:
///   - Conhecer o índice de build de cada cena do jogo
///   - Navegar entre cenas com transição padronizada
///   - Registrar progresso e alinhamento via SaveSystem
///   - Sair do jogo
///
/// Transição padronizada (configurável no Inspector):
///   loadingScreenDuration > 0 → Fade in → Loading Screen → Fade out
///   loadingScreenDuration = 0 → Carregamento instantâneo (sem fade)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Índices de build ──────────────────────────────────────────────────────

    [Header("Scene Build Indexes")]
    [Tooltip("Índice da cena de Boot (cena 0).")]
    public int sceneBoot = 0;

    [Tooltip("Índice da cena de Menu principal.")]
    public int sceneMenu = 1;

    [Tooltip("Índice da cena de Devmap (testes).")]
    public int sceneDevmap = 2;

    [Tooltip("Índice da cena da Fase 1.")]
    public int sceneFase1 = 3;

    [Tooltip("Índice da cena da Fase 2.")]
    public int sceneFase2 = 4;

    [Tooltip("Índice da cena da Fase 3.")]
    public int sceneFase3 = 5;

    [Tooltip("Índice da cena da Cutscene Final.")]
    public int sceneCutscene = 6;

    [Header("Transition")]
    [Tooltip("Tempo de exibição da loading screen entre as fades.\n" +
             "0 = carregamento instantâneo (sem fade, útil durante desenvolvimento).")]
    public float loadingScreenDuration = 2f;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Navegação (UnityEvent) ────────────────────────────────────────────────

    public void GoToRoot() => LoadScene(sceneBoot);
    public void GoToMenu() => LoadScene(sceneMenu);
    public void GoToPhase1() => LoadScene(sceneFase1);
    public void GoToPhase2() => LoadScene(sceneFase2);
    public void GoToPhase3() => LoadScene(sceneFase3);
    public void GoToFinal() => LoadScene(sceneCutscene);

    // ── Carregamento de cena ──────────────────────────────────────────────────

    /// <summary>
    /// Carrega uma cena usando a transição definida no Inspector.
    /// Se loadingScreenDuration == 0, carrega instantaneamente.
    /// </summary>
    public void LoadScene(int buildIndex)
    {
        StartCoroutine(LoadSceneRoutine(buildIndex));
    }

    // ── Alinhamento ───────────────────────────────────────────────────────────

    public void AddAlignment(int delta) => SaveSystem.Instance?.AddAlignment(delta);

    public bool IsAlignmentPositive() => (SaveSystem.Instance?.GetAlignment() ?? 0) >= 0;

    // ── Sair do jogo ─────────────────────────────────────────────────────────

    public void QuitGame() => StartCoroutine(QuitRoutine());

    // ── Coroutines internas ───────────────────────────────────────────────────

    private IEnumerator LoadSceneRoutine(int buildIndex)
    {
        if (loadingScreenDuration <= 0f)
        {
            // Instantâneo — sem fade, sem loading screen
            SceneManager.LoadScene(buildIndex);
            yield break;
        }

        // 1. Fade in da black screen
        bool fadeDone = false;
        ScreenManager.Instance?.FadeToBlack(() => fadeDone = true);
        yield return new WaitUntil(() => fadeDone);

        // 2. Ativa o painel de loading (por baixo da black screen)
        ScreenManager.Instance?.ChangeScreen(Screens.Loading);

        // 3. Fade out da black screen, revelando a loading screen
        fadeDone = false;
        ScreenManager.Instance?.FadeFromBlack(() => fadeDone = true);
        yield return new WaitUntil(() => fadeDone);

        // 4. Troca de cena acontece aqui, invisível para o jogador
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
        op.allowSceneActivation = false;

        // Aguarda o tempo da loading screen E a cena estar pronta
        float elapsed = 0f;
        while (elapsed < loadingScreenDuration || op.progress < 0.9f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 5. Fade in da black screen
        fadeDone = false;
        ScreenManager.Instance?.FadeToBlack(() => fadeDone = true);
        yield return new WaitUntil(() => fadeDone);

        // 6. Ativa a cena — OnSceneLoaded do ScreenManager cuida do ChangeScreen
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // Aguarda um frame para OnSceneLoaded e Start()s processarem
        yield return null;

        // 7. Fade out da black screen, revelando o gameplay
        ScreenManager.Instance?.FadeFromBlack();
    }

    private IEnumerator QuitRoutine()
    {
        if (loadingScreenDuration > 0f)
        {
            bool fadeDone = false;
            ScreenManager.Instance?.FadeToBlack(() => fadeDone = true);
            yield return new WaitUntil(() => fadeDone);
        }

        Application.Quit();
    }
}