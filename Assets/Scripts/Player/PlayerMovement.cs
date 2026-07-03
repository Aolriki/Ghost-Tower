using UnityEngine;
using UnityEngine.InputSystem;

// Movimentacao do player: leitura de input, gravidade e rotacao.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Configuracao

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Gravity")]
    public float gravity = -20f;

    // Estado publico

    public Vector2 MoveInput { get; private set; }

    // Debug

    [Header("Debug")]
    [SerializeField] private float _currentSpeedDebug;

    // Privados

    private CharacterController _cc;
    private Vector3 _verticalVelocity;

    private Animator _animator;

    // Inicializacao

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    // Callbacks de Input (chamados pelo PlayerInput em modo Invoke Unity Events)
    // Registre este metodo no evento Move do PlayerInput no Inspector.

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    // Update

    void Update()
    {
        UpdateMovement();
    }

    // Logica interna

    private void UpdateMovement()
    {
        Vector2 input = MoveInput.magnitude > 1f ? MoveInput.normalized : MoveInput;

        float speed = moveSpeed;
        Vector3 move = new Vector3(input.x, 0f, input.y) * speed;

        _currentSpeedDebug = speed;

        if (_cc.isGrounded && _verticalVelocity.y < 0f)
            _verticalVelocity.y = -2f;

        _verticalVelocity.y += gravity * Time.deltaTime;
        move.y = _verticalVelocity.y;

        _cc.Move(move * Time.deltaTime);

        if (input.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(input.x, 0f, input.y));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);
        }

        if (_animator != null)
            _animator.SetFloat("Speed", input.magnitude, 0.01f, Time.deltaTime);
    }
}