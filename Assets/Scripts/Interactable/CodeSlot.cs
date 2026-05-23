using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Prop "Cadeado de Senha". Herda de Interactable3D.
/// Sempre 3 slots, cada um com valores de 1 a 9 (armazenados internamente como 0 a 8).
/// O jogador monta a combinacao e confirma via CodePropController.
/// </summary>
public class CodeSlot : Interactable3D
{
    public enum CodeSlotState { Null, WrongCode, CorrectCode, Solved }

    // Inspector

    [Header("Camera")]
    [Tooltip("Posicao e rotacao para onde a camera vai ao abrir este prop. " +
             "Crie um GameObject filho 'CamAnchor' e posicione na frente do cadeado.")]
    public Transform camAnchor;

    [Header("Correct Combination")]
    [Tooltip("Valor correto do Slot A. Range 1-9 (o jogador ve esse numero).")]
    [Range(1, 9)] public int correctA = 1;

    [Tooltip("Valor correto do Slot B. Range 1-9.")]
    [Range(1, 9)] public int correctB = 1;

    [Tooltip("Valor correto do Slot C. Range 1-9.")]
    [Range(1, 9)] public int correctC = 1;

    [Tooltip("Se verdadeiro, vai direto para Solved ao acertar e desabilita a interacao.")]
    public bool solveIfCorrect = true;

    [Header("Events")]
    public UnityEvent OnWrongCode;
    public UnityEvent OnCorrectCode;
    public UnityEvent OnSolved;

    // State

    [SerializeField] private CodeSlotState _state = CodeSlotState.Null;
    public CodeSlotState State => _state;

    // Valores atuais dos 3 slots, sempre iniciam em 0 (exibido como "1" na HUD)
    public int ValueA { get; private set; }
    public int ValueB { get; private set; }
    public int ValueC { get; private set; }

    // Numero fixo de slots e estados
    public const int SlotCount = 3;
    public const int StatesPerSlot = 9;

    // Interactable3D override

    public override void Interact()
    {
        if (!canInteract) return;
        GetComponent<CodeSlotController>()?.Open();
    }

    // Public API

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

    /// <summary>Confirma a combinacao. Chamado pelo CodePropController.</summary>
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

    // Hook para blend shape futuro

    public void OnSlotVisualUpdate(int slotIndex, int value)
    {
        // TODO: _skinnedMesh.SetBlendShapeWeight(slotIndex, value * (100f / (StatesPerSlot - 1)));
        Debug.Log($"[CodeSlot] Visual update -- {SlotName(slotIndex)} = {value + 1}");
    }

    private static string SlotName(int i) => i switch
    {
        0 => "Slot Left",
        1 => "Slot Middle",
        _ => "Slot Right"
    };
}