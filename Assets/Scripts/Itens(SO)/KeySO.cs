using UnityEngine;

// Item do tipo chave, consumido por KeySlot.
[CreateAssetMenu(fileName = "NewKey", menuName = "Inventory/Key")]
public class KeySO : ItemSO
{
    public override ItemType type => ItemType.Key;
}