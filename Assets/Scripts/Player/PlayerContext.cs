using UnityEngine;

// Context enums evaluated every frame by PlayerContext.
public enum PropContext
{
    Null,
    ToTake,
    UseKey,
    ToInspect,
    TryUnlock,
    ToTalk
}

public enum NPCContext
{
    Null,
    GiveItem
}

public enum ItemContext
{
    Null,
    ToRead
}

// Reads PlayerInteraction and PlayerHandItem each frame and publishes context events
// when any enum value changes. One instance per gameplay scene, lives on the Player.
public class PlayerContext : MonoBehaviour
{
    public static PlayerContext Instance { get; private set; }

    // Current state, readable by HUDIcons without polling.
    public PropContext CurrentProp { get; private set; } = PropContext.Null;
    public NPCContext CurrentNPC { get; private set; } = NPCContext.Null;
    public ItemContext CurrentItem { get; private set; } = ItemContext.Null;

    // Events fired only when the value actually changes.
    public System.Action<PropContext> OnPropContextChanged;
    public System.Action<NPCContext> OnNPCContextChanged;
    public System.Action<ItemContext> OnItemContextChanged;

    private PlayerInteraction _interaction;
    private PlayerHandItem _handItem;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // PlayerInteraction is a child of the Player, PlayerHandItem is on the Player itself.
        _interaction = GetComponentInChildren<PlayerInteraction>();
        _handItem = GetComponent<PlayerHandItem>();

        if (_interaction == null)
            Debug.LogWarning("[PlayerContext] PlayerInteraction not found in children.");
        if (_handItem == null)
            Debug.LogWarning("[PlayerContext] PlayerHandItem not found on this GameObject.");
    }

    void Update()
    {
        EvaluatePropContext();
        EvaluateNPCContext();
        EvaluateItemContext();
    }

    // Derives the PropContext from the nearest interactable detected by PlayerInteraction.
    private void EvaluatePropContext()
    {
        PropContext next = PropContext.Null;

        Interactable nearest = _interaction?.NearestInteractable;

        if (nearest != null)
        {
            if (nearest is Collectable)
                next = PropContext.ToTake;
            else if (nearest is KeySlot)
                next = PropContext.UseKey;
            else if (nearest is DocSlot)
                next = PropContext.ToInspect;
            else if (nearest is CodeSlot)
                next = PropContext.TryUnlock;
            else if (nearest is NPCInteractable)
                next = PropContext.ToTalk;
        }

        if (next == CurrentProp) return;
        CurrentProp = next;
        OnPropContextChanged?.Invoke(CurrentProp);
    }

    // Give Item appears when talking to an NPC while holding any item.
    private void EvaluateNPCContext()
    {
        NPCContext next = NPCContext.Null;

        Interactable nearest = _interaction?.NearestInteractable;

        if (nearest is NPCInteractable && _handItem?.SelectedItem != null)
            next = NPCContext.GiveItem;

        if (next == CurrentNPC) return;
        CurrentNPC = next;
        OnNPCContextChanged?.Invoke(CurrentNPC);
    }

    // To Read appears when the selected item is of type Doc.
    private void EvaluateItemContext()
    {
        ItemContext next = ItemContext.Null;

        if (_handItem?.SelectedItem != null && _handItem.SelectedItem.type == ItemType.Doc)
            next = ItemContext.ToRead;

        if (next == CurrentItem) return;
        CurrentItem = next;
        OnItemContextChanged?.Invoke(CurrentItem);
    }
}