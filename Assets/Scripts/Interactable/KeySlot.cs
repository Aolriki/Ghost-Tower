using UnityEngine;
using UnityEngine.Events;

public class KeySlot : Interactable3D, IItemReceiver
{
    public enum KeySlotState { Null, WrongKey, CorrectKey, Solved }

    [Header("Key Slot")]
    public ItemSO correctKeyItem;

    [SerializeField] private ItemSO storedKeyItem;
    [SerializeField] private KeySlotState state = KeySlotState.Null;
    public bool solveIfCorrect = true;

    public KeySlotState State => state;
    public ItemSO StoredKeyItem => storedKeyItem;


    public UnityEvent OnSolved;

    public override void Interact()
    {
        if (!canInteract) return;

        ItemSO selectedItem = PlayerItem.Instance?.SelectedItem;

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

        PlayerItem.Instance.DeliverTo(this);
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