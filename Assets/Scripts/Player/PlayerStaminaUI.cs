using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    // Configuração

    [Header("UI References")]
    public Slider       staminaSlider;
    public CanvasGroup  staminaCanvasGroup;

    [Header("Billboard Settings")]
    public Transform staminaUIRoot;   // World Space Canvas
    public float     uiHeightOffset = 2f;

    [Header("Fade Settings")]
    public float fadeSpeed    = 2f;
    public float fullStaminaHideDelay = 1f; // segundos com stamina cheia antes de esconder

    // Privados

    private PlayerMovement _movement;
    private float _timeFullStamina;

    // Inicialização

    void Awake()
    {
        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
            staminaSlider.value    = 1f;
        }

        if (staminaCanvasGroup != null)
            staminaCanvasGroup.alpha = 0f;
    }

    void Start()
    {
        // Busca o módulo de movimento pelo singleton após todos os Awakes
        if (PlayerCore.Instance != null)
            _movement = PlayerCore.Instance.Movement;

        if (_movement == null)
            Debug.LogWarning("[PlayerStaminaUI] PlayerMovement não encontrado via PlayerCore.");
    }

    // Update

    void Update()
    {
        if (_movement == null) return;
        UpdateSlider();
        UpdateFade();
    }

    void LateUpdate()
    {
        UpdateBillboard();
    }

    // Lógica interna


    private void UpdateSlider()
    {
        if (staminaSlider == null) return;
        staminaSlider.value = _movement.Stamina / _movement.MaxStamina;
    }

    private void UpdateFade()
    {
        if (staminaCanvasGroup == null) return;

        bool isFull = _movement.Stamina >= _movement.MaxStamina;

        if (!isFull || _movement.IsRunning)
            _timeFullStamina = 0f;
        else
            _timeFullStamina += Time.deltaTime;

        float targetAlpha = _timeFullStamina >= fullStaminaHideDelay ? 0f : 1f;

        staminaCanvasGroup.alpha = Mathf.MoveTowards(
            staminaCanvasGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime
        );
    }

    private void UpdateBillboard()
    {
        if (staminaUIRoot == null) return;

        Vector3 targetPos = transform.position + Vector3.up * uiHeightOffset;

        staminaUIRoot.position = targetPos;

        if (Camera.main != null)
            staminaUIRoot.rotation = Camera.main.transform.rotation;
    }
}
