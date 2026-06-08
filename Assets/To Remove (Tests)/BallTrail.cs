using UnityEngine;

/// <summary>
/// Controla apenas o comportamento dinâmico do rastro em runtime.
/// Cores e configurações são injetadas pelo BallTrailSetup no Awake.
/// Largura é configurada diretamente pela curva do TrailRenderer no Inspector.
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class BallTrail : MonoBehaviour
{
    // Injetados pelo BallTrailSetup — não aparecem no Inspector
    [HideInInspector] public Color headColor;
    [HideInInspector] public Color bodyColor;
    [HideInInspector] public Color tailColor;
    [HideInInspector] public float minTrailTime;
    [HideInInspector] public float maxTrailTime;
    [HideInInspector] public float maxSpeedReference;
    [HideInInspector] public float smoothing;

    // Referências internas
    private TrailRenderer trail;
    private Rigidbody rb;
    private Rigidbody2D rb2d;
    private bool is2D;
    private float currentTrailTime;
    private Vector3 _prevPos = Vector3.zero;

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        rb = GetComponent<Rigidbody>();
        rb2d = GetComponent<Rigidbody2D>();
        is2D = rb2d != null;

        currentTrailTime = minTrailTime;
        trail.time = currentTrailTime;

        ApplyGradient();
    }

    void Update()
    {
        float speed = GetSpeed();
        float targetTime = Mathf.Lerp(minTrailTime, maxTrailTime, speed / Mathf.Max(maxSpeedReference, 0.01f));
        currentTrailTime = Mathf.Lerp(currentTrailTime, targetTime, Time.deltaTime * smoothing);
        trail.time = currentTrailTime;
    }

    // ─────────────────────────────────────────────────────────────
    public void ApplyGradient()
    {
        if (trail == null) trail = GetComponent<TrailRenderer>();

        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[3]
        {
            new GradientColorKey(headColor, 0f),
            new GradientColorKey(bodyColor, 0.45f),
            new GradientColorKey(tailColor, 1f)
        };

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3]
        {
            new GradientAlphaKey(headColor.a, 0f),
            new GradientAlphaKey(bodyColor.a, 0.45f),
            new GradientAlphaKey(tailColor.a, 1f)
        };

        gradient.SetKeys(colorKeys, alphaKeys);
        trail.colorGradient = gradient;
    }

    // ─────────────────────────────────────────────────────────────
    float GetSpeed()
    {
        if (is2D && rb2d != null) return rb2d.linearVelocity.magnitude;
        if (!is2D && rb != null) return rb.linearVelocity.magnitude;

        // Sem Rigidbody: calcula pela posição (objetos cinemáticos)
        if (_prevPos == Vector3.zero) _prevPos = transform.position;
        float spd = (transform.position - _prevPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _prevPos = transform.position;
        return spd;
    }
}