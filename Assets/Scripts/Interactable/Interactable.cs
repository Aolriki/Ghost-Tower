using UnityEngine;
using UnityEngine.Events;

// Icones de interacao disponiveis no InteractionUI.
public enum InteractIcon { Default, Hand, Padlock, Eye, Cryptex, Crystal, SpeechBubble }

// Base class for all interactable objects in the scene.
[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    public UnityEvent OnInteract;
    public bool canInteract = true;

    [Header("World Space UI")]
    public Vector3 interactOffset = new Vector3(0f, 1.8f, 0f);

    // Icone exibido pelo InteractionUI enquanto este objeto for o mais proximo. Subclasses sobrescrevem.
    public virtual InteractIcon Icon => InteractIcon.Default;

    public virtual void Interact()
    {
        if (!canInteract) return;
        OnInteract?.Invoke();
        Debug.Log($"[Interactable] {gameObject.name} interacted.");
    }

    // Recebe um canal de input vindo do PlayerInteraction.
    // Subclasses podem sobrescrever para reagir a canais especificos (Talk, Deliver, etc.).
    // Por padrao, o canal "Interact" dispara Interact().
    public virtual void ReceiveInput(string channel)
    {
        if (channel == "Interact")
            Interact();
    }

    public virtual void OnCanInteract()
    {
        if (!canInteract) return;
        InteractionUI.Instance?.ShowAt(transform, interactOffset, Icon);
    }

    public virtual void OnCantInteract()
    {
        InteractionUI.Instance?.Hide(transform);
    }
}