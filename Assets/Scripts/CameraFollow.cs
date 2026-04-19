using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;

    [Header("Offset base da câmera em relação ao foco")]
    public Vector3 baseOffset = new Vector3(0f, 8f, -8f);

    [Header("Camera Focus")]
    public float focusMaxDistance = 3f;

    [Range(1f, 20f)]
    public float focusSmoothSpeed = 6f;

    private Vector3 _focusPosition;
    private Vector3 _focusTarget;

    private Vector2 _lookInput; // agora armazenamos input aqui

    void Awake()
    {
        if (player == null) return;

        _focusPosition = player.position;
        _focusTarget = player.position;
    }

    //Evento do PlayerInput (Invoke Unity Events)
    public void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector2 lookInput = _lookInput;

        if (lookInput.magnitude > 1f)
            lookInput = lookInput.normalized;

        _focusTarget = player.position + new Vector3(
            lookInput.x * focusMaxDistance,
            0f,
            lookInput.y * focusMaxDistance
        );

        _focusPosition = Vector3.Lerp(
            _focusPosition,
            _focusTarget,
            1f - Mathf.Exp(-focusSmoothSpeed * Time.deltaTime)
        );

        transform.position = _focusPosition + baseOffset;
        transform.LookAt(_focusPosition);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || player == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_focusPosition, 0.2f);
        Gizmos.DrawLine(player.position, _focusPosition);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_focusTarget, 0.15f);
    }
}