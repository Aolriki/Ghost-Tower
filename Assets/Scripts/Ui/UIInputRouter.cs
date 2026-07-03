using UnityEngine;
using UnityEngine.InputSystem;

// Componente local de cena que despacha o map UI para LookMode, dialogo ou fechamento de tela.
// Compativel com PlayerInput no modo Send Messages — os nomes dos metodos batem com os nomes das actions.
public class UIInputRouter : MonoBehaviour
{
    // Recebe Player/Open Pause (Button, performed).
    public void OnOpenPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        ScreenManager.Instance?.TogglePause();
    }

    // Recebe UI/Navigate (Value, Vector2).
    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (LookMode.Instance == null || !LookMode.Instance.IsActive) return;

        if (context.started)
            LookMode.Instance.NavigateStarted(context.ReadValue<Vector2>());
        else if (context.performed)
            LookMode.Instance.NavigateHeld(context.ReadValue<Vector2>());
        else if (context.canceled)
            LookMode.Instance.NavigateReleased();
    }

    // Recebe UI/Confirm (Button, performed).
    public void OnConfirm(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (LookMode.Instance != null && LookMode.Instance.IsActive)
        {
            LookMode.Instance.Confirm();
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive)
            DialogueManager.Instance.Advance();
    }

    // Recebe UI/Exit (Button, performed).
    public void OnExit(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (LookMode.Instance != null && LookMode.Instance.IsActive)
        {
            LookMode.Instance.Cancel();
            return;
        }

        ScreenManager.Instance?.CloseCurrentScreen();
    }
}