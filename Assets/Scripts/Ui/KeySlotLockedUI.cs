using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Singleton de cena. Painel world space exibido pelo KeySlot quando isLocker rejeita uma interacao.
// Aparece instantaneamente, executa um shake, aguarda e some com fade.
// Compartilha o Canvas existente — ativa/desativa apenas o LockedPanel.
public class KeySlotLockedUI : MonoBehaviour
{
    public static KeySlotLockedUI Instance { get; private set; }

    [Header("References")]
    [Tooltip("Canvas world space compartilhado. Usado apenas para o billboard (LookAt).")]
    public Canvas canvas;

    [Tooltip("RectTransform do LockedPanel. O CanvasGroup e obtido automaticamente deste GameObject.")]
    public RectTransform panelRect;

    [Header("Shake Settings")]
    [Tooltip("Amplitude do shake em unidades locais.")]
    public float shakeAmplitude = 8f;

    [Tooltip("Frequencia do shake em ciclos por segundo.")]
    public float shakeFrequency = 18f;

    [Tooltip("Duracao total do shake.")]
    public float shakeDuration = 0.4f;

    [Header("Timing")]
    [Tooltip("Tempo visivel apos o shake antes de comecar o fade.")]
    public float holdDuration = 1f;

    [Tooltip("Duracao do fade de saida.")]
    public float fadeDuration = 0.5f;

    // ── Private ───────────────────────────────────────────────────────────────

    private Camera _mainCamera;
    private CanvasGroup _canvasGroup;
    private Coroutine _sequenceCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _mainCamera = Camera.main;

        // Obtem ou cria o CanvasGroup automaticamente no panelRect.
        if (panelRect != null)
        {
            _canvasGroup = panelRect.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = panelRect.gameObject.AddComponent<CanvasGroup>();
        }

        SetVisible(false, instant: true);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void LateUpdate()
    {
        if (canvas == null || panelRect == null || !panelRect.gameObject.activeSelf) return;

        // Sempre olha para a camera, igual ao InteractionUI.
        canvas.transform.LookAt(
            canvas.transform.position + _mainCamera.transform.rotation * Vector3.forward,
            _mainCamera.transform.rotation * Vector3.up
        );
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Exibe o painel na posicao indicada e inicia a sequencia: shake -> hold -> fade out.
    // Chamar Show() enquanto ja visivel reinicia a sequencia do inicio.
    public void Show(Vector3 worldPosition)
    {
        if (canvas != null)
            canvas.transform.position = worldPosition;

        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = StartCoroutine(ShowSequence());
    }

    // ── Sequencia ─────────────────────────────────────────────────────────────

    private IEnumerator ShowSequence()
    {
        SetVisible(true, instant: true);

        yield return StartCoroutine(ShakeRoutine());
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(FadeOutRoutine());

        SetVisible(false, instant: true);
    }

    private IEnumerator ShakeRoutine()
    {
        if (panelRect == null) yield break;

        Vector3 originalPos = panelRect.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Amplitude reduz conforme o shake termina (envelope linear).
            float envelope = 1f - (elapsed / shakeDuration);
            float offsetX = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2f) * shakeAmplitude * envelope;

            panelRect.localPosition = originalPos + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }

        panelRect.localPosition = originalPos;
    }

    private IEnumerator FadeOutRoutine()
    {
        if (_canvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    // Ativa/desativa apenas o LockedPanel, sem tocar no Canvas compartilhado.
    private void SetVisible(bool visible, bool instant = false)
    {
        if (panelRect == null) return;

        if (visible)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            panelRect.gameObject.SetActive(true);
        }
        else
        {
            panelRect.gameObject.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }
    }
}