using UnityEngine;
using UnityEngine.InputSystem;

// Segue o player como pivot, com pitch fixo e orbita em Y controlada pelo analogico direito.
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Position")]
    [Range(0f, 89f)]
    public float pitchAngle = 45f;
    public float distance = 12f;

    [Header("Look / Orbit")]
    public float lookRotateSpeed = 150f;
    public float mouseSensitivity = 0.5f; // NOVO: Sensibilidade separada para o mouse
    [Range(0f, 1f)]
    public float lookDeadzone = 0.15f;
    public float lookIdleDelay = 2f;
    [Range(0f, 1f)]
    public float moveStillThreshold = 0.05f;
    [Range(1f, 10f)]
    public float lookCorrectSpeed = 4f;
    public bool invertYawInput = false;

    // ---- State ----

    private Vector3 _focusPosition;

    private PlayerMovement _playerMovement;
    private Vector2 _lookInput;
    private float _yaw;
    private float _stillTimer;

    private bool _isMouseInput; // NOVO: Flag para identificar se o input atual vem do mouse

    [Header("Debug")]
    [SerializeField] private float _yawDebug;

    // ---- Unity ----

    void Awake()
    {
        if (player == null) return;
        _focusPosition = player.position;
    }

    void Start()
    {
        if (player == null) return;

        _yaw = player.eulerAngles.y;
        _playerMovement = player.GetComponentInParent<PlayerMovement>();

        if (_playerMovement == null)
            Debug.LogWarning("[CameraFollow] PlayerMovement nao encontrado em player nem nos pais. Correcao automatica de yaw ficara desativada.");
    }

    void LateUpdate()
    {
        if (player == null) return;

        UpdateFocus();
        UpdateOrbit();
        MoveCamera();
    }

    // ---- Input ----
    // Callback de Input (chamado pelo PlayerInput em modo Invoke Unity Events).
    // Registre este metodo no evento Look do PlayerInput no Inspector.

    public void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
        // NOVO: Verifica dinamicamente se o dispositivo utilizado na ação é um mouse
        _isMouseInput = context.control.device is Mouse;
    }

    // ---- Camera ----

    // Calcula o offset de posicao a partir do pitch, da distancia e do yaw atual.
    private Vector3 ComputeOffset()
    {
        float pitchRad = pitchAngle * Mathf.Deg2Rad;
        float horizontal = Mathf.Cos(pitchRad);
        float vertical = Mathf.Sin(pitchRad);

        Vector3 baseOffset = new Vector3(0f, vertical, -horizontal) * distance;
        return Quaternion.Euler(0f, _yaw, 0f) * baseOffset;
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

    // ---- Look / Orbit ----

    // Analogico direito controla o yaw, sempre em torno de Y (pitch e roll nunca sao alterados por input).
    // A correcao automatica so acontece com o player parado (sem input de movimento) por lookIdleDelay
    // segundos seguidos, para nao girar a camera enquanto o player esta andando.
    private void UpdateOrbit()
    {
        // NOVO: Removemos a deadzone caso o input seja do mouse para não perder movimentos sutis
        float x = _lookInput.x;
        if (!_isMouseInput && Mathf.Abs(x) < lookDeadzone)
        {
            x = 0f;
        }

        if (invertYawInput)
            x = -x;

        bool isMoving = _playerMovement != null && _playerMovement.MoveInput.sqrMagnitude > moveStillThreshold * moveStillThreshold;

        if (!Mathf.Approximately(x, 0f))
        {
            // Controle manual sempre tem prioridade, parado ou andando.

            // NOVO: Cálculo diferenciado dependendo do dispositivo
            if (_isMouseInput)
            {
                // Mouse já entrega um Delta (deslocamento), não precisa de Time.deltaTime
                _yaw += x * mouseSensitivity;
            }
            else
            {
                // Analógico é um valor contínuo, exige Time.deltaTime
                _yaw += x * lookRotateSpeed * Time.deltaTime;
            }

            _stillTimer = 0f;
        }
        else if (isMoving)
        {
            // Andando: nunca corrige, e reinicia a contagem de parado.
            _stillTimer = 0f;
        }
        else
        {
            // Parado e sem input manual: acumula tempo parado antes de corrigir.
            _stillTimer += Time.deltaTime;

            if (_stillTimer >= lookIdleDelay)
            {
                float targetYaw = player.eulerAngles.y;
                float t = 1f - Mathf.Exp(-lookCorrectSpeed * Time.deltaTime);
                _yaw = Mathf.LerpAngle(_yaw, targetYaw, t);

                if (Mathf.Abs(Mathf.DeltaAngle(_yaw, targetYaw)) < 0.05f)
                    _yaw = targetYaw;
            }
        }

        _yawDebug = _yaw;
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
    }
}