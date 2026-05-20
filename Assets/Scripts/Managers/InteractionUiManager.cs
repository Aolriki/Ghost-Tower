using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("World Space UI")]
    public Canvas interactionCanvas;
    public Image interactionIcon;

    private Coroutine floatCoroutine;
    private Coroutine fadeCoroutine;
    private Camera mainCamera;
    private Transform currentTarget;

    private Vector3 currentOffset;


    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        mainCamera = Camera.main;

        if (interactionCanvas != null)
        {
            interactionCanvas.renderMode = RenderMode.WorldSpace;
            interactionCanvas.worldCamera = mainCamera;
            SetCanvasVisible(false, instant: true);
        }
    }

    public void ShowAt(Transform target, Vector3 uiOffset)
    {
        currentTarget = target;
        currentOffset = uiOffset; //salva o offset
        interactionCanvas.transform.position = target.position + uiOffset;
        SetCanvasVisible(true);
        StartFloatEffect(uiOffset);
    }

    private void LateUpdate()
    {
        if (interactionCanvas == null || !interactionCanvas.gameObject.activeSelf) return;

        // O FloatStep já cuida da posição enquanto anima
        // LateUpdate só precisa cuidar da rotação
        interactionCanvas.transform.LookAt(
            interactionCanvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
            mainCamera.transform.rotation * Vector3.up
        );
    }



    // Chamado pelo prop quando o jogador sai do range
    public void Hide(Transform target)
    {
        // Só esconde se o pedido vem do prop que está ativo no momento
        if (currentTarget != target) return;
        currentTarget = null;
        SetCanvasVisible(false);
        StopFloatEffect();
    }

    //Float

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
            // Aplica o offset SOMADO à posição world do target
            interactionCanvas.transform.position = currentTarget.position + Vector3.Lerp(fromOffset, toOffset, ease);
            yield return null;
        }
    }

    //Fade

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