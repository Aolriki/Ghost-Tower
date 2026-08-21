using UnityEngine;

// Base para objetos coletaveis que adicionam um item ao inventario do jogador.
public abstract class Collectable : Interactable
{
    [Header("Item")]
    public ItemSO item;

    // Fluxo unico do collectable: executa a acao e, se bem-sucedida, dispara OnInteract.
    public override void Interact()
    {
        if (!canInteract) return;
        if (!TryInteract()) return;

        OnInteract?.Invoke();
    }

    // Acao especifica de cada collectable. Retorna true se a interacao foi bem-sucedida.
    protected abstract bool TryInteract();

    // Tenta adicionar o item ao inventario. Se bem-sucedido, desativa o GameObject e o destroi apos um delay.
    protected bool Collect()
    {
        if (item == null) return false;
        if (InventoryManager.Instance == null) return false;

        bool added = InventoryManager.Instance.AddItem(item);
        if (!added) return false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.ColetarItem);
        }

        OnCantInteract();
        gameObject.SetActive(false);
        Destroy(gameObject, 0.5f);
        return true;
    }
}