using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class BallTrackingIndicator : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Transform do Player")]
    public Transform playerTransform;

    [Header("Distance Settings")]
    [Tooltip("Abaixo disso → estado Alert")]
    public float minDistance = 3f;
    [Tooltip("Acima disso → estado Invisible")]
    public float maxDistance = 10f;

    [Header("Alert Animation")]
    [Tooltip("Duração de ida e volta do loop (segundos)")]
    public float alertBlinkDuration = 0.5f;

    // ─── Blend Shape Indices ──────────────────────────────────────
    private const int BS_INCREASE = 0;   // "Increase"
    private const int BS_INCREASE2 = 1;   // "Increase2"

    // ─── Internals ────────────────────────────────────────────────
    private SkinnedMeshRenderer _smr;
    private Transform _ballTransform;

    private enum State { Invisible, Tracking, Alert }
    private State _currentState = State.Invisible;

    private float _alertTimer;
    private bool _alertGoingUp = true;

    // ─── Lifecycle ────────────────────────────────────────────────
    private void Awake()
    {
        _smr = GetComponent<SkinnedMeshRenderer>();
    }

    private void Start()
    {
        SetRendererVisible(false);
    }

    private void Update()
    {
        // Aguarda instância da bola
        if (Volleyball.Instance == null)
        {
            SetState(State.Invisible);
            return;
        }

        if (_ballTransform == null)
            _ballTransform = Volleyball.Instance.BallTransform;

        float dist = Vector3.Distance(playerTransform.position, _ballTransform.position);
        UpdateState(dist);
        UpdateRotation();
    }

    // ─── State Machine ────────────────────────────────────────────
    private void UpdateState(float dist)
    {
        if (dist > maxDistance)
        {
            SetState(State.Invisible);
        }
        else if (dist >= minDistance)
        {
            SetState(State.Tracking);
            float t = Mathf.InverseLerp(maxDistance, minDistance, dist); // 0 → 1
            float blendValue = Mathf.Lerp(1f, 100f, t);
            _smr.SetBlendShapeWeight(BS_INCREASE, blendValue);
        }
        else // dist < minDistance
        {
            SetState(State.Alert);
            _smr.SetBlendShapeWeight(BS_INCREASE, 100f);
            UpdateAlertBlink();
        }
    }

    private void SetState(State newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;

        switch (newState)
        {
            case State.Invisible:
                SetRendererVisible(false);
                ResetBlendShapes();
                break;

            case State.Tracking:
                SetRendererVisible(true);
                _smr.SetBlendShapeWeight(BS_INCREASE2, 0f);
                break;

            case State.Alert:
                SetRendererVisible(true);
                _alertTimer = 0f;
                _alertGoingUp = true;
                break;
        }
    }

    // ─── Alert Blink ─────────────────────────────────────────────
    private void UpdateAlertBlink()
    {
        _alertTimer += Time.deltaTime;
        float halfDuration = alertBlinkDuration * 0.5f;

        // Normaliza 0→1 dentro do meio ciclo
        float t = Mathf.Clamp01(_alertTimer / halfDuration);

        float blendValue = _alertGoingUp
            ? Mathf.Lerp(50f, 100f, t)
            : Mathf.Lerp(100f, 50f, t);

        _smr.SetBlendShapeWeight(BS_INCREASE2, blendValue);

        if (_alertTimer >= halfDuration)
        {
            _alertTimer = 0f;
            _alertGoingUp = !_alertGoingUp;
        }
    }

    // ─── Rotation (XZ plane, Y locked) ───────────────────────────
    private void UpdateRotation()
    {
        if (_ballTransform == null || playerTransform == null) return;

        Vector3 ballPos = _ballTransform.position;
        Vector3 playerPos = playerTransform.position;

        // Direção horizontal: X (esquerda/direita) e Y (frente/costas)
        // Ignora Z (cima/baixo) pois é o eixo de altura no seu espaço
        Vector3 worldDir = new Vector3(
            ballPos.x - playerPos.x,
            ballPos.y - playerPos.y, // frente/costas
            0f                       // ignora diferença de altura (Z)
        );

        if (worldDir.sqrMagnitude < 0.001f) return;

        // Converte para espaço local do pai
        Vector3 localDir = transform.parent != null
            ? transform.parent.InverseTransformDirection(worldDir)
            : worldDir;

        // No seu espaço: plano horizontal é XY → ângulo no eixo Z local
        // Mas queremos rotação SOMENTE no eixo Y → Atan2 entre X e Y
        float angle = Mathf.Atan2(localDir.x, localDir.y) * Mathf.Rad2Deg;

        // Aplica SOMENTE no Y — X e Z permanecem zerados
        transform.localEulerAngles = new Vector3(0f, angle, 0f);
    }
    // ─── Helpers ─────────────────────────────────────────────────
    private void SetRendererVisible(bool visible)
    {
        _smr.enabled = visible;
    }

    private void ResetBlendShapes()
    {
        _smr.SetBlendShapeWeight(BS_INCREASE, 0f);
        _smr.SetBlendShapeWeight(BS_INCREASE2, 0f);
    }
}