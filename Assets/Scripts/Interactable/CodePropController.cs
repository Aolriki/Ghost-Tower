using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controlador principal do CodeProp.
/// Singleton. Gerencia:
///   • Abertura/fechamento do painel (via ScreenManager + CodePropInteractionManager)
///   • Navegação entre slots (horizontal) e mudança de valor (vertical)
///   • Confirmação e saída via Input
///   • HUD: TextMeshPro por slot, indicador de slot ativo (cursor piscante),
///     legenda de comandos e feedback de estado (Wrong/Correct/Solved)
/// </summary>
public class CodePropController : MonoBehaviour
{
    public static CodePropController Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("HUD — Slot Display")]
    [Tooltip("Array de TextMeshPro, um por slot (máximo 4). " +
             "Cada TMP mostra o valor numérico atual do slot (1-based para o jogador).")]
    public TextMeshProUGUI[] slotLabels;

    [Header("HUD — Cursor (indicador de slot ativo)")]
    [Tooltip("Array de CanvasGroups, um por slot, para piscar o indicador do slot selecionado.")]
    public CanvasGroup[] slotCursors;

    [Tooltip("Velocidade de piscada do cursor (alpha vai de 0 a 1 nessa frequência).")]
    public float cursorBlinkSpeed = 3f;

    [Header("HUD — Feedback de Estado")]
    [Tooltip("Painel que aparece ao errar (WrongCode). Pode ter um ícone de 'X' ou texto.")]
    public GameObject wrongFeedbackPanel;

    [Tooltip("Painel que aparece ao acertar (CorrectCode / Solved).")]
    public GameObject correctFeedbackPanel;

    [Tooltip("Tempo em segundos que os painéis de feedback ficam visíveis.")]
    public float feedbackDuration = 1.2f;

    [Header("HUD — Legenda de Comandos")]
    [Tooltip("GameObject com a legenda dos controles. Fica sempre visível enquanto o prop está aberto.")]
    public GameObject commandsLegend;

    [Header("Navigation Settings")]
    [Tooltip("Tempo mínimo (segundos) entre inputs repetidos ao segurar o botão.")]
    public float inputRepeatDelay = 0.15f;

    // ── Private ───────────────────────────────────────────────────────────────

    private CodeSlot  _currentProp;
    private bool      _isOpen;
    private int       _selectedSlot;

    // Input hold repeat
    private float _holdTimer;
    private Vector2 _lastNavInput;

    // Feedback
    private Coroutine _feedbackCoroutine;

    // Cursor blink
    private float _blinkPhase;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        HideFeedback();
        if (commandsLegend != null) commandsLegend.SetActive(false);
    }

    void Update()
    {
        if (!_isOpen) return;
        UpdateCursorBlink();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Abre o prop. Chamado pelo CodeSlot.Interact().</summary>
    public void Open(CodeSlot prop)
    {
        if (_isOpen) return;

        _currentProp  = prop;
        _isOpen       = true;
        _selectedSlot = 0;

        // Transição de cena
        PlayerCore.Instance?.SetMovementEnabled(false);
        PlayerCore.Instance?.SetInteractionEnabled(false);
        InteractionUIManager.Instance?.Hide(prop.transform);
        CodePropInteractionManager.Instance?.EnterPropView();
        ScreenManager.Instance?.ChangeScreen(Screens.CodeProp);

        BuildHUD();
        RefreshAllLabels();
        UpdateCursorVisibility();
        if (commandsLegend != null) commandsLegend.SetActive(true);
    }

    /// <summary>Fecha o prop e restaura o estado de Gameplay.</summary>
    public void Close()
    {
        if (!_isOpen) return;
        _isOpen      = false;
        _currentProp = null;

        if (commandsLegend != null) commandsLegend.SetActive(false);
        HideFeedback();
        HideAllCursors();

        CodePropInteractionManager.Instance?.ExitPropView();
        ScreenManager.Instance?.ChangeScreen(Screens.Gameplay);
        PlayerCore.Instance?.SetMovementEnabled(true);
        PlayerCore.Instance?.SetInteractionEnabled(true);
    }

    // ── Input Callbacks (Action Map: CodeProp — Invoke Unity Events) ──────────
    // Registre estes métodos nos eventos do PlayerInput no Inspector.

    /// <summary>
    /// Navigate (Horizontal): muda de slot.
    /// Navigate (Vertical):   muda o valor do slot atual.
    /// Bind: D-Pad, WASD.
    /// </summary>
    public void OnCodeNavigate(InputAction.CallbackContext context)
    {
        if (!_isOpen) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (context.started)
        {
            _lastNavInput = input;
            _holdTimer    = 0f;
            ApplyNavigation(input);
        }
        else if (context.performed)
        {
            // Segurar o botão repete o input
            _lastNavInput = input;
            _holdTimer   += Time.deltaTime;
            if (_holdTimer >= inputRepeatDelay)
            {
                _holdTimer = 0f;
                ApplyNavigation(input);
            }
        }
        else if (context.canceled)
        {
            _lastNavInput = Vector2.zero;
            _holdTimer    = 0f;
        }
    }

    /// <summary>
    /// Confirma a combinação atual.
    /// Bind: South Button (gamepad), Space (keyboard).
    /// </summary>
    public void OnCodeConfirm(InputAction.CallbackContext context)
    {
        if (!_isOpen || !context.performed) return;
        Submit();
    }

    /// <summary>
    /// Fecha o prop e volta ao Gameplay.
    /// Bind: East Button (gamepad), Escape (keyboard).
    /// </summary>
    public void OnCodeExit(InputAction.CallbackContext context)
    {
        if (!_isOpen || !context.performed) return;
        Close();
    }

    // ── Navigation Logic ──────────────────────────────────────────────────────

    private void ApplyNavigation(Vector2 input)
    {
        if (_currentProp == null) return;

        // Horizontal → troca de slot
        if (Mathf.Abs(input.x) > 0.5f)
        {
            int dir = input.x > 0f ? 1 : -1;
            _selectedSlot = Mathf.Clamp(_selectedSlot + dir, 0, _currentProp.slotCount - 1);
            UpdateCursorVisibility();
            return;
        }

        // Vertical → muda valor do slot selecionado
        if (Mathf.Abs(input.y) > 0.5f)
        {
            int dir         = input.y > 0f ? 1 : -1;
            int current     = _currentProp.CurrentValues[_selectedSlot];
            int next        = (current + dir + _currentProp.statesPerSlot) % _currentProp.statesPerSlot;

            _currentProp.SetSlotValue(_selectedSlot, next);
            _currentProp.OnSlotVisualUpdate(_selectedSlot, next);
            RefreshLabel(_selectedSlot);
        }
    }

    private void Submit()
    {
        if (_currentProp == null) return;

        _currentProp.TrySubmit();

        CodeSlot.CodeSlotState state = _currentProp.State;

        if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);

        switch (state)
        {
            case CodeSlot.CodeSlotState.WrongCode:
                _feedbackCoroutine = StartCoroutine(ShowFeedback(wrong: true, closeAfter: false));
                break;

            case CodeSlot.CodeSlotState.CorrectCode:
                _feedbackCoroutine = StartCoroutine(ShowFeedback(wrong: false, closeAfter: false));
                break;

            case CodeSlot.CodeSlotState.Solved:
                _feedbackCoroutine = StartCoroutine(ShowFeedback(wrong: false, closeAfter: true));
                break;
        }
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void BuildHUD()
    {
        if (_currentProp == null) return;

        for (int i = 0; i < slotLabels.Length; i++)
        {
            if (slotLabels[i] == null) continue;
            // Mostra só os slots que existem no prop atual
            slotLabels[i].gameObject.SetActive(i < _currentProp.slotCount);
        }

        for (int i = 0; i < slotCursors.Length; i++)
        {
            if (slotCursors[i] == null) continue;
            slotCursors[i].gameObject.SetActive(i < _currentProp.slotCount);
        }
    }

    private void RefreshAllLabels()
    {
        if (_currentProp == null) return;
        for (int i = 0; i < _currentProp.slotCount; i++)
            RefreshLabel(i);
    }

    private void RefreshLabel(int slotIndex)
    {
        if (slotIndex >= slotLabels.Length || slotLabels[slotIndex] == null) return;
        // Exibe valor 1-based para o jogador (internamente 0-based)
        slotLabels[slotIndex].text = (_currentProp.CurrentValues[slotIndex] + 1).ToString();
    }

    // ── Cursor Blink ──────────────────────────────────────────────────────────

    private void UpdateCursorBlink()
    {
        _blinkPhase += Time.deltaTime * cursorBlinkSpeed;
        float alpha = (Mathf.Sin(_blinkPhase * Mathf.PI * 2f) + 1f) * 0.5f;

        if (_currentProp == null) return;

        for (int i = 0; i < _currentProp.slotCount; i++)
        {
            if (i >= slotCursors.Length || slotCursors[i] == null) continue;
            slotCursors[i].alpha = (i == _selectedSlot) ? alpha : 0f;
        }
    }

    private void UpdateCursorVisibility()
    {
        // Reseta a fase para que o cursor comece visível ao mudar de slot
        _blinkPhase = 0.25f; // sin(0.25 * 2pi) = 1 → alpha máximo
    }

    private void HideAllCursors()
    {
        foreach (var cg in slotCursors)
            if (cg != null) cg.alpha = 0f;
    }

    // ── Feedback ──────────────────────────────────────────────────────────────

    private IEnumerator ShowFeedback(bool wrong, bool closeAfter)
    {
        HideFeedback();

        if (wrong)
            wrongFeedbackPanel?.SetActive(true);
        else
            correctFeedbackPanel?.SetActive(true);

        yield return new WaitForSeconds(feedbackDuration);

        HideFeedback();

        if (closeAfter)
            Close();
    }

    private void HideFeedback()
    {
        wrongFeedbackPanel?.SetActive(false);
        correctFeedbackPanel?.SetActive(false);
    }
}
