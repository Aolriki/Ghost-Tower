using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador do CodeSlot. Vive no mesmo GameObject que o CodeSlot.
/// Nao e singleton — cada prop na cena tem o seu proprio.
///
/// Responsabilidades:
///   - Receber input enquanto este prop estiver ativo
///   - Girar os objetos 3D dos slots (estilo criptex)
///   - Abrir e fechar o estado de interacao com o prop
///
/// UI compartilhada (cursores, legenda) e gerenciada pelo CodeSlotUIManager,
/// que e o unico singleton desse sistema.
/// </summary>
[RequireComponent(typeof(CodeSlot))]
public class CodeSlotController : MonoBehaviour
{
    // Inspector

    [Header("Slot Objects (3D)")]
    public Transform slotLeft;
    public Transform slotMiddle;
    public Transform slotRight;

    [Header("Prop Object")]
    [Tooltip("Objeto raiz do prop. Recebera a animacao de unlock no futuro.")]
    public GameObject propRoot;

    [Header("Rotation Settings")]
    [Tooltip("Graus por face do criptex. Padrao: 40 (9 faces x 40 = 360).")]
    public float degreesPerFace = 40f;

    [Tooltip("Duracao da animacao de rotacao entre faces.")]
    public float rotationDuration = 0.15f;

    [Header("Navigation Settings")]
    [Tooltip("Intervalo minimo entre inputs repetidos ao segurar o botao.")]
    public float inputRepeatDelay = 0.2f;

    // Private

    private CodeSlot _slot;
    private bool _isActive;
    private int _selectedSlot;

    private float _holdTimer;
    private Vector2 _lastNavInput;

    private float _rotA;
    private float _rotB;
    private float _rotC;

    private Coroutine _rotCoroutineA;
    private Coroutine _rotCoroutineB;
    private Coroutine _rotCoroutineC;

    // Unity

    void Awake()
    {
        _slot = GetComponent<CodeSlot>();
    }

    // Public API

    /// <summary>Abre este prop. Chamado pelo CodeSlot.Interact().</summary>
    public void Open()
    {
        if (_isActive) return;

        _isActive = true;
        _selectedSlot = 0;

        ResetSlotRotations();

        PlayerCore.Instance?.SetMovementEnabled(false);
        PlayerCore.Instance?.SetInteractionEnabled(false);
        InteractionUIManager.Instance?.Hide(transform);
        CodePropInteractionManager.Instance?.EnterPropView(_slot.camAnchor);
        ScreenManager.Instance?.ChangeScreen(Screens.CodeProp);

        CodePropInteractionManager.Instance?.Activate(this);
    }

    /// <summary>Fecha este prop e restaura o estado de Gameplay.</summary>
    public void Close()
    {
        if (!_isActive) return;

        _isActive = false;

        CodePropInteractionManager.Instance?.Deactivate();
        CodePropInteractionManager.Instance?.ExitPropView();
        ScreenManager.Instance?.ChangeScreen(Screens.Gameplay);
        PlayerCore.Instance?.SetMovementEnabled(true);
        PlayerCore.Instance?.SetInteractionEnabled(true);
    }

    // Input (chamado pelo CodeSlotUIManager enquanto este prop estiver ativo)

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

    // Navigation

    private void ApplyNavigation(Vector2 input)
    {
        if (Mathf.Abs(input.x) > 0.5f)
        {
            int dir = input.x > 0f ? 1 : -1;
            _selectedSlot = Mathf.Clamp(_selectedSlot + dir, 0, CodeSlot.SlotCount - 1);
            CodePropInteractionManager.Instance?.OnSlotChanged(_selectedSlot);
            return;
        }

        if (Mathf.Abs(input.y) > 0.5f)
        {
            int dir = input.y > 0f ? 1 : -1;
            int current = _slot.GetValue(_selectedSlot);
            int next = (current + dir + CodeSlot.StatesPerSlot) % CodeSlot.StatesPerSlot;

            _slot.SetValue(_selectedSlot, next);
            _slot.OnSlotVisualUpdate(_selectedSlot, next);
            RotateSlot(_selectedSlot, dir);
        }
    }

    private void Submit()
    {
        _slot.TrySubmit();

        if (_slot.State == CodeSlot.CodeSlotState.Solved)
            StartCoroutine(SolvedSequence());
    }

    // Slot Rotation

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

    // Solved Sequence

    private IEnumerator SolvedSequence()
    {
        // TODO: disparar animacao de unlock no propRoot
        // Exemplo futuro:
        //   _animator.SetTrigger("Unlock");
        //   yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).IsName("Unlocked"));

        yield return new WaitForSeconds(0.5f);   // placeholder ate a animacao existir
        Close();
    }
}