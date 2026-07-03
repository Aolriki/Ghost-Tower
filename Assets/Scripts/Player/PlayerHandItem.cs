using UnityEngine;
using UnityEngine.InputSystem;

// Tracks which inventory item the player is currently holding and forwards it to the UI.
public class PlayerHandItem : MonoBehaviour
{
    public static PlayerHandItem Instance { get; private set; }

    public ItemSO SelectedItem => _selectedIndex >= 0 && _selectedIndex < InventoryManager.Instance.Items.Count
        ? InventoryManager.Instance.Items[_selectedIndex]
        : null;

    private int _selectedIndex = -1;
    private InventoryUI _inventoryUI;

    [SerializeField] private ItemSO _selectedItemDebug;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _inventoryUI = FindAnyObjectByType<InventoryUI>();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;

        SyncSelection();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
    }

    // Register this method on the Navigate event of PlayerInput in the Inspector.
    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        var items = InventoryManager.Instance.Items;
        if (items.Count == 0) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.x > 0.5f)
            _selectedIndex = Mathf.Min(_selectedIndex + 1, items.Count - 1);
        else if (input.x < -0.5f)
            _selectedIndex = Mathf.Max(_selectedIndex - 1, 0);

        UpdateUI();
    }

    // Register this method on the Read event of PlayerInput in the Inspector.
    public void OnRead(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (SelectedItem is not DocSO doc) return;

        ScreenManager.OpenDocItem(doc);
    }

    public void DeliverTo(IItemReceiver receiver)
    {
        if (SelectedItem == null) return;
        receiver.ReceiveItem(SelectedItem);
    }

    private void OnInventoryChanged()
    {
        SyncSelection();
    }

    private void SyncSelection()
    {
        var items = InventoryManager.Instance.Items;

        if (items.Count == 0)
            _selectedIndex = -1;
        else
            _selectedIndex = items.Count - 1;

        UpdateUI();
    }

    private void UpdateUI()
    {
        _selectedItemDebug = SelectedItem;
        _inventoryUI?.SetSelected(_selectedIndex);
    }
}