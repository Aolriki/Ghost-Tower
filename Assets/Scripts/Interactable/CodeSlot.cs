using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Prop "Cadeado de Senha". Herda de Interactable.
/// Sempre 3 slots, cada um com valores de 1 a 9 (armazenados internamente como 0 a 8).
/// O jogador monta a combinacao, navega pelos slots e confirma via CodeMode.
/// </summary>
public class CodeSlot : Interactable
{
    public enum CodeSlotState { Null, WrongCode, CorrectCode, Solved }

    // ── Numero fixo de slots e estados ────────────────────────────────────────

    public const int SlotCount = 3;
    public const int StatesPerSlot = 9;

    // ── Inspector: Camera ─────────────────────────────────────────────────────

    [Header("Camera")]
    [Tooltip("Posicao e rotacao para onde a camera vai ao abrir este prop. " +
             "Crie um GameObject filho 'CamAnchor' e posicione na frente do cadeado.")]
    public Transform camAnchor;

    // ── Inspector: Combinação correta ─────────────────────────────────────────

    [Header("Correct Combination")]
    [Tooltip("Valor correto do Slot A. Range 1-9 (o jogador ve esse numero).")]
    [Range(1, 9)] public int correctA = 1;

    [Tooltip("Valor correto do Slot B. Range 1-9.")]
    [Range(1, 9)] public int correctB = 1;

    [Tooltip("Valor correto do Slot C. Range 1-9.")]
    [Range(1, 9)] public int correctC = 1;

    [Tooltip("Se verdadeiro, vai direto para Solved ao acertar e desabilita a interacao.")]
    public bool solveIfCorrect = true;

    // ── Inspector: Objetos 3D dos slots (criptex) ─────────────────────────────

    [Header("Slot Objects (3D)")]
    public Transform slotLeft;
    public Transform slotMiddle;
    public Transform slotRight;

    // ── Inspector: Prop ───────────────────────────────────────────────────────

    [Header("Prop Object")]
    [Tooltip("Objeto raiz do prop. Recebera a animacao de unlock no futuro.")]
    public GameObject propRoot;

    // ── Inspector: Rotação ────────────────────────────────────────────────────

    [Header("Rotation Settings")]
    [Tooltip("Graus por face do criptex. Padrao: 40 (9 faces x 40 = 360).")]
    public float degreesPerFace = 40f;

    [Tooltip("Duracao da animacao de rotacao entre faces.")]
    public float rotationDuration = 0.15f;

    // ── Inspector: Navegação ──────────────────────────────────────────────────

    [Header("Navigation Settings")]
    [Tooltip("Intervalo minimo entre inputs repetidos ao segurar o botao.")]
    public float inputRepeatDelay = 0.2f;

    // ── Inspector: Events ─────────────────────────────────────────────────────

    [Header("Events")]
    public UnityEvent OnWrongCode;
    public UnityEvent OnCorrectCode;
    public UnityEvent OnSolved;

    // ── State: combinação ─────────────────────────────────────────────────────

    [SerializeField] private CodeSlotState _state = CodeSlotState.Null;
    public CodeSlotState State => _state;

    public int ValueA { get; private set; }
    public int ValueB { get; private set; }
    public int ValueC { get; private set; }

    // ── State: navegação e input ──────────────────────────────────────────────

    private bool _isActive;
    private int _selectedSlot;

    private float _holdTimer;

    // ── State: rotação ────────────────────────────────────────────────────────

    private float _rotA;
    private float _rotB;
    private float _rotC;

    private Coroutine _rotCoroutineA;
    private Coroutine _rotCoroutineB;
    private Coroutine _rotCoroutineC;

    // ── Interactable override ─────────────────────────────────────────────────

    public override void Interact()
    {
        if (!canInteract) return;
        Open();
    }

    // ── Open / Close ──────────────────────────────────────────────────────────

    /// <summary>Abre este prop e entra no modo CodeProp.</summary>
    public void Open()
    {
        if (_isActive) return;

        _isActive = true;
        _selectedSlot = 0;

        ResetSlotRotations();

        PlayerCore.Instance?.SetMovementEnabled(false);
        PlayerCore.Instance?.SetInteractionEnabled(false);
        InteractionUI.Instance?.Hide(transform);
        CodeMode.Instance?.EnterPropView(camAnchor);
        ScreenManager.Instance?.ChangeScreen(Screens.CodeProp);

        CodeMode.Instance?.Activate(this);
    }

    /// <summary>Fecha este prop e restaura o estado de Gameplay.</summary>
    public void Close()
    {
        if (!_isActive) return;

        _isActive = false;

        CodeMode.Instance?.Deactivate();
        CodeMode.Instance?.ExitPropView();
        ScreenManager.Instance?.ChangeScreen(Screens.Gameplay);
        PlayerCore.Instance?.SetMovementEnabled(true);
        PlayerCore.Instance?.SetInteractionEnabled(true);
    }

    // ── Input (chamado pelo CodeMode enquanto este prop estiver ativo) ─────────

    public void HandleNavigate(Vector2 input, bool isHeld)
    {
        if (!_isActive) return;

        if (isHeld)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer < inputRepeatDelay) return;
            _holdTimer = 0f;
        }
        else
        {
            _holdTimer = 0f;
        }

        ApplyNavigation(input);
    }

    public void HandleConfirm()
    {
        if (!_isActive) return;
        Submit();
    }

    public void HandleExit()
    {
        if (!_isActive) return;
        Close();
    }

    // ── Combinação ────────────────────────────────────────────────────────────

    public int GetValue(int slotIndex)
    {
        return slotIndex switch { 0 => ValueA, 1 => ValueB, _ => ValueC };
    }

    public void SetValue(int slotIndex, int value)
    {
        value = Mathf.Clamp(value, 0, StatesPerSlot - 1);
        switch (slotIndex)
        {
            case 0: ValueA = value; break;
            case 1: ValueB = value; break;
            case 2: ValueC = value; break;
        }
    }

    /// <summary>Confirma a combinacao. Chamado pelo HandleConfirm.</summary>
    public void TrySubmit()
    {
        // correctA/B/C sao 1-based no Inspector; valores internos sao 0-based
        bool correct = ValueA == correctA - 1 &&
                       ValueB == correctB - 1 &&
                       ValueC == correctC - 1;

        if (!correct) { SetState(CodeSlotState.WrongCode); return; }

        if (solveIfCorrect) SetState(CodeSlotState.Solved);
        else SetState(CodeSlotState.CorrectCode);
    }

    public void SetState(CodeSlotState newState)
    {
        _state = newState;
        switch (_state)
        {
            case CodeSlotState.WrongCode: OnWrongCode?.Invoke(); break;
            case CodeSlotState.CorrectCode: OnCorrectCode?.Invoke(); break;
            case CodeSlotState.Solved:
                canInteract = false;
                OnCantInteract();
                OnSolved?.Invoke();
                break;
        }
        Debug.Log($"[CodeSlot] {gameObject.name} -> {_state}");
    }

    // ── Visual (hook para blend shape futuro) ─────────────────────────────────

    public void OnSlotVisualUpdate(int slotIndex, int value)
    {
        // TODO: _skinnedMesh.SetBlendShapeWeight(slotIndex, value * (100f / (StatesPerSlot - 1)));
        Debug.Log($"[CodeSlot] Visual update -- {SlotName(slotIndex)} = {value + 1}");
    }

    // ── Navegação interna ─────────────────────────────────────────────────────

    private void ApplyNavigation(Vector2 input)
    {
        if (Mathf.Abs(input.x) > 0.5f)
        {
            int dir = input.x > 0f ? 1 : -1;
            _selectedSlot = Mathf.Clamp(_selectedSlot + dir, 0, SlotCount - 1);
            CodeMode.Instance?.OnSlotChanged(_selectedSlot);
            return;
        }

        if (Mathf.Abs(input.y) > 0.5f)
        {
            int dir = input.y > 0f ? 1 : -1;
            int current = GetValue(_selectedSlot);
            int next = (current + dir + StatesPerSlot) % StatesPerSlot;

            SetValue(_selectedSlot, next);
            OnSlotVisualUpdate(_selectedSlot, next);
            RotateSlot(_selectedSlot, dir);
        }
    }

    private void Submit()
    {
        TrySubmit();

        if (_state == CodeSlotState.Solved)
            StartCoroutine(SolvedSequence());
    }

    // ── Rotação dos slots (criptex) ───────────────────────────────────────────

    private void RotateSlot(int slotIndex, int direction)
    {
        float delta = degreesPerFace * direction;

        switch (slotIndex)
        {
            case 0:
                _rotA += delta;
                if (_rotCoroutineA != null) StopCoroutine(_rotCoroutineA);
                _rotCoroutineA = StartCoroutine(RotateTo(slotLeft, _rotA));
                break;
            case 1:
                _rotB += delta;
                if (_rotCoroutineB != null) StopCoroutine(_rotCoroutineB);
                _rotCoroutineB = StartCoroutine(RotateTo(slotMiddle, _rotB));
                break;
            case 2:
                _rotC += delta;
                if (_rotCoroutineC != null) StopCoroutine(_rotCoroutineC);
                _rotCoroutineC = StartCoroutine(RotateTo(slotRight, _rotC));
                break;
        }
    }

    private IEnumerator RotateTo(Transform slot, float targetXDegrees)
    {
        if (slot == null) yield break;

        Quaternion startRot = slot.localRotation;
        Quaternion endRot = Quaternion.Euler(targetXDegrees, 0f, 0f);
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            slot.localRotation = Quaternion.Lerp(startRot, endRot, elapsed / rotationDuration);
            yield return null;
        }

        slot.localRotation = endRot;
    }

    private void ResetSlotRotations()
    {
        _rotA = 0f; _rotB = 0f; _rotC = 0f;

        if (slotLeft != null) slotLeft.localRotation = Quaternion.identity;
        if (slotMiddle != null) slotMiddle.localRotation = Quaternion.identity;
        if (slotRight != null) slotRight.localRotation = Quaternion.identity;
    }

    // ── Solved sequence ───────────────────────────────────────────────────────

    private IEnumerator SolvedSequence()
    {
        // TODO: disparar animacao de unlock no propRoot
        // Exemplo futuro:
        //   _animator.SetTrigger("Unlock");
        //   yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).IsName("Unlocked"));

        yield return new WaitForSeconds(0.5f); // placeholder ate a animacao existir
        Close();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string SlotName(int i) => i switch
    {
        0 => "Slot Left",
        1 => "Slot Middle",
        _ => "Slot Right"
    };
}