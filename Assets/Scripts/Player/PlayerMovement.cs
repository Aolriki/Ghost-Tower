using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Configuração

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float addRunSpeed = 3f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRecoverRate = 15f;
    public float sprintCooldown = 1f;

    // Estado público (lido por PlayerStaminaUI e outros sistemas)

    public float Stamina => _stamina;
    public float MaxStamina => maxStamina;
    public bool IsRunning => _isRunning;
    public Vector2 MoveInput { get; private set; }

    // Debug


    [Header("Debug")]
    [SerializeField] private float _currentSpeedDebug;

    // Privados

    private CharacterController _cc;
    private Vector3 _verticalVelocity;

    private bool _runHeld;
    private bool _isRunning;
    private bool _exhausted;
    private bool _onCooldown;
    private float _cooldownTimer;
    private float _stamina;


    // Inicialização


    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _stamina = maxStamina;
    }

    // Callbacks de Input (chamados pelo PlayerInput em modo Invoke Unity Events)
    // Registre estes métodos nos eventos do PlayerInput no Inspector.

    /*public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }
    */
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    
    /*
    public void OnSprint(InputValue value)
    {
        bool pressed = value.isPressed;

        if (pressed)
        {
            _runHeld = true;
        }
        else
        {
            _runHeld = false;
            // Aplica cooldown ao soltar o botão
            _onCooldown = true;
            _cooldownTimer = sprintCooldown;
        }
    }
    */

    public void OnSprint(InputAction.CallbackContext context)
    {
        bool pressed = context.ReadValueAsButton();

        if (pressed)
        {
            _runHeld = true;
        }
        else
        {
            _runHeld = false;
            _onCooldown = true;
            _cooldownTimer = sprintCooldown;
        }
    }

    // Update


    void Update()
    {
        UpdateCooldown();
        UpdateStamina();
        UpdateMovement();
    }

    // Lógica interna

    private void UpdateCooldown()
    {
        if (!_onCooldown) return;
        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
            _onCooldown = false;
    }

    private void UpdateStamina()
    {
        bool canSprint = !_onCooldown && !_exhausted && _stamina > 0f;
        _isRunning = _runHeld && canSprint;

        if (_isRunning)
        {
            _stamina -= staminaDrainRate * Time.deltaTime;

            if (_stamina <= 0f)
            {
                _stamina = 0f;
                _exhausted = true;
                _onCooldown = true;
                _cooldownTimer = sprintCooldown;
                _isRunning = false;
            }
        }
        else
        {
            _stamina = Mathf.Min(_stamina + staminaRecoverRate * Time.deltaTime, maxStamina);

            if (_exhausted && _stamina >= maxStamina)
                _exhausted = false;
        }
    }

    private void UpdateMovement()
    {
        Vector2 input = MoveInput.magnitude > 1f ? MoveInput.normalized : MoveInput;

        float speed = moveSpeed + (_isRunning ? addRunSpeed : 0f);
        Vector3 move = new Vector3(input.x, 0f, input.y) * speed;

        _currentSpeedDebug = speed;

        if (_cc.isGrounded && _verticalVelocity.y < 0f)
            _verticalVelocity.y = -2f;

        _verticalVelocity.y += gravity * Time.deltaTime;
        move.y = _verticalVelocity.y;

        _cc.Move(move * Time.deltaTime);

        if (input.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(new Vector3(input.x, 0f, input.y));
    }
}
