using UnityEngine;

public class Collectable : Interactable
{
    [Header("Item")]
    public ItemSO item;

    public override void Interact()
    {
        if (!canInteract) return;
        if (item == null) return;
        if (InventoryManager.Instance == null) return;


        Debug.Log($"Collectable: {gameObject.name} | Item: {item.itemName}"); // Remover depois

       
        bool added = InventoryManager.Instance.AddItem(item);
        if (!added) return;

        canInteract = false;
        OnCantInteract();
    }
}