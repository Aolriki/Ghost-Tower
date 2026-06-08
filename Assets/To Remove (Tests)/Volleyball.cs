using UnityEngine;

public class Volleyball : MonoBehaviour
{
    public static Volleyball Instance { get; private set; }

    public Transform BallTransform => transform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
