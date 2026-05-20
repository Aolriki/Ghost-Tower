using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput;

    private const string MAP_PLAYER = "Player";
    private const string MAP_UI = "UI";
    private const string MAP_CODEPROP = "CodeProp";   //NOVO Action Map

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (playerInput == null)
            playerInput = FindAnyObjectByType<PlayerInput>();

        if (playerInput == null)
            Debug.LogWarning("[InputManager] PlayerInput não encontrado.");
    }

    public void SwitchToPlayer() => Switch(MAP_PLAYER);
    public void SwitchToUI() => Switch(MAP_UI);
    public void SwitchToCodeProp() => Switch(MAP_CODEPROP);   //NOVO

    public string CurrentMap => playerInput?.currentActionMap?.name;

    private void Switch(string mapName)
    {
        if (playerInput == null) return;
        playerInput.SwitchCurrentActionMap(mapName);
        Debug.Log($"[InputManager] Action Map → {mapName}");
    }

    public void RegisterPlayerInput(PlayerInput input)
    {
        playerInput = input;
    }
}