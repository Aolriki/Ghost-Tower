using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Deixe vazio para usar Camera.main automaticamente")]
    public Camera targetCamera;
    [Tooltip("Se true, inverte a face que olha para a câmera")]
    public bool flipFacing = false;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 lookDir = targetCamera.transform.position - transform.position;
        if (flipFacing)
            lookDir = -lookDir;

        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(lookDir, Vector3.up);
        transform.rotation = lookRotation * Quaternion.Euler(-90f, 0f, 0f);
    }
}