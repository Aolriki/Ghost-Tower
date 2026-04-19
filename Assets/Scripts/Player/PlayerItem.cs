using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItem : MonoBehaviour
{
    public static PlayerItem Instance { get; private set; }

    public ItemSO SelectedItem => _selectedIndex >= 0 && _selectedIndex < InventoryManager.Instance.Items.Count
        ? InventoryManager.Instance.Items[_selectedIndex]
        : null;

    private InventoryUI _inventoryUI;
    private int _selectedIndex = -1;

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
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
    }

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

    // Registre este método no evento Read do PlayerInput no Inspector.
    public void OnRead(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        ItemSO selected = SelectedItem;
        if (selected == null || selected.type != ItemType.Doc) return;

        DocSlot source = DocSlot.FindByItem(selected);
        if (source == null)
        {
            Debug.LogWarning("[PlayerItem] Nenhum DocSlot encontrado para o item selecionado.");
            return;
        }

        source.ReadMe();
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
        if (_inventoryUI != null)
            _inventoryUI.SetSelected(_selectedIndex);
    }

    public void DeliverTo(IItemReceiver receiver)
    {
        if (SelectedItem == null) return;
        receiver.ReceiveItem(SelectedItem);
    }
}