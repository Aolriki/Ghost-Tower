using UnityEngine;

// Singleton de cena que agrega e expoe os modulos do player.
public class PlayerCore : MonoBehaviour
{
    // Singleton

    public static PlayerCore Instance { get; private set; }

    public PlayerMovement Movement { get; private set; }
    public PlayerInteraction Interaction { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject); // descomente se precisar persistir entre cenas

        Movement = GetComponentInChildren<PlayerMovement>();
        Interaction = GetComponentInChildren<PlayerInteraction>();

        if (Movement == null) Debug.LogWarning("[PlayerCore] PlayerMovement nao encontrado.");
        if (Interaction == null) Debug.LogWarning("[PlayerCore] PlayerInteraction nao encontrado.");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (Movement != null) Movement.enabled = enabled;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        if (Interaction != null) Interaction.CanInteract = enabled;
    }
}