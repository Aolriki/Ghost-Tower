using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Screens
{
    None,
    MainMenu,
    Gameplay,
    DocPage,
    Pause,
    Loading,
    Dialogue,
    CodeProp,
}

/// <summary>
/// Singleton global (DontDestroyOnLoad).
/// Responsabilidades:
///   - Gerenciar painéis e transições de tela
///   - Executar fades de blackscreen
///   - Abrir e fechar o painel de documento
///
/// Regra: este script NUNCA referencia sistemas de cena
/// (PlayerCore, InventoryManager, etc.).
/// Navegação entre cenas e QuitGame pertencem ao GameManager.
/// </summary>
public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance { get; private set; }

    [SerializeField] private Screens _currentScreen = Screens.None;
    public Screens CurrentScreen
    {
        get => _currentScreen;
        private set => _currentScreen = value;
    }

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Gameplay")]
    [SerializeField] private GameObject hudPanel;

    [Header("Doc Page")]
    [SerializeField] private GameObject docPagePanel;
    [SerializeField] private Transform docContent;

    [Header("Pause")]
    [SerializeField] private GameObject pausePanel;

    [Header("Code Prop")]
    [SerializeField] private GameObject codePropPanel;

    [Header("Black Screen")]
    [SerializeField] private CanvasGroup blackScreenPanel;
    [SerializeField] private float blackScreenFadeDuration = 1f;

    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;

    private GameObject _currentDocPage;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        blackScreenPanel?.gameObject.SetActive(false);
        loadingPanel?.SetActive(false);
        docPagePanel?.SetActive(false);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager.Instance == null) return;

        GameManager gm = GameManager.Instance;
        int idx = scene.buildIndex;

        if (idx == gm.sceneMenu)
            ChangeScreen(Screens.MainMenu);
        else if (idx == gm.sceneBoot)
        { /* Boot não precisa de tela */ }
        else
            ChangeScreen(Screens.Gameplay);
    }

    // ── Painéis ───────────────────────────────────────────────────────────────

    public void ChangeScreen(Screens screen)
    {
        CurrentScreen = screen;
        DeactivateAllPanels();

        switch (screen)
        {
            case Screens.MainMenu:
                mainMenuPanel?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 1f;
                break;

            case Screens.Gameplay:
                hudPanel?.SetActive(true);
                InputManager.Instance?.SwitchToPlayer();
                Time.timeScale = 1f;
                break;

            case Screens.DocPage:
                hudPanel?.SetActive(true);
                docPagePanel?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 0f;
                break;

            case Screens.Pause:
                hudPanel?.SetActive(true);
                pausePanel?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 0f;
                break;

            case Screens.Loading:
                loadingPanel?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                break;

            case Screens.Dialogue:
                hudPanel?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 1f;
                break;

            case Screens.CodeProp:
                codePropPanel?.SetActive(true);
                InputManager.Instance?.SwitchToCodeProp();
                Time.timeScale = 1f;
                break;
        }
    }

    // ── Atalhos para UnityEvent ───────────────────────────────────────────────

    public void GoToMainMenu() => ChangeScreen(Screens.MainMenu);
    public void GoToGameplay() => ChangeScreen(Screens.Gameplay);
    public void GoToPause() => ChangeScreen(Screens.Pause);
    public void GoToDialogue() => ChangeScreen(Screens.Dialogue);

    /// <summary>
    /// Re-aplica o Action Map do InputManager para a tela atual.
    /// Chamado pelo SceneInputRegistrar após registrar o PlayerInput,
    /// garantindo que a troca de mapa aconteça com o PlayerInput já disponível.
    /// </summary>
    public void ReapplyCurrentScreen() => ChangeScreen(CurrentScreen);

    public void TogglePause()
    {
        if (CurrentScreen == Screens.Gameplay) ChangeScreen(Screens.Pause);
        else if (CurrentScreen == Screens.Pause) ChangeScreen(Screens.Gameplay);
    }

    // ── Doc Page ──────────────────────────────────────────────────────────────

    public void OpenDoc(GameObject docPagePrefab)
    {
        if (docContent == null || docPagePrefab == null) return;

        if (_currentDocPage != null)
            Destroy(_currentDocPage);

        _currentDocPage = Instantiate(docPagePrefab, docContent);
        ChangeScreen(Screens.DocPage);
    }

    public void CloseDoc()
    {
        if (_currentDocPage != null)
        {
            Destroy(_currentDocPage);
            _currentDocPage = null;
        }

        ChangeScreen(Screens.Gameplay);
    }

    // ── Fade (chamado pelo GameManager) ───────────────────────────────────────

    public void FadeToBlack(System.Action onComplete = null) => StartCoroutine(FadeRoutine(1f, onComplete));
    public void FadeFromBlack(System.Action onComplete = null) => StartCoroutine(FadeRoutine(0f, onComplete));

    // ── Internal ──────────────────────────────────────────────────────────────

    private void DeactivateAllPanels()
    {
        mainMenuPanel?.SetActive(false);
        hudPanel?.SetActive(false);
        docPagePanel?.SetActive(false);
        pausePanel?.SetActive(false);
        codePropPanel?.SetActive(false);
        loadingPanel?.SetActive(false);
    }

    private IEnumerator FadeRoutine(float target, System.Action onComplete)
    {
        if (blackScreenPanel == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float start = target == 1f ? 0f : 1f;

        blackScreenPanel.alpha = start;
        blackScreenPanel.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < blackScreenFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackScreenPanel.alpha = Mathf.Lerp(start, target, elapsed / blackScreenFadeDuration);
            yield return null;
        }

        blackScreenPanel.alpha = target;

        if (target == 0f)
            blackScreenPanel.gameObject.SetActive(false);

        onComplete?.Invoke();
    }
}