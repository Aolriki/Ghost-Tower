using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Pause")]
    [SerializeField] private GameObject pausePanel;

    [Header("Code Prop")]
    [SerializeField] private GameObject codePropPanel;

    [Header("Black Screen")]
    [SerializeField] private CanvasGroup blackScreenPanel;
    [SerializeField] private float blackScreenFadeDuration = 1f;

    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        blackScreenPanel?.gameObject.SetActive(false);
        loadingPanel?.SetActive(false);
    }

    void Start()
    {
        int scene = SceneManager.GetActiveScene().buildIndex;
        //ChangeScreen(scene == 0 ? Screens.MainMenu : Screens.Gameplay);  Importante descomentar quando for buildar o jogo.
        ChangeScreen(Screens.Gameplay);
    }

    // ?? Public ????????????????????????????????????????????????????????????????

    public void ChangeScreen(Screens screen)
    {
        Screens previous = CurrentScreen;
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
                hudPanel?.SetActive(true);   // HUD continua visível por baixo
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
                hudPanel?.SetActive(true);   // HUD de gameplay continua visível
                InputManager.Instance?.SwitchToUI();
                Time.timeScale = 1f;         // jogo não pausa durante diálogo
                break;

            case Screens.CodeProp:
                // HUD fica oculto — o CodePropController gerencia o painel próprio
                codePropPanel?.SetActive(true);
                InputManager.Instance?.SwitchToCodeProp();
                Time.timeScale = 1f;   // roda em tempo real para animações do prop
                break;
        }
    }

    // Atalhos convenientes para botões de UI via UnityEvent
    public void GoToMainMenu() => ChangeScreen(Screens.MainMenu);
    public void GoToGameplay() => ChangeScreen(Screens.Gameplay);
    public void GoToPause() => ChangeScreen(Screens.Pause);
    public void GoToDialogue() => ChangeScreen(Screens.Dialogue);

    public void TogglePause()
    {
        if (CurrentScreen == Screens.Gameplay) ChangeScreen(Screens.Pause);
        else if (CurrentScreen == Screens.Pause) ChangeScreen(Screens.Gameplay);
    }

    public void QuitGame()
    {
        StartCoroutine(QuitDelayed());
    }

    public void LoadScene(int buildIndex)
    {
        StartCoroutine(LoadSceneRoutine(buildIndex));
    }

    // ?? Black Screen ??????????????????????????????????????????????????????????

    public void FadeToBlack(System.Action onComplete = null) => StartCoroutine(FadeRoutine(1f, onComplete));
    public void FadeFromBlack(System.Action onComplete = null) => StartCoroutine(FadeRoutine(0f, onComplete));

    // ?? Internal ??????????????????????????????????????????????????????????????

    private void DeactivateAllPanels()
    {
        mainMenuPanel?.SetActive(false);
        hudPanel?.SetActive(false);
        docPagePanel?.SetActive(false);
        pausePanel?.SetActive(false);
        codePropPanel?.SetActive(false);
    }

    private IEnumerator LoadSceneRoutine(int buildIndex)
    {
        ChangeScreen(Screens.Loading);
        FadeToBlack();
        yield return new WaitForSecondsRealtime(1f);

        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
        while (!op.isDone) yield return null;

        FadeFromBlack();
        yield return new WaitForSecondsRealtime(blackScreenFadeDuration);
        ChangeScreen(buildIndex == 0 ? Screens.MainMenu : Screens.Gameplay);
    }

    private IEnumerator FadeRoutine(float target, System.Action onComplete)
    {
        if (blackScreenPanel == null) yield break;
        blackScreenPanel.gameObject.SetActive(true);

        float start = blackScreenPanel.alpha;
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

    private IEnumerator QuitDelayed()
    {
        FadeToBlack();
        yield return new WaitForSecondsRealtime(blackScreenFadeDuration + 0.5f);
        Application.Quit();
    }
}