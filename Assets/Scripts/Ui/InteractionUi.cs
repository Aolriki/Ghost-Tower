using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Singleton de CENA (sem DontDestroyOnLoad).
/// Morre e renasce a cada cena de gameplay.
/// Props e NPCs continuam acessando via InteractionUIManager.Instance —
/// a diferença é que a instância agora é local da cena.
/// </summary>
public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [Header("World Space UI")]
    public Canvas interactionCanvas;
    public Image interactionIcon;

    private Coroutine floatCoroutine;
    private Coroutine fadeCoroutine;
    private Camera mainCamera;
    private Transform currentTarget;
    private Vector3 currentOffset;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Sem DontDestroyOnLoad — singleton local de cena
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        mainCamera = Camera.main;

        if (interactionCanvas != null)
        {
            interactionCanvas.renderMode = RenderMode.WorldSpace;
            interactionCanvas.worldCamera = mainCamera;
            SetCanvasVisible(false, instant: true);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ShowAt(Transform target, Vector3 uiOffset)
    {
        currentTarget = target;
        currentOffset = uiOffset;
        interactionCanvas.transform.position = target.position + uiOffset;
        SetCanvasVisible(true);
        StartFloatEffect(uiOffset);
    }

    public void Hide(Transform target)
    {
        if (currentTarget != target) return;
        currentTarget = null;
        SetCanvasVisible(false);
        StopFloatEffect();
    }

    // ── LateUpdate ────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (interactionCanvas == null || !interactionCanvas.gameObject.activeSelf) return;

        interactionCanvas.transform.LookAt(
            interactionCanvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
            mainCamera.transform.rotation * Vector3.up
        );
    }

    // ── Float ─────────────────────────────────────────────────────────────────

    private void StartFloatEffect(Vector3 uiOffset)
    {
        StopFloatEffect();
        floatCoroutine = StartCoroutine(FloatLoop(uiOffset));
    }

    private void StopFloatEffect()
    {
        if (floatCoroutine != null) { StopCoroutine(floatCoroutine); floatCoroutine = null; }
    }

    private IEnumerator FloatLoop(Vector3 uiOffset)
    {
        float amplitude = 0.15f;
        Vector3 baseOffset = uiOffset;
        Vector3 topOffset = uiOffset + Vector3.up * amplitude;
        while (true)
        {
            yield return FloatStep(baseOffset, topOffset, 0.9f);
            yield return FloatStep(topOffset, baseOffset, 0.9f);
        }
    }

    private IEnumerator FloatStep(Vector3 fromOffset, Vector3 toOffset, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (currentTarget == null) yield break;
            float ease = Mathf.SmoothStep(0f, 1f, t / duration);
            interactionCanvas.transform.position = currentTarget.position + Vector3.Lerp(fromOffset, toOffset, ease);
            yield return null;
        }
    }

    // ── Fade ──────────────────────────────────────────────────────────────────

    private void SetCanvasVisible(bool visible, bool instant = false)
    {
        if (interactionCanvas == null) return;
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }

        if (visible)
        {
            interactionCanvas.gameObject.SetActive(true);
            if (instant) SetIconAlpha(1f);
            else fadeCoroutine = StartCoroutine(FadeIcon(1f, 0.2f));
        }
        else
        {
            if (instant)
            {
                SetIconAlpha(0f);
                interactionCanvas.gameObject.SetActive(false);
            }
            else
            {
                fadeCoroutine = StartCoroutine(
                    FadeIcon(0f, 0.15f, () => interactionCanvas.gameObject.SetActive(false))
                );
            }
        }
    }

    private IEnumerator FadeIcon(float target, float duration, System.Action onComplete = null)
    {
        if (interactionIcon == null) yield break;
        Color c = interactionIcon.color;
        float start = c.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, target, t / duration);
            interactionIcon.color = c;
            yield return null;
        }
        c.a = target;
        interactionIcon.color = c;
        onComplete?.Invoke();
    }

    private void SetIconAlpha(float alpha)
    {
        if (interactionIcon == null) return;
        Color c = interactionIcon.color;
        c.a = alpha;
        interactionIcon.color = c;
    }
}