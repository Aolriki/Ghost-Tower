using UnityEngine;
using UnityEngine.InputSystem;

// Registra o PlayerInput local no InputManager


[RequireComponent(typeof(PlayerInput))]
public class PlayerInputRegister : MonoBehaviour
{
    private PlayerInput _playerInput;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        // Start garante que o InputManager global já acordou (seu Awake rodou)
        if (InputManager.Instance == null)
        {
            Debug.LogWarning("[SceneInputRegistrar] InputManager.Instance não encontrado. " +
                             "Verifique se a cena Boot foi carregada primeiro.");
            return;
        }

        InputManager.Instance.RegisterPlayerInput(_playerInput);

        // Re-aplica o Action Map correto para a tela atual, já que o
        // OnSceneLoaded do ScreenManager dispara antes deste Start().
        if (ScreenManager.Instance != null)
            ScreenManager.Instance.ReapplyCurrentScreen();
    }

    void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.UnregisterPlayerInput(_playerInput);
    }
}