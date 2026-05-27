using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enum que define para qual cena o Boot vai redirecionar ao dar Play.
/// Troque no Inspector durante o desenvolvimento.
/// Na build final, use Menu.
/// </summary>
public enum BootDestination
{
    Here,       // Fica na cena atual (modo devmap — não carrega outra cena)
    Menu,
    Fase1,
    Fase2,
    Fase3,
    Cutscene,
}

/// <summary>
/// Vive na cena 0 (Boot / Devmap).
///
/// Com StartDestination = Here:
///   Os globais inicializam normalmente e o jogo roda na própria cena 0.
///   Use isso durante o desenvolvimento — sem precisar trocar de cena.
///
/// Com qualquer outro destino:
///   Carrega a cena correspondente após inicializar os globais.
///   Use Menu na build final.
///
/// Na hora da build, remova os objetos de desenvolvimento da cena 0
/// e troque StartDestination para Menu.
/// </summary>
public class BootLoader : MonoBehaviour
{
    [Header("Destino ao dar Play")]
    [Tooltip("Here = roda na própria cena 0 (desenvolvimento).\nMude para Menu na build final.")]
    public BootDestination startDestination = BootDestination.Here;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[BootLoader] GameManager não encontrado! " +
                           "Verifique se os globais estão na hierarquia da cena Boot.");
            return;
        }

        // Here = fica onde está, globais já inicializaram no Awake de cada um
        if (startDestination == BootDestination.Here)
        {
            Debug.Log("[BootLoader] Modo desenvolvimento — rodando na cena Boot.");

            // Ainda assim avisa o ScreenManager qual tela mostrar
            ScreenManager.Instance?.ChangeScreen(Screens.Gameplay);
            return;
        }

        int targetScene = ResolveDestination();
        Debug.Log($"[BootLoader] Redirecionando para cena {targetScene} ({startDestination})");
        SceneManager.LoadScene(targetScene);
    }

    private int ResolveDestination()
    {
        GameManager gm = GameManager.Instance;

        return startDestination switch
        {
            BootDestination.Menu => gm.sceneMenu,
            BootDestination.Fase1 => gm.sceneFase1,
            BootDestination.Fase2 => gm.sceneFase2,
            BootDestination.Fase3 => gm.sceneFase3,
            BootDestination.Cutscene => gm.sceneCutscene,
            _ => gm.sceneMenu,
        };
    }
}