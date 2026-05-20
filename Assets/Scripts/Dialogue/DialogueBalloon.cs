using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Balão de fala com efeito de digitação (typewriter).
/// Redimensiona automaticamente usando um TMP invisível como régua.
///
/// Vive como prefab instanciado pelo DialogueManager.
/// O DialogueManager reposiciona este GameObject a cada frase,
/// colocando-o na posição world do personagem que está falando.
/// </summary>
public class DialogueBalloon : MonoBehaviour
{
    [Header("References")]
    [Tooltip("RectTransform raiz do balão visual (para ForceRebuildLayout).")]
    public RectTransform speechBubbleRect;

    [Tooltip("TMP invisível usado apenas para medir o tamanho do texto.")]
    public TextMeshProUGUI invisTMP;

    [Tooltip("TMP visível onde o typewriter escreve.")]
    public TextMeshProUGUI sentenceTMP;

    [Header("Settings")]
    public float typingSpeed = 0.04f;
    public float maxWidth = 750f;

    // ── State ─────────────────────────────────────────────────────────────────

    [HideInInspector] public string currentSentenceText;
    [HideInInspector] public UnityEvent OnTypingOver = new();

    private RectTransform _invisRect;
    private Coroutine _typingCoroutine;
    private Camera _mainCamera;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        _invisRect = invisTMP.rectTransform;
        _mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (_mainCamera == null) return;
        // Orienta o canvas para a câmera, igual ao InteractionUIManager
        transform.LookAt(
            transform.position + _mainCamera.transform.rotation * Vector3.forward,
            _mainCamera.transform.rotation * Vector3.up
        );
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Exibe um novo texto com efeito de digitação.</summary>
    public void UpdateText(string text)
    {
        currentSentenceText = text;
        invisTMP.text = text;
        ResizeBubble();

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeRoutine());
    }

    /// <summary>
    /// Completa instantaneamente a frase em andamento (skip).
    /// Chamado pelo DialogueManager quando o jogador pressiona enquanto digita.
    /// </summary>
    public void SkipTyping()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        sentenceTMP.text = currentSentenceText;
    }

    /// <summary>True enquanto o typewriter ainda está digitando.</summary>
    public bool IsTyping => sentenceTMP.text != currentSentenceText;

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ResizeBubble()
    {
        invisTMP.ForceMeshUpdate();

        float width = Mathf.Min(
            invisTMP.GetPreferredValues(currentSentenceText).x,
            maxWidth
        );
        _invisRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        float height = invisTMP.GetPreferredValues(currentSentenceText, width, 0).y;
        _invisRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        LayoutRebuilder.ForceRebuildLayoutImmediate(speechBubbleRect);
    }

    private IEnumerator TypeRoutine()
    {
        sentenceTMP.text = "";
        foreach (char c in currentSentenceText)
        {
            sentenceTMP.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        OnTypingOver?.Invoke();
    }
}