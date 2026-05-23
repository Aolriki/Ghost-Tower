using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton de CENA (sem DontDestroyOnLoad).
/// Morre e renasce a cada cena de gameplay.
/// Lógica interna idêntica à versão anterior.
/// </summary>
public class CodePropInteractionManager : MonoBehaviour
{
    public static CodePropInteractionManager Instance { get; private set; }

    [Header("Camera Transition")]
    [Range(1f, 20f)]
    public float cameraMoveSpeed = 8f;

    [Header("Player Body")]
    public Renderer[] playerBodyRenderers;

    [Header("Cursors")]
    public CanvasGroup cursorLeft;
    public CanvasGroup cursorMiddle;
    public CanvasGroup cursorRight;

    public float cursorBlinkSpeed = 3f;

    [Header("HUD")]
    public GameObject commandsLegend;

    // Private

    private Transform _mainCameraTransform;
    private Vector3 _savedCamPosition;
    private Quaternion _savedCamRotation;
    private Coroutine _camMoveCoroutine;

    private CodeSlotController _activeController;
    private int _selectedSlot;
    private float _blinkPhase;
    private bool _isActive;

    private Vector2 _heldInput;
    private bool _inputHeld;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Sem DontDestroyOnLoad — singleton local de cena
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;

        HideAllCursors();
        if (commandsLegend != null) commandsLegend.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!_isActive) return;
        UpdateCursorBlink();

        if (_inputHeld && _activeController != null)
            _activeController.HandleNavigate(_heldInput, isHeld: true);
    }

    // ── Public API — Camera ───────────────────────────────────────────────────

    public void EnterPropView(Transform camAnchor)
    {
        if (_mainCameraTransform == null) return;

        if (camAnchor == null)
        {
            Debug.LogWarning("[CodePropInteractionManager] camAnchor não atribuído.");
            return;
        }

        _savedCamPosition = _mainCameraTransform.position;
        _savedCamRotation = _mainCameraTransform.rotation;

        SetCameraFollowEnabled(false);

        if (_camMoveCoroutine != null) StopCoroutine(_camMoveCoroutine);
        _camMoveCoroutine = StartCoroutine(MoveCameraTo(camAnchor.position, camAnchor.rotation));

        SetPlayerBodyVisible(false);
    }

    public void ExitPropView()
    {
        if (_mainCameraTransform == null) return;

        if (_camMoveCoroutine != null) StopCoroutine(_camMoveCoroutine);
        _camMoveCoroutine = StartCoroutine(
            MoveCameraTo(_savedCamPosition, _savedCamRotation,
                onComplete: () => SetCameraFollowEnabled(true))
        );

        SetPlayerBodyVisible(true);
    }

    // ── Public API — Controller ativo ────────────────────────────────────────

    public void Activate(CodeSlotController controller)
    {
        _activeController = controller;
        _selectedSlot = 0;
        _isActive = true;

        ResetCursorBlink();
        if (commandsLegend != null) commandsLegend.SetActive(true);
    }

    public void Deactivate()
    {
        _activeController = null;
        _isActive = false;
        _inputHeld = false;

        HideAllCursors();
        if (commandsLegend != null) commandsLegend.SetActive(false);
    }

    public void OnSlotChanged(int newSlot)
    {
        _selectedSlot = newSlot;
        ResetCursorBlink();
    }

    // ── Input Callbacks ───────────────────────────────────────────────────────

    public void OnCodeNavigate(InputAction.CallbackContext context)
    {
        if (!_isActive || _activeController == null) return;

        if (context.started)
        {
            _heldInput = context.ReadValue<Vector2>();
            _inputHeld = false;
            _activeController.HandleNavigate(_heldInput, isHeld: false);
        }
        else if (context.performed)
        {
            _heldInput = context.ReadValue<Vector2>();
            _inputHeld = true;
        }
        else if (context.canceled)
        {
            _heldInput = Vector2.zero;
            _inputHeld = false;
        }
    }

    public void OnCodeConfirm(InputAction.CallbackContext context)
    {
        if (!_isActive || !context.performed || _activeController == null) return;
        _activeController.HandleConfirm();
    }

    public void OnCodeExit(InputAction.CallbackContext context)
    {
        if (!_isActive || !context.performed || _activeController == null) return;
        _activeController.HandleExit();
    }

    // ── Camera internal ───────────────────────────────────────────────────────

    private IEnumerator MoveCameraTo(Vector3 targetPos, Quaternion targetRot,
                                     System.Action onComplete = null)
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
        foreach (var r in playerBodyRenderers)
            if (r != null) r.enabled = visible;
    }

    private void SetCameraFollowEnabled(bool enabled)
    {
        CameraFollow follow = _mainCameraTransform?.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = enabled;
    }

    // ── Cursor Blink internal ─────────────────────────────────────────────────

    private void UpdateCursorBlink()
    {
        _blinkPhase += Time.deltaTime * cursorBlinkSpeed;
        float alpha = (Mathf.Sin(_blinkPhase * Mathf.PI * 2f) + 1f) * 0.5f;

        if (cursorLeft != null) cursorLeft.alpha = _selectedSlot == 0 ? alpha : 0f;
        if (cursorMiddle != null) cursorMiddle.alpha = _selectedSlot == 1 ? alpha : 0f;
        if (cursorRight != null) cursorRight.alpha = _selectedSlot == 2 ? alpha : 0f;
    }

    private void ResetCursorBlink() => _blinkPhase = 0.25f;

    private void HideAllCursors()
    {
        if (cursorLeft != null) cursorLeft.alpha = 0f;
        if (cursorMiddle != null) cursorMiddle.alpha = 0f;
        if (cursorRight != null) cursorRight.alpha = 0f;
    }
}