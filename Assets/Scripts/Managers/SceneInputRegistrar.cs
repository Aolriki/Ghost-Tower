using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Coloque este componente no mesmo GameObject que o PlayerInput da cena.
///
/// Ele faz uma única coisa: ao nascer, registra o PlayerInput local
/// no InputManager global. Ao morrer, remove o registro.
///
/// Isso desacopla o InputManager do PlayerInput — o InputManager global
/// não precisa saber nada sobre a cena atual. Cada cena cuida do seu
/// próprio registro.
///
/// Uso:
///   - Cenas de Gameplay: adicione ao GameObject do PlayerCore (junto com PlayerInput)
///   - Menu / Cutscene: adicione ao GameObject do PlayerInput minimalista da cena
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class SceneInputRegistrar : MonoBehaviour
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