using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Global singleton (DontDestroyOnLoad).
// Single point of action map switching.
// Does not own the PlayerInput — each scene registers its own via RegisterPlayerInput().
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private const string MAP_PLAYER = "Player";
    private const string MAP_UI = "UI";
    private const string MAP_CODEPROP = "CodeProp";

    private PlayerInput _playerInput;
    private Coroutine _switchCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by the scene PlayerInput on Start.
    public void RegisterPlayerInput(PlayerInput input)
    {
        _playerInput = input;
        Debug.Log($"[InputManager] PlayerInput registered: {input.gameObject.name}");
    }

    // Called by the scene PlayerInput on OnDestroy.
    public void UnregisterPlayerInput(PlayerInput input)
    {
        if (_playerInput != input) return;
        _playerInput = null;
        Debug.Log("[InputManager] PlayerInput unregistered.");
    }

    public void SwitchToPlayer() => Switch(MAP_PLAYER);
    public void SwitchToUI() => Switch(MAP_UI);
    public void SwitchToCodeProp() => Switch(MAP_CODEPROP);

    public string CurrentMap => _playerInput?.currentActionMap?.name;

    private void Switch(string mapName)
    {
        if (_playerInput == null) return;
        if (_switchCoroutine != null) StopCoroutine(_switchCoroutine);
        _switchCoroutine = StartCoroutine(SwitchNextFrame(mapName));
    }

    private IEnumerator SwitchNextFrame(string mapName)
    {
        yield return null;
        if (_playerInput == null) yield break;
        _playerInput.SwitchCurrentActionMap(mapName);
        _switchCoroutine = null;
        Debug.Log($"[InputManager] Action Map -> {mapName}");
    }
}