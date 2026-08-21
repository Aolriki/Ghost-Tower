using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Prop criptex de senha: herda de LookProp, monta a combinacao por slots e resolve ao confirmar.
public class CodeSlot : LookProp
{
    public enum CodeSlotState { Null, WrongCode, CorrectCode, Solved }
    public override InteractIcon Icon => InteractIcon.Cryptex;

    // Eixo unico de rotacao do criptex.
    public enum RotationAxis { X, Y, Z }

    // ── Slots ─────────────────────────────────────────────────────────────────────

    public const int SlotCount = 3;

    [Header("Slots")]
    [Tooltip("Quantidade de valores possiveis por slot. Define os graus por face (360 / valor).")]
    [Range(2, 12)] public int statesPerSlot = 9;

    // ── Combinacao correta ────────────────────────────────────────────────────────

    [Header("Correct Combination")]
    [Tooltip("Valor correto do Slot A. O jogador ve esse numero (1 ate statesPerSlot).")]
    public int correctA = 1;

    [Tooltip("Valor correto do Slot B.")]
    public int correctB = 1;

    [Tooltip("Valor correto do Slot C.")]
    public int correctC = 1;

    [Tooltip("Se verdadeiro, vai direto para Solved ao acertar e desabilita a interacao.")]
    public bool solveIfCorrect = true;

    // ── Objetos 3D dos slots (criptex) ────────────────────────────────────────────

    [Header("Slot Objects (3D)")]
    public Transform slotLeft;
    public Transform slotMiddle;
    public Transform slotRight;

    [Header("Shake")]
    [Tooltip("Transform da mesh do prop. Separado do GameObject pai que contem este script.")]
    public Transform meshTransform;
    [Tooltip("Intensidade do shake ao errar a senha.")]
    public float shakeMagnitude = 0.05f;
    [Tooltip("Duracao total do shake em segundos.")]
    public float shakeDuration = 0.4f;

    // ── Rotacao ───────────────────────────────────────────────────────────────────

    [Header("Rotation Settings")]
    [Tooltip("Eixo em torno do qual os slots giram. So um eixo por vez.")]
    public RotationAxis rotationAxis = RotationAxis.X;

    [Tooltip("Duracao da animacao de rotacao entre faces.")]
    public float rotationDuration = 0.15f;

    // ── Pinos (canvas world space do proprio prop) ────────────────────────────────

    [Header("Cursors (pinos indicadores de slot ativo)")]
    [Tooltip("CanvasGroup do pino do slot esquerdo.")]
    public CanvasGroup cursorLeft;

    [Tooltip("CanvasGroup do pino do slot do meio.")]
    public CanvasGroup cursorMiddle;

    [Tooltip("CanvasGroup do pino do slot direito.")]
    public CanvasGroup cursorRight;

    [Tooltip("Velocidade de piscada dos pinos.")]
    public float cursorBlinkSpeed = 3f;

    // ── Events ────────────────────────────────────────────────────────────────────

    [Header("Events")]
    public UnityEvent OnWrongCode;
    public UnityEvent OnCorrectCode;
    public UnityEvent OnSolved;

    // ── State: combinacao ─────────────────────────────────────────────────────────

    [SerializeField] private CodeSlotState _state = CodeSlotState.Null;
    public CodeSlotState State => _state;

    public int ValueA { get; private set; }
    public int ValueB { get; private set; }
    public int ValueC { get; private set; }

    // ── State: navegacao e blink ──────────────────────────────────────────────────

    private int _selectedSlot;
    private float _blinkPhase;

    // ── State: rotacao ────────────────────────────────────────────────────────────

    private float _rotLeft;
    private float _rotMiddle;
    private float _rotRight;

    private Coroutine _rotCoroutineLeft;
    private Coroutine _rotCoroutineMiddle;
    private Coroutine _rotCoroutineRight;
    private Coroutine _shakeCoroutine;

    // Graus por face derivados do numero de possibilidades.
    private float DegreesPerFace => 360f / statesPerSlot;

    // ── Unity ─────────────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        HideAllCursors();
    }

    void Update()
    {
        if (!IsLooking) return;
        UpdateCursorBlink();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // O atributo Range nao acompanha o statesPerSlot, entao o clamp e manual.
        correctA = Mathf.Clamp(correctA, 1, statesPerSlot);
        correctB = Mathf.Clamp(correctB, 1, statesPerSlot);
        correctC = Mathf.Clamp(correctC, 1, statesPerSlot);
    }
#endif

    // ── LookProp overrides ────────────────────────────────────────────────────────

    public override void OnEnterLook()
    {
        base.OnEnterLook();

        _selectedSlot = 0;
        ResetBlink();
        ResetAllSlots();

        PlayerContext.Instance?.SetLookContext(LookContext.CodeMode);
    }

    public override void OnExitLook()
    {
        base.OnExitLook();

        HideAllCursors();

        // Reseta slots ao sair, exceto quando o puzzle foi resolvido (visual permanece).
        if (_state != CodeSlotState.Solved)
            ResetAllSlots();

        PlayerContext.Instance?.SetLookContext(LookContext.Null);
    }

    // Cada chamada ja e um input valido e throttled: o LookMode gerencia o hold repeat.
    public override void OnLookNavigate(Vector2 input)
    {
        ApplyNavigation(input);
    }

    public override void OnLookConfirm()
    {
        Submit();
    }

    // OnLookCancel herdado de LookProp ja chama LookMode.Exit().

    // ── Combinacao ────────────────────────────────────────────────────────────────

    public int GetValue(int slotIndex)
    {
        return slotIndex switch { 0 => ValueA, 1 => ValueB, _ => ValueC };
    }

    public void SetValue(int slotIndex, int value)
    {
        value = Mathf.Clamp(value, 0, statesPerSlot - 1);
        switch (slotIndex)
        {
            case 0: ValueA = value; break;
            case 1: ValueB = value; break;
            case 2: ValueC = value; break;
        }
    }

    // Confirma a combinacao. correctA/B/C sao 1-based no Inspector; valores internos sao 0-based.
    public void TrySubmit()
    {
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
            case CodeSlotState.WrongCode:
                HUDNotification.Instance?.Show(HUDNotification.Instance.WrongCodeMessage);
                if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = StartCoroutine(ShakeRoutine());
                OnWrongCode?.Invoke();

                // NOVO: Toca o som de falha junto com o tremor visual
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(SFXType.FalhaDestrancar);
                break;

            case CodeSlotState.CorrectCode:
                OnCorrectCode?.Invoke();
                // NOVO: Opcional - Tocar som de sucesso aqui também, caso o puzzle tenha múltiplas etapas
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(SFXType.SucessoDestrancar);
                break;

            case CodeSlotState.Solved:
                canInteract = false;
                OnCantInteract();
                OnSolved?.Invoke();

                // NOVO: Som de sucesso ao concluir o puzzle e travar a interação
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(SFXType.SucessoDestrancar);
                break;
        }
        Debug.Log($"[CodeSlot] {gameObject.name} -> {_state}");
    }

    // Hook para blend shape futuro.
    public void OnSlotVisualUpdate(int slotIndex, int value)
    {
        // TODO: _skinnedMesh.SetBlendShapeWeight(slotIndex, value * (100f / (statesPerSlot - 1)));
        Debug.Log($"[CodeSlot] Visual update -- {SlotName(slotIndex)} = {value + 1}");
    }

    // ── Navegacao interna ─────────────────────────────────────────────────────────

    private void ApplyNavigation(Vector2 input)
    {
        if (Mathf.Abs(input.x) > 0.5f)
        {
            int dir = input.x > 0f ? 1 : -1;
            _selectedSlot = Mathf.Clamp(_selectedSlot + dir, 0, SlotCount - 1);
            ResetBlink();
            return;
        }

        if (Mathf.Abs(input.y) > 0.5f)
        {
            int dir = input.y > 0f ? 1 : -1;
            int current = GetValue(_selectedSlot);
            int next = (current + dir + statesPerSlot) % statesPerSlot;

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

    // ── Rotacao dos slots (criptex) ───────────────────────────────────────────────

    private void RotateSlot(int slotIndex, int direction)
    {
        float delta = DegreesPerFace * direction;

        switch (slotIndex)
        {
            case 0:
                _rotLeft += delta;
                if (_rotCoroutineLeft != null) StopCoroutine(_rotCoroutineLeft);
                _rotCoroutineLeft = StartCoroutine(RotateTo(slotLeft, _rotLeft));
                break;
            case 1:
                _rotMiddle += delta;
                if (_rotCoroutineMiddle != null) StopCoroutine(_rotCoroutineMiddle);
                _rotCoroutineMiddle = StartCoroutine(RotateTo(slotMiddle, _rotMiddle));
                break;
            case 2:
                _rotRight += delta;
                if (_rotCoroutineRight != null) StopCoroutine(_rotCoroutineRight);
                _rotCoroutineRight = StartCoroutine(RotateTo(slotRight, _rotRight));
                break;
        }
    }

    private IEnumerator RotateTo(Transform slot, float targetDegrees)
    {
        if (slot == null) yield break;

        Quaternion startRot = slot.localRotation;
        Quaternion endRot = Quaternion.Euler(AxisVector(targetDegrees));
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            slot.localRotation = Quaternion.Lerp(startRot, endRot, elapsed / rotationDuration);
            yield return null;
        }

        slot.localRotation = endRot;
    }

    // Monta o vetor de Euler colocando os graus apenas no eixo escolhido.
    private Vector3 AxisVector(float degrees)
    {
        switch (rotationAxis)
        {
            case RotationAxis.X: return new Vector3(degrees, 0f, 0f);
            case RotationAxis.Y: return new Vector3(0f, degrees, 0f);
            default: return new Vector3(0f, 0f, degrees);
        }
    }

    // Zera valores internos, acumuladores de rotacao e Transforms dos slots.
    // Interrompe coroutines pendentes antes de aplicar o identity para evitar que
    // uma animacao em andamento sobrescreva o reset no frame seguinte.
    private void ResetAllSlots()
    {
        ValueA = 0; ValueB = 0; ValueC = 0;

        _rotLeft = 0f; _rotMiddle = 0f; _rotRight = 0f;

        if (_rotCoroutineLeft != null) { StopCoroutine(_rotCoroutineLeft); _rotCoroutineLeft = null; }
        if (_rotCoroutineMiddle != null) { StopCoroutine(_rotCoroutineMiddle); _rotCoroutineMiddle = null; }
        if (_rotCoroutineRight != null) { StopCoroutine(_rotCoroutineRight); _rotCoroutineRight = null; }

        if (slotLeft != null) slotLeft.localRotation = Quaternion.identity;
        if (slotMiddle != null) slotMiddle.localRotation = Quaternion.identity;
        if (slotRight != null) slotRight.localRotation = Quaternion.identity;
    }

    // ── Shake ─────────────────────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine()
    {
        if (meshTransform == null) yield break;

        Vector3 origin = meshTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            // Intensidade diminui conforme o shake chega ao fim.
            float strength = shakeMagnitude * (1f - elapsed / shakeDuration);
            meshTransform.localPosition = origin + Random.insideUnitSphere * strength;
            yield return null;
        }

        meshTransform.localPosition = origin;
        _shakeCoroutine = null;
    }

    // ── Pinos (blink do slot selecionado) ─────────────────────────────────────────

    private void UpdateCursorBlink()
    {
        _blinkPhase += Time.deltaTime * cursorBlinkSpeed;
        float alpha = (Mathf.Sin(_blinkPhase * Mathf.PI * 2f) + 1f) * 0.5f;

        if (cursorLeft != null) cursorLeft.alpha = _selectedSlot == 0 ? alpha : 0f;
        if (cursorMiddle != null) cursorMiddle.alpha = _selectedSlot == 1 ? alpha : 0f;
        if (cursorRight != null) cursorRight.alpha = _selectedSlot == 2 ? alpha : 0f;
    }

    private void ResetBlink()
    {
        _blinkPhase = 0.25f;
    }

    private void HideAllCursors()
    {
        if (cursorLeft != null) cursorLeft.alpha = 0f;
        if (cursorMiddle != null) cursorMiddle.alpha = 0f;
        if (cursorRight != null) cursorRight.alpha = 0f;
    }

    // ── Solved sequence ───────────────────────────────────────────────────────────

    private IEnumerator SolvedSequence()
    {
        yield return new WaitForSeconds(0.5f); // placeholder ate a animacao existir
        OnLookCancel();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static string SlotName(int i) => i switch
    {
        0 => "Slot Left",
        1 => "Slot Middle",
        _ => "Slot Right"
    };
}