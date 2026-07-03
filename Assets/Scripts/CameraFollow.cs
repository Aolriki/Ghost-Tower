using UnityEngine;
using System.Collections.Generic;

// Segue o player com angulo de pitch e distancia configuraveis como pivot.
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Position")]
    [Range(0f, 89f)]
    public float pitchAngle = 45f;
    public float distance = 12f;

    [Header("Wall Camera Fade")]
    public string wallFadeProperty = "_CameraFade";
    [Range(0f, 1f)]
    public float wallFadeMax = 0.85f;
    [Range(1f, 20f)]
    public float wallFadeSpeed = 6f;

    // ---- State ----

    private Vector3 _focusPosition;

    private Dictionary<Renderer, FadeState> _wallStates = new Dictionary<Renderer, FadeState>();
    private HashSet<Renderer> _blocking = new HashSet<Renderer>();

    private MaterialPropertyBlock _propBlock;
    private int _fadePropertyID;

    private class FadeState
    {
        public float currentFade;
        public float targetFade;
    }

    // ---- Unity ----

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _fadePropertyID = Shader.PropertyToID(wallFadeProperty);

        if (player == null) return;
        _focusPosition = player.position;
    }

    void Start()
    {
        PrewarmWallStates();
    }

    void LateUpdate()
    {
        if (player == null) return;

        UpdateFocus();
        MoveCamera();
        UpdateWallVisibility();
    }

    // ---- Camera ----

    // Calcula o offset de posicao a partir do pitch e da distancia.
    private Vector3 ComputeOffset()
    {
        float pitchRad = pitchAngle * Mathf.Deg2Rad;
        float horizontal = Mathf.Cos(pitchRad);
        float vertical = Mathf.Sin(pitchRad);
        return new Vector3(0f, vertical, -horizontal) * distance;
    }

    private void UpdateFocus()
    {
        _focusPosition = player.position;
    }

    private void MoveCamera()
    {
        transform.position = _focusPosition + ComputeOffset();
        transform.LookAt(_focusPosition);
    }

    // ---- Wall Camera Fade ----

    private void PrewarmWallStates()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Wall"))
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r == null || _wallStates.ContainsKey(r)) continue;

            _wallStates[r] = new FadeState
            {
                currentFade = 0f,
                targetFade = 0f
            };
        }
    }

    private void UpdateWallVisibility()
    {
        Vector3 origin = transform.position;
        Vector3 direction = _focusPosition - origin;
        float dist = direction.magnitude;

        _blocking.Clear();
        foreach (RaycastHit hit in Physics.RaycastAll(origin, direction.normalized, dist))
        {
            if (!hit.collider.CompareTag("Wall")) continue;
            Renderer r = hit.collider.GetComponent<Renderer>();
            if (r != null && _wallStates.ContainsKey(r))
                _blocking.Add(r);
        }

        foreach (var pair in _wallStates)
            pair.Value.targetFade = _blocking.Contains(pair.Key) ? wallFadeMax : 0f;

        float t = 1f - Mathf.Exp(-wallFadeSpeed * Time.deltaTime);
        foreach (var pair in _wallStates)
        {
            FadeState state = pair.Value;
            if (Mathf.Approximately(state.currentFade, state.targetFade)) continue;

            state.currentFade = Mathf.Lerp(state.currentFade, state.targetFade, t);

            if (Mathf.Abs(state.currentFade - state.targetFade) < 0.005f)
                state.currentFade = state.targetFade;

            // Aplica via MaterialPropertyBlock para nao instanciar material por objeto.
            // O material asset continua compartilhado, so o valor de _CameraFade
            // fica overridado por renderer individualmente.
            pair.Key.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_fadePropertyID, state.currentFade);
            pair.Key.SetPropertyBlock(_propBlock);
        }
    }

    // ---- Editor Preview ----

    // Move o transform no editor sempre que um campo muda no Inspector.
    void OnValidate()
    {
        if (Application.isPlaying || player == null) return;
        transform.position = player.position + ComputeOffset();
        transform.LookAt(player.position);
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector3 pivot = Application.isPlaying ? _focusPosition : player.position;
        Vector3 camPos = pivot + ComputeOffset();

        // Linha da camera ao pivot
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(camPos, pivot);
        Gizmos.DrawWireSphere(pivot, 0.2f);

        // Posicao calculada da camera (util quando nao esta em play)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(camPos, 0.15f);

        if (!Application.isPlaying) return;
    }
}