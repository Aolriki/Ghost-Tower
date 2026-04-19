using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    // Configuração


    [SerializeField] private Transform playerRoot;

    // Estado público

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

    // Privados


    private bool _canInteract = true;
    private readonly List<Interactable3D> _interactables = new List<Interactable3D>();
    private Interactable3D _nearest;


    // Inicialização


    void Awake()
    {
        if (playerRoot == null)
            playerRoot = transform.parent != null ? transform.parent : transform;
    }


    // Callback de Input (chamado pelo PlayerInput em modo Invoke Unity Events)
    // Registre este método no evento Interact do PlayerInput no Inspector.


    public void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log($"OnInteract called | phase: {context.phase}");
        if (!_canInteract) return;

        if (context.performed && _nearest != null)
            _nearest.Interact();
    }

    // Update


    void Update()
    {
        if (!_canInteract) return;
        RefreshNearest();
    }


    // Lógica interna

    private void RefreshNearest()
    {
        if (_interactables.Count == 0)
        {
            ClearNearest();
            return;
        }

        float minDist = float.MaxValue;
        Interactable3D closest = null;
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


    // Trigger (detecção de range)

    void OnTriggerEnter(Collider other)
    {
        var interactable = other.GetComponent<Interactable3D>();
        if (interactable != null && !_interactables.Contains(interactable))
            _interactables.Add(interactable);
    }

    void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponent<Interactable3D>();
        if (interactable == null) return;

        if (_nearest == interactable)
        {
            _nearest.OnCantInteract();
            _nearest = null;
        }

        _interactables.Remove(interactable);
    }
}
