using System.Collections.Generic;
using UnityEngine;

// Singleton local de cena que gerencia a lista de itens do jogador.
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

    void Start()
    {
        // Notifica os sistemas globais de HUD que uma nova cena esta pronta.
        InventoryUI.Instance?.OnSceneReady();
        HUDIcons.Instance?.OnSceneReady();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool AddItem(ItemSO item)
    {
        if (_items.Count >= MaxItems) return false;
        _items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void AddItem_Event(ItemSO item) => AddItem(item);
    public void RemoveItem(ItemSO item)
    {
        if (_items.Remove(item))
            OnInventoryChanged?.Invoke();
    }
}