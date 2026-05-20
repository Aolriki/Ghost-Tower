using System.Collections;
using UnityEngine;

/// <summary>
/// Gerencia a transição de cena ao abrir/fechar um CodeProp:
///   • Move a câmera para um ponto de vista dedicado do prop
///   • Oculta o corpo da personagem
///   • Restaura tudo ao fechar
///
/// É um singleton leve. Vive no mesmo GameObject do CodePropController
/// ou em um GameObject separado na cena.
/// </summary>
public class CodePropInteractionManager : MonoBehaviour
{
    public static CodePropInteractionManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Camera Transition")]
    [Tooltip("Transform para onde a câmera vai durante a interação com o CodeProp.\n" +
             "Crie um GameObject vazio na cena chamado 'CodePropCamAnchor' e posicione-o.")]
    public Transform codePropCameraAnchor;

    [Tooltip("Velocidade de interpolação da câmera (Lerp por frame).")]
    [Range(1f, 20f)]
    public float cameraMoveSpeed = 8f;

    [Header("Player Body")]
    [Tooltip("Renderer(s) do corpo da personagem que serão ocultados durante a interação.")]
    public Renderer[] playerBodyRenderers;

    // ── Private ───────────────────────────────────────────────────────────────

    private Transform _mainCameraTransform;
    private Vector3   _savedCamPosition;
    private Quaternion _savedCamRotation;

    private Coroutine _camMoveCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Prepara a cena para a interação:
    /// salva a câmera, move para o anchor e oculta a personagem.
    /// </summary>
    public void EnterPropView(Transform propAnchorOverride = null)
    {
        if (_mainCameraTransform == null) return;

        // Salva estado atual da câmera
        _savedCamPosition = _mainCameraTransform.position;
        _savedCamRotation = _mainCameraTransform.rotation;

        // Desabilita o CameraFollow para não brigar com o Lerp
        SetCameraFollowEnabled(false);

        // Move a câmera para o anchor
        Transform target = propAnchorOverride != null ? propAnchorOverride : codePropCameraAnchor;
        if (target != null)
        {
            if (_camMoveCoroutine != null) StopCoroutine(_camMoveCoroutine);
            _camMoveCoroutine = StartCoroutine(MoveCameraTo(target.position, target.rotation));
        }

        // Oculta corpo da personagem
        SetPlayerBodyVisible(false);
    }

    /// <summary>
    /// Restaura a cena ao estado de Gameplay:
    /// câmera volta para a posição salva e o corpo reaparece.
    /// </summary>
    public void ExitPropView()
    {
        if (_mainCameraTransform == null) return;

        if (_camMoveCoroutine != null) StopCoroutine(_camMoveCoroutine);
        _camMoveCoroutine = StartCoroutine(
            MoveCameraTo(_savedCamPosition, _savedCamRotation, onComplete: () =>
            {
                SetCameraFollowEnabled(true);
            })
        );

        SetPlayerBodyVisible(true);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

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
        // CameraFollow vive no mesmo GameObject que Camera.main normalmente
        CameraFollow follow = _mainCameraTransform?.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = enabled;
    }
}
