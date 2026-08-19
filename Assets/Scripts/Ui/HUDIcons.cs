using TMPro;
using UnityEngine;
using UnityEngine.Localization;
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
//   Default  -> To Take, Use Key, To Inspect, Try Unlock, Place Item, Give Item
//   ToTalk   -> To Talk
//   ToRead   -> To Read
// During LookContext.CodeMode, all normal icons hide and the code mode group appears.
public class HUDIcons : MonoBehaviour
{
    public static HUDIcons Instance { get; private set; }

    [Header("Container")]
    [Tooltip("RectTransform with HorizontalLayoutGroup that holds the three icon roots.")]
    public RectTransform iconsContainer;

    [Header("Default Input Icon")]
    public HUDIconEntry defaultIcon;
    public LocalizedString labelToTake;
    public LocalizedString labelUseKey;
    public LocalizedString labelToInspect;
    public LocalizedString labelTryUnlock;
    public LocalizedString labelPlaceItem;
    public LocalizedString labelGiveItem;

    [Header("To Talk Icon")]
    public HUDIconEntry toTalkIcon;
    public LocalizedString labelToTalk;

    [Header("To Read Icon")]
    public HUDIconEntry toReadIcon;
    public LocalizedString labelToRead;

    [Header("Code Mode Group")]
    [Tooltip("GameObject pai que agrupa os quatro icones do code mode. Comeca desativado.")]
    public GameObject codeModeGroup;
    public HUDIconEntry confirmIcon;
    public HUDIconEntry rotateIcon;
    public HUDIconEntry navigateIcon;
    public LocalizedString labelToConfirm;
    public LocalizedString labelToRotate;
    public LocalizedString labelToNavigate;

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
        _context.OnLookContextChanged += HandleLookContext;

        // Sync immediately with whatever state the context already has.
        HandleLookContext(_context.CurrentLook);
        HandlePropContext(_context.CurrentProp);
        HandleNPCContext(_context.CurrentNPC);
        HandleItemContext(_context.CurrentItem);
    }

    // ── Look context ──────────────────────────────────────────────────────────

    private void HandleLookContext(LookContext ctx)
    {
        bool inCode = ctx == LookContext.CodeMode;

        // Esconde todos os icones normais ao entrar no code mode.
        if (inCode)
        {
            SetIcon(defaultIcon, false);
            SetIcon(toTalkIcon, false);
            SetIcon(toReadIcon, false);
            RebuildLayout();
        }

        // Ativa ou desativa o grupo de code mode.
        if (codeModeGroup != null)
            codeModeGroup.SetActive(inCode);

        if (inCode)
        {
            ShowCodeIcon(confirmIcon, labelToConfirm);
            ShowCodeIcon(rotateIcon, labelToRotate);
            ShowCodeIcon(navigateIcon, labelToNavigate);
        }

        // Ao sair do code mode, forca o sync dos contextos — o PlayerContext nao
        // dispara eventos quando os valores nao mudaram durante a sessao.
        if (!inCode && _context != null)
        {
            HandlePropContext(_context.CurrentProp);
            HandleNPCContext(_context.CurrentNPC);
            HandleItemContext(_context.CurrentItem);
        }
    }

    // ── Prop context ──────────────────────────────────────────────────────────

    private void HandlePropContext(PropContext ctx)
    {
        // Ignora enquanto o code mode estiver ativo.
        if (_context != null && _context.CurrentLook != LookContext.Null) return;

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
            case PropContext.PlaceItem:
                ShowDefault(labelPlaceItem);
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
        if (_context != null && _context.CurrentLook != LookContext.Null) return;

        if (ctx == NPCContext.GiveItem)
            ShowDefault(labelGiveItem);

        RebuildLayout();
    }

    // Item context drives the To Read icon.
    private void HandleItemContext(ItemContext ctx)
    {
        if (_context != null && _context.CurrentLook != LookContext.Null) return;

        bool show = ctx == ItemContext.ToRead;
        if (show)
            ShowToRead(labelToRead);
        else
            SetIcon(toReadIcon, false);

        RebuildLayout();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowDefault(LocalizedString text)
    {
        if (defaultIcon == null) return;
        SetIcon(defaultIcon, true);
        if (defaultIcon.label != null)
            defaultIcon.label.text = text.GetLocalizedString();
    }

    private void ShowToTalk(LocalizedString text)
    {
        if (toTalkIcon == null) return;
        SetIcon(toTalkIcon, true);
        if (toTalkIcon.label != null)
            toTalkIcon.label.text = text.GetLocalizedString();
    }

    private void ShowToRead(LocalizedString text)
    {
        if (toReadIcon == null) return;
        SetIcon(toReadIcon, true);
        if (toReadIcon.label != null)
            toReadIcon.label.text = text.GetLocalizedString();
    }

    private void ShowCodeIcon(HUDIconEntry entry, LocalizedString text)
    {
        if (entry == null) return;
        SetIcon(entry, true);
        if (entry.label != null)
            entry.label.text = text.GetLocalizedString();
    }

    private void SetIcon(HUDIconEntry entry, bool active)
    {
        if (entry?.iconRoot != null)
            entry.iconRoot.SetActive(active);
    }

    private void HideAll()
    {
        SetIcon(defaultIcon, false);
        SetIcon(toTalkIcon, false);
        SetIcon(toReadIcon, false);
        if (codeModeGroup != null) codeModeGroup.SetActive(false);
        RebuildLayout();
    }

    // Resizes and recenters the container based on how many icon roots are active.
    private void RebuildLayout()
    {
        if (iconsContainer == null) return;

        HorizontalLayoutGroup hlg = iconsContainer.GetComponent<HorizontalLayoutGroup>();
        float spacing = hlg != null ? hlg.spacing : 0f;

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

    private int CountActiveIcons()
    {
        int n = 0;
        if (defaultIcon?.iconRoot != null && defaultIcon.iconRoot.activeSelf) n++;
        if (toTalkIcon?.iconRoot != null && toTalkIcon.iconRoot.activeSelf) n++;
        if (toReadIcon?.iconRoot != null && toReadIcon.iconRoot.activeSelf) n++;
        return n;
    }

    private void Unsubscribe()
    {
        if (_context == null) return;
        _context.OnPropContextChanged -= HandlePropContext;
        _context.OnNPCContextChanged -= HandleNPCContext;
        _context.OnItemContextChanged -= HandleItemContext;
        _context.OnLookContextChanged -= HandleLookContext;
        _context = null;
    }
}