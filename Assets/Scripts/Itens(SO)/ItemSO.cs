using UnityEngine;

// Tipos possiveis de item no inventario.
public enum ItemType { Key, Doc, Mesh }

// Classe base abstrata para todos os itens do inventario.
public abstract class ItemSO : ScriptableObject
{
    public Sprite icon;
    public Sprite iconSelected;
    public string itemName;

    // Tipo constante, definido por cada subclasse.
    public abstract ItemType type { get; }
}