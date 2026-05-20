using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Prop "Cadeado de Senha". Herda de Interactable3D.
///
/// Possui N slots (configurável de 1 a 4), cada um com statesPerSlot
/// estados possíveis (de 3 a 9). O jogador monta uma combinação e
/// confirma via CodePropController. O estado resultante pode ser
/// WrongCode, CorrectCode ou Solved.
///
/// Padrão idêntico ao KeySlot: use solveIfCorrect = true para que o
/// prop seja desabilitado automaticamente ao acertar o código.
/// </summary>
public class CodeSlot : Interactable3D
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    public enum CodeSlotState { Null, WrongCode, CorrectCode, Solved }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Code Settings")]
    [Tooltip("Número de slots (campos) do cadeado. Mínimo 1, máximo 4.")]
    [Range(1, 4)]
    public int slotCount = 3;

    [Tooltip("Quantos estados cada slot pode ter (1 … N). Mínimo 3, máximo 9.")]
    [Range(3, 9)]
    public int statesPerSlot = 9;

    [Tooltip("Combinação correta. Cada elemento é um índice de 0 a statesPerSlot-1.")]
    public int[] correctCombination = new int[] { 0, 3, 6 };

    [Tooltip("Se verdadeiro, vai direto para Solved ao acertar o código " +
             "e desabilita a interação, igual ao KeySlot.")]
    public bool solveIfCorrect = true;

    [Header("Events")]
    public UnityEvent OnWrongCode;
    public UnityEvent OnCorrectCode;
    public UnityEvent OnSolved;

    // ── State ─────────────────────────────────────────────────────────────────

    [SerializeField] private CodeSlotState _state = CodeSlotState.Null;
    public CodeSlotState State => _state;

    /// <summary>Valores atuais de cada slot (índices 0-based).</summary>
    public int[] CurrentValues { get; private set; }

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Start()
    {
        InitSlots();
    }

    // ── Interactable3D override ───────────────────────────────────────────────

    /// <summary>
    /// Quando o jogador interage com o prop, abre o CodePropController.
    /// O controller assume o controle da câmera, input e HUD.
    /// </summary>
    public override void Interact()
    {
        if (!canInteract) return;
        CodePropController.Instance?.Open(this);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Define o valor de um slot individualmente (chamado pelo CodePropController
    /// enquanto o jogador navega).
    /// </summary>
    public void SetSlotValue(int slotIndex, int value)
    {
        if (slotIndex < 0 || slotIndex >= slotCount) return;
        CurrentValues[slotIndex] = Mathf.Clamp(value, 0, statesPerSlot - 1);
    }

    /// <summary>
    /// Confirma a combinação atual. Avalia e muda o estado.
    /// Chamado pelo CodePropController quando o jogador pressiona confirmar.
    /// </summary>
    public void TrySubmit()
    {
        bool correct = EvaluateCombination();

        if (!correct)
        {
            SetState(CodeSlotState.WrongCode);
            return;
        }

        if (solveIfCorrect)
            SetState(CodeSlotState.Solved);
        else
            SetState(CodeSlotState.CorrectCode);
    }

    /// <summary>
    /// Força um estado externo (ex: sistema de puzzle chama SetState(Solved)).
    /// </summary>
    public void SetState(CodeSlotState newState)
    {
        _state = newState;

        switch (_state)
        {
            case CodeSlotState.WrongCode:
                OnWrongCode?.Invoke();
                break;

            case CodeSlotState.CorrectCode:
                OnCorrectCode?.Invoke();
                break;

            case CodeSlotState.Solved:
                canInteract = false;
                OnCantInteract();
                OnSolved?.Invoke();
                break;
        }

        Debug.Log($"[CodeSlot] {gameObject.name} → {_state}");
    }

    // ── ShapeKey / Model hook (para quando o modelo 3D estiver pronto) ────────

    /// <summary>
    /// Chamado pelo CodePropController sempre que um slot muda de valor.
    /// Aqui você vai conectar o blend shape do modelo 3D no futuro.
    /// Por ora, apenas loga para confirmar que a comunicação funciona.
    /// </summary>
    public void OnSlotVisualUpdate(int slotIndex, int value)
    {
        // TODO: acionar blend shape / animação do modelo 3D
        // Exemplo futuro:
        //   _skinnedMeshRenderer.SetBlendShapeWeight(slotIndex * statesPerSlot + value, 100f);
        Debug.Log($"[CodeSlot] Visual update — Slot {slotIndex} = {value + 1}");
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void InitSlots()
    {
        CurrentValues = new int[slotCount];

        // Garante que correctCombination tem o tamanho certo
        if (correctCombination == null || correctCombination.Length != slotCount)
        {
            correctCombination = new int[slotCount];
            Debug.LogWarning($"[CodeSlot] {gameObject.name}: correctCombination redimensionado para {slotCount} slots. " +
                             "Configure os valores corretos no Inspector.");
        }
    }

    private bool EvaluateCombination()
    {
        if (correctCombination == null || correctCombination.Length != slotCount)
            return false;

        for (int i = 0; i < slotCount; i++)
            if (CurrentValues[i] != correctCombination[i])
                return false;

        return true;
    }
}