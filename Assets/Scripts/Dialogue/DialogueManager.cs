using Characters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable]
public struct CharacterDialogueData
{
    public CharacterName characterName;
    public Vector3 balloonPosition;

    public CharacterDialogueData(CharacterName name, Vector3 position)
    {
        characterName = name;
        balloonPosition = position;
    }
}

[System.Serializable]
public class CharacterBalloonEntry
{
    public CharacterName character;
    public DialogueBalloon balloonPrefab;
}

/// <summary>
/// Singleton de CENA (sem DontDestroyOnLoad).
/// Morre e renasce a cada cena de gameplay.
/// Lógica interna idêntica à versão anterior.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Balloon Prefabs")]
    public DialogueBalloon defaultBalloonPrefab;
    public List<CharacterBalloonEntry> balloonPrefabs = new();

    [Header("Events")]
    public UnityEvent OnDialogueStart;
    public UnityEvent OnDialogueEnd;

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsActive { get; private set; }
    public SODialogue LastPlayedDialogue { get; private set; }

    private SODialogue _currentDialogue;
    private List<CharacterDialogueData> _charactersData = new();
    private int _sentenceIndex;
    private DialogueBalloon _currentBalloon;
    private Coroutine _autoNextCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Sem DontDestroyOnLoad — singleton local de cena
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void StartDialogue(SODialogue dialogue, List<CharacterDialogueData> charactersData)
    {
        if (dialogue == null || dialogue.sentences.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] SODialogue nulo ou sem sentences.");
            return;
        }

        StopAutoNext();

        _currentDialogue = dialogue;
        _charactersData = new List<CharacterDialogueData>(charactersData);
        _sentenceIndex = 0;

        if (!IsActive)
        {
            IsActive = true;
            OnDialogueStart?.Invoke();
        }

        ShowCurrentSentence();
    }

    public void Advance()
    {
        if (!IsActive) return;

        if (_currentBalloon != null && _currentBalloon.IsTyping)
        {
            _currentBalloon.SkipTyping();
            return;
        }

        NextSentence();
    }

    public void EndDialogue()
    {
        StopAutoNext();
        LastPlayedDialogue = _currentDialogue;
        DestroyBalloon();
        IsActive = false;
        OnDialogueEnd?.Invoke();
    }

    // ── Input callbacks ───────────────────────────────────────────────────────

    public void OnDialogueNext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Advance();
    }

    public void OnDialogueSkip(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        EndDialogue();
    }

    // ── Internal flow ─────────────────────────────────────────────────────────

    private void ShowCurrentSentence()
    {
        Sentence sentence = _currentDialogue.sentences[_sentenceIndex];
        sentence.sentenceAction?.Execute();

        Vector3 pos = FindBalloonPosition(sentence.talker);
        PositionBalloon(pos, sentence.talker);
        _currentBalloon.UpdateText(sentence.sentenceText);
    }

    private void NextSentence()
    {
        if (_sentenceIndex < _currentDialogue.sentences.Length - 1)
        {
            _sentenceIndex++;
            ShowCurrentSentence();
            return;
        }

        if (_currentDialogue.nextDialogue != null)
        {
            _autoNextCoroutine = StartCoroutine(AutoNextRoutine(_currentDialogue.nextDialogue));
            return;
        }

        EndDialogue();
    }

    private IEnumerator AutoNextRoutine(SODialogue next)
    {
        yield return new WaitForSeconds(0.1f);
        StartDialogue(next, _charactersData);
    }

    private void StopAutoNext()
    {
        if (_autoNextCoroutine != null)
        {
            StopCoroutine(_autoNextCoroutine);
            _autoNextCoroutine = null;
        }
    }

    // ── Balloon ───────────────────────────────────────────────────────────────

    private void PositionBalloon(Vector3 worldPosition, CharacterName talker)
    {
        DestroyBalloon();
        DialogueBalloon prefab = GetPrefabFor(talker);
        _currentBalloon = Instantiate(prefab);
        _currentBalloon.transform.position = worldPosition;
    }

    private void DestroyBalloon()
    {
        if (_currentBalloon != null)
        {
            Destroy(_currentBalloon.gameObject);
            _currentBalloon = null;
        }
    }

    private DialogueBalloon GetPrefabFor(CharacterName talker)
    {
        foreach (var entry in balloonPrefabs)
            if (entry.character == talker && entry.balloonPrefab != null)
                return entry.balloonPrefab;

        return defaultBalloonPrefab;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector3 FindBalloonPosition(CharacterName talker)
    {
        foreach (var data in _charactersData)
            if (data.characterName == talker)
                return data.balloonPosition;

        return _charactersData.Count > 0 ? _charactersData[0].balloonPosition : Vector3.zero;
    }
}