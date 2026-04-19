using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Interactable3D : MonoBehaviour
{
    [Header("Interaction")]
    public UnityEvent OnInteract;
    public bool canInteract = true;

    [Header("World Space UI")]
    public Vector3 uiOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Highlight")]
    public MeshRenderer[] meshRenderers;
    public string highlightParam = "_ChangeColorOnMouseDown";

    public virtual void Interact()
    {
        if (!canInteract) return;
        OnInteract?.Invoke();
        Debug.Log($"[Interactable3D] {gameObject.name} interacted.");
    }

    public virtual void OnCanInteract()
    {
        if (!canInteract) return;
        InteractionUIManager.Instance?.ShowAt(transform, uiOffset);
        ChangeHighlight(1);
    }

    public virtual void OnCantInteract()
    {
        InteractionUIManager.Instance?.Hide(transform);
        ChangeHighlight(0);
    }

    public void ChangeHighlight(int value)
    {
        foreach (var mr in meshRenderers)
            if (mr != null)
                mr.material.SetInt(highlightParam, value);
    }
}