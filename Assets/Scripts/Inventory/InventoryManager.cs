using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public IReadOnlyList<ItemSO> Items => _items;

    public System.Action OnInventoryChanged;

    [SerializeField] List<ItemSO> _items = new List<ItemSO>(8);
    private const int MaxItems = 8;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool AddItem(ItemSO item)
    {
        if (_items.Count >= MaxItems) return false;
        _items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItem(ItemSO item)
    {
        if (_items.Remove(item))
            OnInventoryChanged?.Invoke();
    }
}