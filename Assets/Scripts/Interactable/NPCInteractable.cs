using Characters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Componente principal do NPC.
// Canal "Interact": entrega item (se canReceiveItems) ou inicia dialogo (se nao ha item na mao).
// Canal "Talk": inicia ou avanca o dialogo diretamente.
public class NPCInteractable : Interactable, IItemReceiver
{
    // ── Inspector: Identity ───────────────────────────────────────────────────

    [Header("Identity")]
    public CharacterName characterName;

    // ── Inspector: Dialogue ───────────────────────────────────────────────────

    [Header("Dialogue States")]
    [Tooltip("Array de SODialogue. Use SetState() para mudar o estado via evento externo.")]
    public SODialogue[] dialogueStates;

    [Header("Balloon Anchors")]
    [Tooltip("Posicao world do balao do NPC (acima da cabeca).")]
    public Transform npcBalloonAnchor;

    [Tooltip("Posicao world do balao do Player. Se vazio, usa PlayerCore + offset.")]
    public Transform playerBalloonAnchor;

    // ── Inspector: Item Receiving ─────────────────────────────────────────────

    [Header("Item Receiving")]
    [Tooltip("Habilita a recepcao de itens via canal Interact.")]
    public bool canReceiveItems = false;

    [Tooltip("Lista de itens aceitos e o estado que cada um aciona no NPC.")]
    public AcceptableItemEntry[] acceptableItems;

    [Tooltip("Dialogo exibido quando o player tenta entregar um item fora da lista. Nao altera o estado do NPC.")]
    public SODialogue refusalDialogue;

    // ── Inspector: Flip ───────────────────────────────────────────────────────

    [Header("Flip")]
    [Tooltip("Transform do visual do NPC usado para flip horizontal em direcao ao player.")]
    public Transform graphic;

    // ── Inspector: Events ─────────────────────────────────────────────────────

    [Header("Events")]
    public UnityEvent OnDialogueStarted;
    public UnityEvent OnDialogueFinished;
    public UnityEvent OnItemAccepted;

    // ── Private ───────────────────────────────────────────────────────────────

    [SerializeField] private int _currentStateIndex = 0;

    // Ultimo bloco executado. Null = ainda nao conversou neste estado.
    // Quando nao e null, o NPC repete este bloco nas proximas interacoes.
    // Resetado por SetState().
    private SODialogue _repeatDialogue;

    private bool _inDialogue;
    private bool _dialogueJustEnded;

    // Indica se o dialogo em execucao e de recusa (nao avanca estado, nao repete).
    private bool _isRefusalDialogue;

    // ── Properties ────────────────────────────────────────────────────────────

    public int CurrentStateIndex => _currentStateIndex;

    // Dialogo que sera executado na proxima interacao de conversa.
    // Se ja conversou neste estado, repete o ultimo bloco.
    // Caso contrario, comeca pelo inicio do estado atual.
    private SODialogue DialogueToPlay =>
        _repeatDialogue != null
            ? _repeatDialogue
            : CurrentStateDialogue;

    private SODialogue CurrentStateDialogue =>
        dialogueStates != null && _currentStateIndex < dialogueStates.Length
            ? dialogueStates[_currentStateIndex]
            : null;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!canInteract || graphic == null) return;
        FlipTowardPlayer();
    }

    // ── Interactable overrides ────────────────────────────────────────────────

    // "Interact": tenta entregar item se canReceiveItems e houver item na mao.
    //             Caso contrario, age como conversa normal.
    // "Talk": inicia ou avanca o dialogo diretamente, ignorando itens.
    public override void ReceiveInput(string channel)
    {
        if (channel == "Interact")
        {
            if (canReceiveItems && PlayerHandItem.Instance?.SelectedItem != null)
                TryDeliverItem();

            return;
        }

        if (channel == "Talk")
            Interact();
    }

    public override void Interact()
    {
        if (!canInteract) return;
        if (_dialogueJustEnded) return;

        if (_inDialogue)
        {
            DialogueManager.Instance?.Advance();
            return;
        }

        if (DialogueToPlay == null)
        {
            Debug.LogWarning($"[NPCInteractable] {name}: sem SODialogue no estado {_currentStateIndex}.");
            return;
        }

        BeginDialogue(DialogueToPlay, isRefusal: false);
    }

    public override void OnCanInteract()
    {
        if (characterName != CharacterName.None)
            base.OnCanInteract();
    }

    public override void OnCantInteract()
    {
        base.OnCantInteract();
    }

    // ── Item Receiving ────────────────────────────────────────────────────────

    // Chamado pelo PlayerHandItem.DeliverTo apos TryDeliverItem validar o item.
    public void ReceiveItem(ItemSO item)
    {
        if (item == null) return;

        AcceptableItemEntry entry = FindEntry(item);

        if (entry == null)
        {
            // Segurança: nao deveria chegar aqui, pois TryDeliverItem ja filtrou.
            PlayRefusalDialogue();
            return;
        }

        // Item aceito: remove do inventario, aciona o estado e dispara eventos.
        InventoryManager.Instance.RemoveItem(item);
        SetState(entry.nextStateIndex);
        OnItemAccepted?.Invoke();

        // Inicia automaticamente o dialogo do novo estado.
        Interact();
    }

    // Verifica se o item selecionado esta na lista antes de chamar DeliverTo.
    // Se nao estiver, aciona a recusa sem remover o item do inventario.
    private void TryDeliverItem()
    {
        if (_inDialogue) return;

        ItemSO selected = PlayerHandItem.Instance?.SelectedItem;

        if (selected == null) return;

        if (FindEntry(selected) == null)
        {
            PlayRefusalDialogue();
            return;
        }

        PlayerHandItem.Instance.DeliverTo(this);
    }

    private AcceptableItemEntry FindEntry(ItemSO item)
    {
        if (acceptableItems == null) return null;

        foreach (AcceptableItemEntry entry in acceptableItems)
            if (entry != null && entry.item == item)
                return entry;

        return null;
    }

    private void PlayRefusalDialogue()
    {
        if (refusalDialogue == null) return;
        if (_inDialogue) return;
        if (_dialogueJustEnded) return;

        BeginDialogue(refusalDialogue, isRefusal: true);
    }

    // ── Dialogue lifecycle ────────────────────────────────────────────────────

    private void BeginDialogue(SODialogue dialogue, bool isRefusal)
    {
        _inDialogue = true;
        _isRefusalDialogue = isRefusal;

        PlayerCore.Instance?.SetMovementEnabled(false);
        PlayerCore.Instance?.SetInteractionEnabled(false);
        InteractionUI.Instance?.Hide(transform);

        DialogueManager.Instance.OnDialogueEnd.AddListener(HandleDialogueEnded);
        DialogueManager.Instance.StartDialogue(dialogue, BuildCharactersData());

        if (!isRefusal)
            OnDialogueStarted?.Invoke();
    }

    private void HandleDialogueEnded()
    {
        DialogueManager.Instance.OnDialogueEnd.RemoveListener(HandleDialogueEnded);

        // Dialogo de recusa nao atualiza o _repeatDialogue nem dispara OnDialogueFinished.
        if (!_isRefusalDialogue)
        {
            _repeatDialogue = DialogueManager.Instance.LastPlayedDialogue;
            OnDialogueFinished?.Invoke();
        }

        _isRefusalDialogue = false;
        _inDialogue = false;
        _dialogueJustEnded = true;
        StartCoroutine(ClearDialogueEndedFlag());

        PlayerCore.Instance?.SetMovementEnabled(true);
        PlayerCore.Instance?.SetInteractionEnabled(true);
    }

    private IEnumerator ClearDialogueEndedFlag()
    {
        yield return null;
        _dialogueJustEnded = false;
    }

    // ── State machine ─────────────────────────────────────────────────────────

    // Muda o estado do NPC. Chamado por sistemas externos (puzzle, item, trigger).
    // Limpa o _repeatDialogue para que o novo estado comece pelo inicio.
    public void SetState(int index)
    {
        if (dialogueStates == null || index < 0 || index >= dialogueStates.Length)
        {
            Debug.LogWarning($"[NPCInteractable] SetState({index}): indice invalido. " +
                             $"Total: {dialogueStates?.Length}");
            return;
        }

        _currentStateIndex = index;
        _repeatDialogue = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<CharacterDialogueData> BuildCharactersData()
    {
        var list = new List<CharacterDialogueData>();

        Vector3 npcPos = npcBalloonAnchor != null
            ? npcBalloonAnchor.position
            : transform.position + Vector3.up * 2.5f;
        list.Add(new CharacterDialogueData(characterName, npcPos));

        Vector3 playerPos = playerBalloonAnchor != null
            ? playerBalloonAnchor.position
            : PlayerCore.Instance != null
                ? PlayerCore.Instance.transform.position + Vector3.up * 2.5f
                : Vector3.zero;
        list.Add(new CharacterDialogueData(CharacterName.Princess, playerPos));

        return list;
    }

    private void FlipTowardPlayer()
    {
        if (PlayerCore.Instance == null) return;
        bool playerIsRight = PlayerCore.Instance.transform.position.x > transform.position.x;
        graphic.localScale = playerIsRight ? Vector3.one : new Vector3(-1f, 1f, 1f);
    }
}

// ── Serializable Data ─────────────────────────────────────────────────────────

// Entrada da lista de itens aceitos pelo NPC.
// Cada item aponta para o estado que o NPC deve assumir apos a entrega.
[System.Serializable]
public class AcceptableItemEntry
{
    [Tooltip("Item que o NPC aceita.")]
    public ItemSO item;

    [Tooltip("Indice do estado que o NPC assume apos receber este item.")]
    public int nextStateIndex;
}