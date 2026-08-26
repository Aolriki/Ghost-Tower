using UnityEngine;

// Item do tipo documento, abre uma pagina de leitura na tela.
[CreateAssetMenu(fileName = "NewDoc", menuName = "Inventory/Doc")]
public class DocSO : ItemSO
{
    public override ItemType type => ItemType.Doc;

    [Header("Doc")]
    public GameObject docPagePrefab;

    [Header("Configuração de Áudio")]
    [Tooltip("O som que tocará quando esta tela for aberta")]
    public SFXType somDeAbertura = SFXType.AbrirDocumento; // Já deixamos o papel como padrão!

}