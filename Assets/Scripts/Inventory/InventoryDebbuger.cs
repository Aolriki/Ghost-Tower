using UnityEngine;

public class InventoryDebugger : MonoBehaviour
{
    [Header("On Screen Log")]
    public bool showOnScreen = true;
    public Vector2 screenPosition = new Vector2(10f, 10f);

    void OnGUI()
    {
        if (!showOnScreen) return;
        if (InventoryManager.Instance == null) return;

        var items = InventoryManager.Instance.Items;
        string display = $"Inventory ({items.Count}/8)\n";
        for (int i = 0; i < items.Count; i++)
            display += $"[{i}] {items[i].itemName} ({items[i].type})\n";

        GUI.Label(new Rect(screenPosition.x, screenPosition.y, 300f, 400f), display);
    }
}