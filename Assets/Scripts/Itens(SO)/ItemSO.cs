using UnityEngine;

public enum ItemType { Key, Doc, Swaper }

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public Sprite icon;
    public Sprite iconSelected;
    public ItemType type;
    public string itemName;
}