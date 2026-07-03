using System.Collections;
using UnityEngine;

// Coordenador de cena para observacao de props: move a camera, esconde o player e roteia input para o LookProp ativo.
public class LookMode : MonoBehaviour
{
    public static LookMode Instance { get; private set; }

    [Header("Camera Transition")]
    [Range(1f, 20f)]
    public float cameraMoveSpeed = 8f;

    [Header("Player Body")]
    [Tooltip("Renderers do corpo da personagem ocultados durante a observacao.")]
    public Renderer[] playerBodyRenderers;

    public LookProp ActiveProp { get; private set; }
    public bool IsActive => ActiveProp != null;

    private Transform _mainCameraTransform;
    private Vector3 _savedCamPosition;
    private Quaternion _savedCamRotation;
    private Coroutine _camMoveCoroutine;

    [Header("Navigation")]
    [Tooltip("Intervalo entre repeticoes de navegacao enquanto o input esta segurado (segundos).")]
    public float navigateRepeatDelay = 0.2f;

    private Vector2 _heldInput;
    private bool _inputHeld;
    private float _repeatTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!IsActive) return;

        if (_inputHeld)
        {
            _repeatTimer -= Time.deltaTime;
            if (_repeatTimer <= 0f)
            {
                _repeatTimer = navigateRepeatDelay;
                ActiveProp?.OnLookNavigate(_heldInput);
            }
        }
    }

    // ── Entrada e saida ────────────────────────────────────────────────────────

    public void Enter(LookProp prop)
    {
        if (prop == null || _mainCameraTransform == null) return;

        if (prop.CamAnchor == null)
        {
            Debug.LogWarning($"[LookMode] {prop.name}: CamAnchor nao atribuido.");
            return;
        }

        ActiveProp = prop;
        _inputHeld = false;

        PlayerCore.Instance?.SetMovementEnabled(false);
        PlayerCore.Instance?.SetInteractionEnabled(false);
        InteractionUI.Instance?.Hide(prop.transform);

        _savedCamPosition = _mainCameraTransform.position;
        _savedCamRotation = _mainCameraTransform.rotation;

        SetCameraFollowEnabled(false);
        SetPlayerBodyVisible(false);

        ScreenManager.Instance?.ChangeScreen(Screens.Look);

        if (_camMoveCoroutine != null) StopCoroutine(_camMoveCoroutine);
        _camMoveCoroutine = StartCoroutine(MoveCameraTo(prop.CamAnchor.position, prop.CamAnchor.rotation));

        prop.OnEnterLook();
    }

    public void Exit()
    {
        if (ActiveProp == null) return;

        LookProp prop = ActiveProp;
        ActiveProp = null;
        _inputHeld = false;

        if (_camMoveCoroutine != null) StopCoroutine(_camMoveCoroutine);
        _camMoveCoroutine = StartCoroutine(
            MoveCameraTo(_savedCamPosition, _savedCamRotation,
                onComplete: () => SetCameraFollowEnabled(true))
        );

        SetPlayerBodyVisible(true);

        PlayerCore.Instance?.SetMovementEnabled(true);
        PlayerCore.Instance?.SetInteractionEnabled(true);

        ScreenManager.Instance?.ChangeScreen(Screens.Gameplay);

        prop.OnExitLook();
    }

    // ── Input repassado pelo UIInputRouter ────────────────────────────────────

    // Primeiro toque: dispara uma vez e inicia o timer de hold.
    public void NavigateStarted(Vector2 input)
    {
        if (!IsActive) return;
        _heldInput = input;
        _inputHeld = true;
        _repeatTimer = navigateRepeatDelay;
        ActiveProp?.OnLookNavigate(input);
    }

    // performed chega logo apos started — apenas atualiza o valor, nao dispara de novo.
    public void NavigateHeld(Vector2 input)
    {
        if (!IsActive) return;
        _heldInput = input;
        // Nao chama OnLookNavigate aqui. O Update cuida do repeat apos o delay.
    }

    public void NavigateReleased()
    {
        _heldInput = Vector2.zero;
        _inputHeld = false;
        _repeatTimer = 0f;
    }

    public void Confirm()
    {
        ActiveProp?.OnLookConfirm();
    }

    public void Cancel()
    {
        ActiveProp?.OnLookCancel();
    }

    // ── Camera ─────────────────────────────────────────────────────────────────

    private IEnumerator MoveCameraTo(Vector3 targetPos, Quaternion targetRot, System.Action onComplete = null)
    {
        while (Vector3.Distance(_mainCameraTransform.position, targetPos) > 0.01f)
        {
            float t = 1f - Mathf.Exp(-cameraMoveSpeed * Time.deltaTime);
            _mainCameraTransform.position = Vector3.Lerp(_mainCameraTransform.position, targetPos, t);
            _mainCameraTransform.rotation = Quaternion.Slerp(_mainCameraTransform.rotation, targetRot, t);
            yield return null;
        }

        _mainCameraTransform.position = targetPos;
        _mainCameraTransform.rotation = targetRot;
        onComplete?.Invoke();
    }

    private void SetPlayerBodyVisible(bool visible)
    {
        if (playerBodyRenderers == null) return;
        foreach (var r in playerBodyRenderers)
            if (r != null) r.enabled = visible;
    }

    private void SetCameraFollowEnabled(bool enabled)
    {
        CameraFollow follow = _mainCameraTransform?.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = enabled;
    }
}