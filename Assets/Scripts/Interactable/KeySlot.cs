using UnityEngine;
using UnityEngine.Events;

// Slot de chave. Aceita qualquer chave por padrao.
// Com isLocker = true, bloqueia itens errados e exibe o painel via KeySlotLockedUI.Instance.
public class KeySlot : Interactable, IItemReceiver
{
    public enum KeySlotState { Null, WrongKey, CorrectKey, Solved }

    [Header("Key Slot")]
    public ItemSO correctKeyItem;

    [SerializeField] private ItemSO storedKeyItem;
    [SerializeField] private KeySlotState state = KeySlotState.Null;
    public bool solveIfCorrect = true;

    [Header("Locker")]
    [Tooltip("Quando true, so aceita o correctKeyItem. Itens errados e maos vazias exibem o painel de bloqueio.")]
    public bool isLocker = false;

    [Tooltip("Offset do painel de bloqueio em relacao ao KeySlot.")]
    public Vector3 lockedUIOffset = new Vector3(0f, 1.8f, 0f);

    public KeySlotState State => state;
    public ItemSO StoredKeyItem => storedKeyItem;

    public UnityEvent OnSolved;

    public override void Interact()
    {
        if (!canInteract) return;

        ItemSO selectedItem = PlayerHandItem.Instance?.SelectedItem;

        // Locker: rejeita mao vazia e itens que nao sejam o correto.
        if (isLocker)
        {
            bool isCorrectItem = selectedItem != null
                                 && selectedItem.type == ItemType.Key
                                 && selectedItem == correctKeyItem;

            if (!isCorrectItem)
            {
                KeySlotLockedUI.Instance?.Show(transform.position + lockedUIOffset);
                return;
            }
        }
        else
        {
            // Sem locker: mao vazia devolve o item guardado.
            if (selectedItem == null)
            {
                if (storedKeyItem != null)
                {
                    InventoryManager.Instance.AddItem(storedKeyItem);
                    storedKeyItem = null;
                    state = KeySlotState.Null;
                }
                return;
            }

            if (selectedItem.type != ItemType.Key) return;
        }

        PlayerHandItem.Instance.DeliverTo(this);
    }

    public void ReceiveItem(ItemSO item)
    {
        if (item.type != ItemType.Key) return;

        ItemSO previousStored = storedKeyItem;

        InventoryManager.Instance.RemoveItem(item);
        storedKeyItem = item;

        if (previousStored != null)
            InventoryManager.Instance.AddItem(previousStored);

        EvaluateState();
    }

    private void EvaluateState()
    {
        if (storedKeyItem == null)
        {
            state = KeySlotState.Null;
            return;
        }

        bool isCorrect = correctKeyItem != null && storedKeyItem == correctKeyItem;

        if (!isCorrect)
        {
            state = KeySlotState.WrongKey;
            return;
        }

        if (solveIfCorrect)
        {
            SetState(KeySlotState.Solved);
            OnSolved?.Invoke();
        }
        else
        {
            state = KeySlotState.CorrectKey;
        }
    }

    public void SetState(KeySlotState newState)
    {
        state = newState;

        if (state == KeySlotState.Solved)
        {
            canInteract = false;
            OnCantInteract();
        }
    }
}