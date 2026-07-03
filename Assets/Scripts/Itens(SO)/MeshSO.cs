using UnityEngine;

// Item do tipo malha 3D, instanciado por MeshSlot na cena.
[CreateAssetMenu(fileName = "NewMesh", menuName = "Inventory/Mesh")]
public class MeshSO : ItemSO
{
    public override ItemType type => ItemType.Mesh;

    [Header("Mesh")]
    // Prefab contendo a malha e o material do item.
    public GameObject meshPrefab;
}