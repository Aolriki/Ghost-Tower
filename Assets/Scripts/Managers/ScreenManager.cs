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

// Global singleton (DontDestroyOnLoad).
// Manages panels, screen transitions, fade, doc pages and the floor map.
// Rule: this script never references scene-local systems directly.
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
    [SerializeField] private Transform mapContent;

    [Header("Code Prop")]
    [SerializeField] private GameObject codePropPanel;

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
        docPagePanel?.SetActive(false);
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

    // Manages which panels are active and which input map is in use.
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

    // Shortcut methods for UnityEvents.
    public void GoToMainMenu() => ChangeScreen(Screens.MainMenu);
    public void GoToGameplay() => ChangeScreen(Screens.Gameplay);
    public void GoToPause() => ChangeScreen(Screens.Pause);
    public void GoToDialogue() => ChangeScreen(Screens.Dialogue);

    // Reapplies the correct input map after PlayerInputRegister registers the local PlayerInput.
    public void ReapplyCurrentScreen() => ChangeScreen(CurrentScreen);

    public void TogglePause()
    {
        if (CurrentScreen == Screens.Gameplay) ChangeScreen(Screens.Pause);
        else if (CurrentScreen == Screens.Pause) ChangeScreen(Screens.Gameplay);
    }

    // Opens a document page prefab inside the doc content area.
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

    // Instantiates the floor map prefab inside the pause panel map area.
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

    // Destroys the current map instance. Called automatically on scene change.
    public void UnloadMap()
    {
        if (_currentMapInstance != null)
        {
            Destroy(_currentMapInstance);
            _currentMapInstance = null;
        }
    }

    public void FadeToBlack(System.Action onComplete = null) => StartCoroutine(FadeRoutine(1f, onComplete));
    public void FadeFromBlack(System.Action onComplete = null) => StartCoroutine(FadeRoutine(0f, onComplete));

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