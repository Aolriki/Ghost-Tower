using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Groups a shared icon image with its label text below it.
[System.Serializable]
public class HUDIconEntry
{
    public GameObject iconRoot;
    public Image iconImage;
    public TextMeshProUGUI label;
}

// Lives on the global ScreenManager GameObject (or a child of it).
// Subscribes to PlayerContext events when a gameplay scene loads.
// Three shared icon slots cover all action contexts:
//   Default  -> To Take, Use Key, To Inspect, Try Unlock, Give Item
//   ToTalk   -> To Talk
//   ToRead   -> To Read
public class HUDIcons : MonoBehaviour
{
    public static HUDIcons Instance { get; private set; }

    [Header("Container")]
    [Tooltip("RectTransform with HorizontalLayoutGroup that holds the three icon roots.")]
    public RectTransform iconsContainer;

    [Header("Default Input Icon")]
    public HUDIconEntry defaultIcon;
    public string labelToTake = "To Take";
    public string labelUseKey = "Use Key";
    public string labelToInspect = "To Inspect";
    public string labelTryUnlock = "Try Unlock";
    public string labelGiveItem = "Give Item";

    [Header("To Talk Icon")]
    public HUDIconEntry toTalkIcon;
    public string labelToTalk = "To Talk";

    [Header("To Read Icon")]
    public HUDIconEntry toReadIcon;
    public string labelToRead = "To Read";

    // Cached reference to the scene-local PlayerContext.
    private PlayerContext _context;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unsubscribe();
    }

    // Called by the scene-local InventoryManager.Start() for every gameplay scene.
    public void OnSceneReady()
    {
        Unsubscribe();

        _context = PlayerContext.Instance;

        if (_context == null)
        {
            HideAll();
            return;
        }

        _context.OnPropContextChanged += HandlePropContext;
        _context.OnNPCContextChanged += HandleNPCContext;
        _context.OnItemContextChanged += HandleItemContext;

        // Sync immediately with whatever state the context already has.
        HandlePropContext(_context.CurrentProp);
        HandleNPCContext(_context.CurrentNPC);
        HandleItemContext(_context.CurrentItem);
    }

    // Prop context drives the default icon and the To Talk icon.
    private void HandlePropContext(PropContext ctx)
    {
        switch (ctx)
        {
            case PropContext.ToTake:
                ShowDefault(labelToTake);
                SetIcon(toTalkIcon, false);
                break;
            case PropContext.UseKey:
                ShowDefault(labelUseKey);
                SetIcon(toTalkIcon, false);
                break;
            case PropContext.ToInspect:
                ShowDefault(labelToInspect);
                SetIcon(toTalkIcon, false);
                break;
            case PropContext.TryUnlock:
                ShowDefault(labelTryUnlock);
                SetIcon(toTalkIcon, false);
                break;
            case PropContext.ToTalk:
                SetIcon(defaultIcon, false);
                ShowToTalk(labelToTalk);
                break;
            default:
                SetIcon(defaultIcon, false);
                SetIcon(toTalkIcon, false);
                break;
        }

        RebuildLayout();
    }

    // NPC context overrides the default icon with Give Item when applicable.
    private void HandleNPCContext(NPCContext ctx)
    {
        if (ctx == NPCContext.GiveItem)
            ShowDefault(labelGiveItem);

        RebuildLayout();
    }

    // Item context drives the To Read icon.
    private void HandleItemContext(ItemContext ctx)
    {
        bool show = ctx == ItemContext.ToRead;
        if (show)
            ShowToRead(labelToRead);
        else
            SetIcon(toReadIcon, false);

        RebuildLayout();
    }

    // Shows the default icon with the given label text.
    private void ShowDefault(string text)
    {
        if (defaultIcon == null) return;
        SetIcon(defaultIcon, true);
        if (defaultIcon.label != null)
            defaultIcon.label.text = text;
    }

    // Shows the To Talk icon with the given label text.
    private void ShowToTalk(string text)
    {
        if (toTalkIcon == null) return;
        SetIcon(toTalkIcon, true);
        if (toTalkIcon.label != null)
            toTalkIcon.label.text = text;
    }

    // Shows the To Read icon with the given label text.
    private void ShowToRead(string text)
    {
        if (toReadIcon == null) return;
        SetIcon(toReadIcon, true);
        if (toReadIcon.label != null)
            toReadIcon.label.text = text;
    }

    // Activates or deactivates a single icon root.
    private void SetIcon(HUDIconEntry entry, bool active)
    {
        if (entry?.iconRoot != null)
            entry.iconRoot.SetActive(active);
    }

    // Deactivates all icons.
    private void HideAll()
    {
        SetIcon(defaultIcon, false);
        SetIcon(toTalkIcon, false);
        SetIcon(toReadIcon, false);
        RebuildLayout();
    }

    // Resizes and recenters the container based on how many icon roots are active.
    private void RebuildLayout()
    {
        if (iconsContainer == null) return;

        HorizontalLayoutGroup hlg = iconsContainer.GetComponent<HorizontalLayoutGroup>();
        float spacing = hlg != null ? hlg.spacing : 0f;

        // Uses sizeDelta to read the slot width as defined in the editor,
        // independent of the layout rebuild cycle.
        float slotWidth = iconsContainer.childCount > 0
            ? ((RectTransform)iconsContainer.GetChild(0)).sizeDelta.x
            : 0f;

        int count = CountActiveIcons();
        if (count == 0) count = 1;

        float width = (slotWidth * count) + (spacing * (count - 1));
        iconsContainer.sizeDelta = new Vector2(width, iconsContainer.sizeDelta.y);
        iconsContainer.anchoredPosition = new Vector2(0f, iconsContainer.anchoredPosition.y);

        LayoutRebuilder.ForceRebuildLayoutImmediate(iconsContainer);
    }

    // Counts how many icon roots are currently active.
    private int CountActiveIcons()
    {
        int n = 0;
        if (defaultIcon?.iconRoot != null && defaultIcon.iconRoot.activeSelf) n++;
        if (toTalkIcon?.iconRoot != null && toTalkIcon.iconRoot.activeSelf) n++;
        if (toReadIcon?.iconRoot != null && toReadIcon.iconRoot.activeSelf) n++;
        return n;
    }

    // Removes all subscriptions to avoid leaks when the context is destroyed.
    private void Unsubscribe()
    {
        if (_context == null) return;
        _context.OnPropContextChanged -= HandlePropContext;
        _context.OnNPCContextChanged -= HandleNPCContext;
        _context.OnItemContextChanged -= HandleItemContext;
        _context = null;
    }
}