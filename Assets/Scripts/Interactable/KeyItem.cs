using UnityEngine;

// Collectable que entrega um item do tipo chave ao inventario do jogador.
public class KeyItem : Collectable
{
    public override InteractIcon Icon => InteractIcon.Hand;

    protected override bool TryInteract()
    {
        if (item == null || item.type != ItemType.Key) return false;
        return Collect();
    }
}