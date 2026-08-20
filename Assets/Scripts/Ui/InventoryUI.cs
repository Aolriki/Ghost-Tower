using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Singleton global (junto ao ScreenManager) que renderiza os slots do inventario no HotbarContainer.
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("References")]
    [Tooltip("Prefab de slot de item. Deve conter um Image e um TextMeshProUGUI filho.")]
    public GameObject slotPrefab;
    [Tooltip("Container dos slots. Atribuir no Inspector — deve ser filho do mesmo prefab.")]
    public RectTransform slotsContainer;

    [Header("Settings")]
    public float slotSize = 120f;
    public float selectedScale = 1.2f;


    private Image[] _slotIcons = new Image[8];
    private TMP_Text[] _slotLabels = new TMP_Text[8];
    private int _selectedIndex = -1;

    private InventoryManager _inventoryManager;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (slotsContainer == null)
            Debug.LogWarning("[InventoryUI] slotsContainer nao atribuido no Inspector.");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unsubscribe();
    }

    // Chamado pelo InventoryManager.Start de cada cena para reassinar os eventos locais.
    public void OnSceneReady()
    {
        Unsubscribe();
        _inventoryManager = InventoryManager.Instance;
        if (_inventoryManager == null) return;

        _inventoryManager.OnInventoryChanged += Refresh;
        BuildSlots();
        Refresh();
    }

    public void SetSelected(int index)
    {
        _selectedIndex = index;
        ApplySelection();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void BuildSlots()
    {
        if (slotsContainer == null || slotPrefab == null) return;

        // Limpa slots anteriores da cena anterior.
        foreach (Transform child in slotsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < 8; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotsContainer);
            slot.name = $"Slot_{i}";

            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(slotSize, slotSize);

            _slotIcons[i] = slot.GetComponentInChildren<Image>(true);
            _slotLabels[i] = slot.GetComponentInChildren<TMP_Text>(true);

            if (_slotLabels[i] != null)
                _slotLabels[i].gameObject.SetActive(false);
        }
    }

    private void Refresh()
    {
        if (_inventoryManager == null) return;

        var items = _inventoryManager.Items;

        for (int i = 0; i < 8; i++)
        {
            if (_slotIcons[i] == null) continue;

            if (i < items.Count && items[i].icon != null)
            {
                _slotIcons[i].sprite = items[i].icon;
                _slotIcons[i].enabled = true;
            }
            else
            {
                _slotIcons[i].sprite = null;
                _slotIcons[i].enabled = false;
            }
        }

        ApplySelection();
        Canvas.ForceUpdateCanvases();
        CenterContainer();
    }

    private void CenterContainer()
    {
        if (_inventoryManager == null || slotsContainer == null) return;

        RectTransform rt = slotsContainer.GetComponent<RectTransform>();
        if (rt == null) return;

        HorizontalLayoutGroup hlg = slotsContainer.GetComponent<HorizontalLayoutGroup>();
        float spacing = hlg != null ? hlg.spacing : 0f;
        float paddingLeft = hlg != null ? hlg.padding.left : 0f;
        float paddingRight = hlg != null ? hlg.padding.right : 0f;

        int count = _inventoryManager.Items.Count;
        if (count == 0) count = 1;

        float width = (slotSize * count) + (spacing * (count - 1)) + paddingLeft + paddingRight;
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
    }

    private void ApplySelection()
    {
        if (_inventoryManager == null || slotsContainer == null) return;

        var items = _inventoryManager.Items;

        for (int i = 0; i < 8; i++)
        {
            if (_slotIcons[i] == null) continue;

            bool selected = i == _selectedIndex;
            slotsContainer.GetChild(i).localScale = Vector3.one * (selected ? selectedScale : 1f);

            if (i < items.Count)
            {
                _slotIcons[i].sprite = selected && items[i].iconSelected != null
                    ? items[i].iconSelected
                    : items[i].icon;
            }

            if (_slotLabels[i] != null)
            {
                _slotLabels[i].gameObject.SetActive(selected && i < items.Count);
                if (selected && i < items.Count)
                    _slotLabels[i].text = items[i].itemName.GetLocalizedString();
            }
        }
    }

    private void Unsubscribe()
    {
        if (_inventoryManager == null) return;
        _inventoryManager.OnInventoryChanged -= Refresh;
        _inventoryManager = null;
    }
}