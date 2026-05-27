using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Detects the nearest Interactable and forwards inputs by channel.
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;

    public bool CanInteract
    {
        get => _canInteract;
        set
        {
            _canInteract = value;
            if (!_canInteract && _nearest != null)
            {
                _nearest.OnCantInteract();
                _nearest = null;
            }
        }
    }

    // Exposed so PlayerContext can read the nearest interactable without polling.
    public Interactable NearestInteractable => _nearest;

    private bool _canInteract = true;
    private readonly List<Interactable> _interactables = new List<Interactable>();
    private Interactable _nearest;

    void Awake()
    {
        if (playerRoot == null)
            playerRoot = transform.parent != null ? transform.parent : transform;
    }

    // Register this method on the Interact event of PlayerInput in the Inspector.
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!_canInteract || !context.performed || _nearest == null) return;
        _nearest.ReceiveInput("Interact");
    }

    // Register this method on the Talk event of PlayerInput in the Inspector.
    public void OnTalk(InputAction.CallbackContext context)
    {
        if (!_canInteract || !context.performed || _nearest == null) return;
        _nearest.ReceiveInput("Talk");
    }

    void Update()
    {
        if (!_canInteract) return;
        RefreshNearest();
    }

    private void RefreshNearest()
    {
        if (_interactables.Count == 0)
        {
            ClearNearest();
            return;
        }

        float minDist = float.MaxValue;
        Interactable closest = null;
        Vector3 playerPos = playerRoot.position;

        for (int i = _interactables.Count - 1; i >= 0; i--)
        {
            var interactable = _interactables[i];

            if (interactable == null)
            {
                _interactables.RemoveAt(i);
                continue;
            }

            if (!interactable.canInteract) continue;

            float dist = Vector3.Distance(playerPos, interactable.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = interactable;
            }
        }

        if (_nearest != closest)
        {
            if (_nearest != null) _nearest.OnCantInteract();
            if (closest != null) closest.OnCanInteract();
        }

        _nearest = closest;
    }

    private void ClearNearest()
    {
        if (_nearest != null) _nearest.OnCantInteract();
        _nearest = null;
    }

    void OnTriggerEnter(Collider other)
    {
        var interactable = other.GetComponent<Interactable>();
        if (interactable != null && !_interactables.Contains(interactable))
            _interactables.Add(interactable);
    }

    void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponent<Interactable>();
        if (interactable == null) return;

        if (_nearest == interactable)
        {
            _nearest.OnCantInteract();
            _nearest = null;
        }

        _interactables.Remove(interactable);
    }
}