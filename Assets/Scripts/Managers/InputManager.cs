using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton global (DontDestroyOnLoad).
/// Ponto único de troca de Action Map.
///
/// NÃO é mais dono do PlayerInput — cada cena registra o seu
/// próprio via RegisterPlayerInput() no Start do PlayerInput local.
///
/// Cenas de gameplay registram o PlayerInput do PlayerCore.
/// Cenas de Menu/Cutscene registram um PlayerInput minimalista (só UI).
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private const string MAP_PLAYER = "Player";
    private const string MAP_UI = "UI";
    private const string MAP_CODEPROP = "CodeProp";

    private PlayerInput _playerInput;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Registro ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Chamado pelo PlayerInput da cena atual ao nascer.
    /// Substitui qualquer referência anterior.
    /// </summary>
    public void RegisterPlayerInput(PlayerInput input)
    {
        _playerInput = input;
        Debug.Log($"[InputManager] PlayerInput registrado: {input.gameObject.name}");
    }

    /// <summary>
    /// Chamado pelo PlayerInput da cena ao ser destruído (OnDestroy).
    /// Evita que o InputManager fique com referência morta.
    /// </summary>
    public void UnregisterPlayerInput(PlayerInput input)
    {
        if (_playerInput == input)
        {
            _playerInput = null;
            Debug.Log("[InputManager] PlayerInput removido.");
        }
    }

    // ── Troca de Action Map ───────────────────────────────────────────────────

    public void SwitchToPlayer() => Switch(MAP_PLAYER);
    public void SwitchToUI() => Switch(MAP_UI);
    public void SwitchToCodeProp() => Switch(MAP_CODEPROP);

    public string CurrentMap => _playerInput?.currentActionMap?.name;

    private void Switch(string mapName)
    {
        if (_playerInput == null) return;

        _playerInput.SwitchCurrentActionMap(mapName);
        Debug.Log($"[InputManager] Action Map → {mapName}");
    }
}