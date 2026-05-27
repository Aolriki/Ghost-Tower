using System.Collections.Generic;
using UnityEngine;

// Manages the player item list for the current scene.
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
        // Notify the global HUD systems that a new scene is ready.
        // InventoryUI reconnects here because it lives on the global ScreenManager object
        // and cannot rely on Start() ordering relative to this scene-local component.
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

    public void RemoveItem(ItemSO item)
    {
        if (_items.Remove(item))
            OnInventoryChanged?.Invoke();
    }
}