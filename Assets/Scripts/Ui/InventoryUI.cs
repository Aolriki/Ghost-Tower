using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public GameObject slotPrefab;
    public Transform slotsContainer;

    [Header("Settings")]
    public float slotSize = 120f;
    public float selectedScale = 1.2f;
    public Color borderColor = Color.white;
    public float borderThickness = 3f;

    private Image[] _slotIcons = new Image[8];
    private Outline[] _slotOutlines = new Outline[8];
    private int _selectedIndex = -1;

    void Start()
    {
        BuildSlots();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += Refresh;

        Refresh();
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    private void BuildSlots()
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotsContainer);
            slot.name = $"Slot_{i}";
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(slotSize, slotSize);
            _slotIcons[i] = slot.GetComponentInChildren<Image>(true);
        }
    }

    private void Refresh()
    {
        var items = InventoryManager.Instance.Items;

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
        RectTransform rt = slotsContainer.GetComponent<RectTransform>();
        if (rt == null) return;

        HorizontalLayoutGroup hlg = slotsContainer.GetComponent<HorizontalLayoutGroup>();
        float spacing = hlg != null ? hlg.spacing : 0f;
        int count = InventoryManager.Instance.Items.Count;
        if (count == 0) count = 1;

        float width = (slotSize * count) + (spacing * (count - 1));
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
    }

    public void SetSelected(int index)
    {
        _selectedIndex = index;
        ApplySelection();
    }

    private void ApplySelection()
    {
        var items = InventoryManager.Instance.Items;

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
        }
    }
}