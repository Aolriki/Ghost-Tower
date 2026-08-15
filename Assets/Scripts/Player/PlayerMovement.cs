using UnityEngine;
using UnityEngine.InputSystem;

// Movimentacao do player: leitura de input relativo a camera, gravidade e rotacao.
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
    private Transform _cameraTransform;

    // Inicializacao

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
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
        Vector3 moveDirection = ComputeMoveDirection(input);

        float speed = moveSpeed;
        Vector3 move = moveDirection * speed;

        _currentSpeedDebug = speed;

        if (_cc.isGrounded && _verticalVelocity.y < 0f)
            _verticalVelocity.y = -2f;

        _verticalVelocity.y += gravity * Time.deltaTime;
        move.y = _verticalVelocity.y;

        _cc.Move(move * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);
        }

        if (_animator != null)
            _animator.SetFloat("Speed", input.magnitude, 0.01f, Time.deltaTime);
    }

    // Converte o input (espaco da tela: X = direita, Y = frente) para o espaco do mundo,
    // usando o forward e o right da camera projetados no plano horizontal.
    private Vector3 ComputeMoveDirection(Vector2 input)
    {
        if (_cameraTransform == null)
            return new Vector3(input.x, 0f, input.y);

        Vector3 camForward = _cameraTransform.forward;
        camForward.y = 0f;

        Vector3 camRight = _cameraTransform.right;
        camRight.y = 0f;

        if (camForward.sqrMagnitude < 0.0001f || camRight.sqrMagnitude < 0.0001f)
            return new Vector3(input.x, 0f, input.y);

        camForward.Normalize();
        camRight.Normalize();

        return camRight * input.x + camForward * input.y;
    }
}