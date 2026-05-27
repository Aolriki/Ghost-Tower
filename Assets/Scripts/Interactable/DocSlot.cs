using UnityEngine;

public class DocSlot : Interactable
{
    [Header("Doc Item")]
    public ItemSO item;

    [Header("Doc Settings")]
    public bool giveItem = true;
    public GameObject docItemPagePrefab; // Prefab do painel específico deste documento

    // Referência guardada para que PlayerItem possa chamar ReadMe() pelo inventário
    public static DocSlot FindByItem(ItemSO item)
    {
        foreach (var slot in FindObjectsByType<DocSlot>())
            if (slot.item == item) return slot;
        return null;
    }

    public override void Interact()
    {
        if (!canInteract) return;
        if (item == null) return;
        if (InventoryManager.Instance == null) return;

        if (giveItem)
        {
            bool added = InventoryManager.Instance.AddItem(item);
            if (!added) return; // Inventário cheio: não abre o doc nem desativa

            canInteract = false;
            OnCantInteract();
        }

        ReadMe();
    }

    public void ReadMe()
    {
        if (docItemPagePrefab == null)
        {
            Debug.LogWarning($"[DocSlot] {gameObject.name}: docItemPagePrefab não atribuído.");
            return;
        }

        ScreenManager.Instance?.OpenDoc(docItemPagePrefab);
    }
}