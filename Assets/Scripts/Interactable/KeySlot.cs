using UnityEngine;
using UnityEngine.Events;

// Prop de chave: consome a chave correta do inventario, resolve, e exibe dica no HUD em tentativas erradas.
public class KeySlot : Interactable
{
    public enum KeySlotState { Hidden, Default, Solved }

    public override InteractIcon Icon => InteractIcon.Padlock;

    [Header("Key Slot")]
    public KeySO correctKeyItem;

    [Tooltip("Mensagem customizada exibida em tentativas erradas. None usa o texto padrao do HUDNotification.")]
    [SerializeField] private KeySlotMessageId messageId = KeySlotMessageId.None;

    [SerializeField] private KeySlotState state = KeySlotState.Default;
    public KeySlotState State => state;

    public UnityEvent OnSolved;
    public UnityEvent OnFail; // NOVO: Evento específico para quando o jogador erra a chave

    void Awake()
    {
        ApplyState();
    }

    public override void Interact()
    {
        if (!canInteract) return;

        ItemSO selectedItem = PlayerHandItem.Instance?.SelectedItem;
        bool isCorrect = correctKeyItem != null && selectedItem == correctKeyItem;

        // Mao vazia ou item errado: apenas mostra a dica, sem consumir nada.
        if (!isCorrect)
        {
            ShowWrongMessage();
            OnFail?.Invoke(); // NOVO: Dispara o evento de erro para o Inspector
            return;
        }

        InventoryManager.Instance.RemoveItem(selectedItem);
        SetState(KeySlotState.Solved);
    }

    // Revela o slot, liberando a interacao. Chamado por sistemas externos.
    public void Reveal()
    {
        if (state != KeySlotState.Hidden) return;
        SetState(KeySlotState.Default);
    }

    private void SetState(KeySlotState newState)
    {
        state = newState;
        ApplyState();

        if (state == KeySlotState.Solved)
            OnSolved?.Invoke();
    }

    // Sincroniza a interacao e o icone com o estado atual.
    private void ApplyState()
    {
        switch (state)
        {
            case KeySlotState.Default:
                canInteract = true;
                break;
            case KeySlotState.Hidden:
            case KeySlotState.Solved:
                canInteract = false;
                OnCantInteract();
                break;
        }
    }

    private void ShowWrongMessage()
    {
        if (HUDNotification.Instance == null) return;

        string message = HUDNotification.Instance.GetKeySlotMessage(messageId);
        HUDNotification.Instance.Show(message);
    }
}