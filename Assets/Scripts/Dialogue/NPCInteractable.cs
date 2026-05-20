using Characters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente principal do NPC. Herda de Interactable3D.
///
/// Estado de diálogo:
///   _currentStateIndex aponta para o SODialogue do array que será usado
///   na próxima interação. Nunca avança sozinho — só via SetState().
///
///   Após o diálogo terminar, o NPC memoriza o último bloco executado
///   (_repeatDialogue). Nas interações seguintes, repete esse bloco
///   em vez de voltar ao início do estado.
///
///   SetState() limpa o _repeatDialogue, forçando o NPC a executar
///   o novo estado desde o início.
///
/// Bifurcação:
///   Sistema externo chama SetState(índice) ao detectar a condição
///   (item entregue, puzzle resolvido, etc.).
/// </summary>
public class NPCInteractable : Interactable3D
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Identity")]
    public CharacterName characterName;

    [Header("Dialogue States")]
    [Tooltip("Array de SODialogue. Use SetState() para mudar o estado via evento externo.")]
    public SODialogue[] dialogueStates;

    [Header("Balloon Anchors")]
    [Tooltip("Posição world do balão do NPC (acima da cabeça).")]
    public Transform npcBalloonAnchor;

    [Tooltip("Posição world do balão do Player. Se vazio, usa PlayerCore + offset.")]
    public Transform playerBalloonAnchor;

    [Header("Flip")]
    [Tooltip("Transform do visual do NPC — usado para flip horizontal em direção ao player.")]
    public Transform graphic;

    [Header("Events")]
    public UnityEvent OnDialogueStarted;
    public UnityEvent OnDialogueFinished;

    // ── Private ───────────────────────────────────────────────────────────────

    [SerializeField] private int _currentStateIndex = 0;

    /// <summary>
    /// Último bloco executado. Null = ainda não conversou neste estado.
    /// Quando não é null, o NPC repete este bloco nas próximas interações.
    /// Resetado por SetState().
    /// </summary>
    private SODialogue _repeatDialogue;

    private bool _inDialogue;
    private bool _dialogueJustEnded;

    // ── Properties ────────────────────────────────────────────────────────────

    public int CurrentStateIndex => _currentStateIndex;

    /// <summary>
    /// Diálogo que será executado na próxima interação.
    /// Se já conversou neste estado, repete o último bloco.
    /// Caso contrário, começa pelo início do estado atual.
    /// </summary>
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

    // ── Interactable3D overrides ──────────────────────────────────────────────

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

        BeginDialogue();
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

    // ── Dialogue lifecycle ────────────────────────────────────────────────────

    private void BeginDialogue()
    {
        _inDialogue = true;

        PlayerCore.Instance?.SetMovementEnabled(false);
        PlayerCore.Instance?.SetInteractionEnabled(false);
        InteractionUIManager.Instance?.Hide(transform);

        DialogueManager.Instance.OnDialogueEnd.AddListener(HandleDialogueEnded);
        DialogueManager.Instance.StartDialogue(DialogueToPlay, BuildCharactersData());

        OnDialogueStarted?.Invoke();
    }

    private void HandleDialogueEnded()
    {
        DialogueManager.Instance.OnDialogueEnd.RemoveListener(HandleDialogueEnded);

        // Memoriza o último bloco executado para repetir nas próximas interações
        _repeatDialogue = DialogueManager.Instance.LastPlayedDialogue;

        _inDialogue = false;
        _dialogueJustEnded = true;
        StartCoroutine(ClearDialogueEndedFlag());

        PlayerCore.Instance?.SetMovementEnabled(true);
        PlayerCore.Instance?.SetInteractionEnabled(true);

        OnDialogueFinished?.Invoke();
    }

    private IEnumerator ClearDialogueEndedFlag()
    {
        yield return null;
        _dialogueJustEnded = false;
    }

    // ── State machine ─────────────────────────────────────────────────────────

    /// <summary>
    /// Muda o estado do NPC. Chamado por sistemas externos (puzzle, item, trigger).
    /// Limpa o _repeatDialogue para que o novo estado comece pelo início.
    /// </summary>
    public void SetState(int index)
    {
        if (dialogueStates == null || index < 0 || index >= dialogueStates.Length)
        {
            Debug.LogWarning($"[NPCInteractable] SetState({index}): índice inválido. " +
                             $"Total: {dialogueStates?.Length}");
            return;
        }

        _currentStateIndex = index;
        _repeatDialogue = null; // força o novo estado a começar do início
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