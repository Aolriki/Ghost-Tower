using UnityEngine;

// Item do tipo documento, abre uma pagina de leitura na tela.
[CreateAssetMenu(fileName = "NewDoc", menuName = "Inventory/Doc")]
public class DocSO : ItemSO
{
    public override ItemType type => ItemType.Doc;

    [Header("Doc")]
    public GameObject docPagePrefab;
}