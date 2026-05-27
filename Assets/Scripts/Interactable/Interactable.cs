using UnityEngine;
using UnityEngine.Events;

// Base class for all interactable objects in the scene.
[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    public UnityEvent OnInteract;
    public bool canInteract = true;

    [Header("World Space UI")]
    public Vector3 interactOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Highlight")]
    public MeshRenderer[] meshRenderers;
    public string highlightParam = "_ChangeColorOnMouseDown";

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
        InteractionUI.Instance?.ShowAt(transform, interactOffset);
        ChangeHighlight(1);
    }

    public virtual void OnCantInteract()
    {
        InteractionUI.Instance?.Hide(transform);
        ChangeHighlight(0);
    }

    public void ChangeHighlight(int value)
    {
        foreach (var mr in meshRenderers)
            if (mr != null)
                mr.material.SetInt(highlightParam, value);
    }
}