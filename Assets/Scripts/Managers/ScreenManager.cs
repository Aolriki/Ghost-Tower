using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Screens
{
    None,
    MainMenu,
    Gameplay,
    UIMode,
    Pause,
    Loading,
    Dialogue,
    Look,
}

// Singleton global (DontDestroyOnLoad) que gerencia paineis, transicoes de tela, fade, doc pages e o mapa.
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

    [Header("Config")]
    [SerializeField] private GameObject configPanel;

    [Header("Gameplay")]
    [SerializeField] private GameObject hudPanel;
    [Tooltip("Botao de fechar/sair, visivel em todas as telas exceto Gameplay e MainMenu.")]
    [SerializeField] private GameObject exitGroup;

    [Header("UI Mode")]
    [SerializeField] private GameObject uiModePanel;
    [SerializeField] private Transform docContent;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialoguePanel;

    [Header("Pause")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Transform mapContent;

    [Header("Black Screen")]
    [SerializeField] private CanvasGroup blackScreenPanel;
    [SerializeField] private float blackScreenFadeDuration = 1f;

    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;

    private GameObject _currentDocPage;
    private GameObject _currentMapInstance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        blackScreenPanel?.gameObject.SetActive(false);
        loadingPanel?.SetActive(false);
        uiModePanel?.SetActive(false);
        configPanel?.SetActive(false);
        exitGroup?.SetActive(false);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager.Instance == null) return;

        UnloadMap();

        GameManager gm = GameManager.Instance;
        int idx = scene.buildIndex;

        if (idx == gm.sceneMenu)
            ChangeScreen(Screens.MainMenu);
        else if (idx == gm.sceneBoot)
        { /* Boot does not need a screen. */ }
        else
            ChangeScreen(Screens.Gameplay);
    }

    // ── Screen control ────────────────────────────────────────────────────────

    public void ChangeScreen(Screens screen)
    {
        CurrentScreen = screen;
        DeactivateAllPanels();

        switch (screen)
        {
            case Screens.MainMenu:
                mainMenuPanel?.SetActive(true);
                exitGroup?.SetActive(false);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 1f;
                break;

            case Screens.Gameplay:
                hudPanel?.SetActive(true);
                exitGroup?.SetActive(false);
                InputManager.Instance?.SwitchToPlayer();
                Time.timeScale = 1f;
                break;

            case Screens.UIMode:
                uiModePanel?.SetActive(true);
                exitGroup?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 0f;
                break;

            case Screens.Pause:
                pausePanel?.SetActive(true);
                exitGroup?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 0f;
                break;

            case Screens.Loading:
                loadingPanel?.SetActive(true);
                exitGroup?.SetActive(false);
                InputManager.Instance?.SwitchToUI();
                break;

            case Screens.Dialogue:
                dialoguePanel?.SetActive(true);
                exitGroup?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 1f;
                break;

            case Screens.Look:
                hudPanel?.SetActive(true);
                exitGroup?.SetActive(true);
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 1f;
                break;
        }
    }

    // Closes the current screen and returns to gameplay.
    // Use this as the single close/back action for all UI panels.
    public void CloseCurrentScreen()
    {
        if (configPanel != null && configPanel.activeSelf)
        {
            CloseConfig();
            return;
        }

        switch (CurrentScreen)
        {
            case Screens.Pause:
                ChangeScreen(Screens.Gameplay);
                break;

            case Screens.UIMode:
                CloseDoc();
                break;

            case Screens.Dialogue:
                DialogueManager.Instance?.EndDialogue();
                ChangeScreen(Screens.Gameplay);
                break;
        }
    }

    // ── Navigation shortcuts for UnityEvents ──────────────────────────────────

    public void GoToMainMenu() => ChangeScreen(Screens.MainMenu);
    public void GoToGameplay() => ChangeScreen(Screens.Gameplay);
    public void GoToPause() => ChangeScreen(Screens.Pause);
    public void GoToDialogue() => ChangeScreen(Screens.Dialogue);

    public void ReapplyCurrentScreen() => ChangeScreen(CurrentScreen);

    public void TogglePause()
    {
        if (CurrentScreen == Screens.Gameplay) ChangeScreen(Screens.Pause);
        else if (CurrentScreen == Screens.Pause) ChangeScreen(Screens.Gameplay);
    }

    // ── Config Panel ──────────────────────────────────────────────────────────

    // Config is a sub panel of the Main Menu screen, it does not change CurrentScreen.
    public void OpenConfig()
    {
        configPanel?.SetActive(true);
        exitGroup?.SetActive(true);
    }

    public void CloseConfig()
    {
        configPanel?.SetActive(false);
        exitGroup?.SetActive(false);
    }

    // ── Doc Page ──────────────────────────────────────────────────────────────

    // Abre um doc a partir de um prefab direto.
    public void OpenDoc(GameObject docPagePrefab)
    {
        if (docContent == null || docPagePrefab == null) return;

        if (_currentDocPage != null)
            Destroy(_currentDocPage);

        _currentDocPage = Instantiate(docPagePrefab, docContent);
        ChangeScreen(Screens.UIMode);
    }

    // Abre um doc a partir de um ItemSO do tipo Doc.
    // Abre um doc a partir de um DocSO.
    public static void OpenDocItem(DocSO item)
    {
        if (item == null) return;
        if (item.docPagePrefab == null)
        {
            Debug.LogWarning($"[ScreenManager] {item.itemName}: docPagePrefab nao atribuido no DocSO.");
            return;
        }

        Instance?.OpenDoc(item.docPagePrefab);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.AbrirDocumento);
        }
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

    // ── Map ───────────────────────────────────────────────────────────────────

    public GameObject LoadMap(GameObject mapPrefab)
    {
        if (mapContent == null)
        {
            Debug.LogWarning("[ScreenManager] mapContent is not assigned.");
            return null;
        }

        if (mapPrefab == null) return null;

        if (_currentMapInstance != null)
            Destroy(_currentMapInstance);

        _currentMapInstance = Instantiate(mapPrefab, mapContent);
        return _currentMapInstance;
    }

    public void UnloadMap()
    {
        if (_currentMapInstance != null)
        {
            Destroy(_currentMapInstance);
            _currentMapInstance = null;
        }
    }

    // ── Fade ──────────────────────────────────────────────────────────────────

    public void FadeToBlack(System.Action onComplete = null) => StartCoroutine(FadeRoutine(1f, onComplete));
    public void FadeFromBlack(System.Action onComplete = null) => StartCoroutine(FadeRoutine(0f, onComplete));

    // ── Internal ──────────────────────────────────────────────────────────────

    private void DeactivateAllPanels()
    {
        mainMenuPanel?.SetActive(false);
        configPanel?.SetActive(false);
        hudPanel?.SetActive(false);
        uiModePanel?.SetActive(false);
        pausePanel?.SetActive(false);
        loadingPanel?.SetActive(false);
        dialoguePanel?.SetActive(false);
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
