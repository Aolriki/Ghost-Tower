using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Prop que recebe itens de malha, instancia a malha numa ancora e resolve em grupo quando todos os slots da cena estao corretos.
public class MeshSlot : Interactable, IItemReceiver
{
    public enum MeshSlotState { Empty, Wrong, Correct, Solved }

    public override InteractIcon Icon => InteractIcon.Crystal;

    [Header("Mesh Slot")]
    public MeshSO correctItem;

    [Tooltip("Filho do slot que segura a posicao onde a malha do item e instanciada.")]
    public Transform meshAnchor;

    [Header("Events")]
    public UnityEvent OnSolved;

    [SerializeField] private MeshSlotState state = MeshSlotState.Empty;
    public MeshSlotState State => state;

    private MeshSO _currentItem;
    private GameObject _currentMeshInstance;

    // Registro estatico de todos os slots ativos da cena, usado para resolver o grupo.
    private static readonly List<MeshSlot> _all = new List<MeshSlot>();

    void OnEnable()
    {
        if (!_all.Contains(this)) _all.Add(this);
    }

    void OnDisable()
    {
        _all.Remove(this);
    }

    public override void Interact()
    {
        if (!canInteract) return;

        ItemSO selectedItem = PlayerHandItem.Instance?.SelectedItem;

        // Mao vazia: retira o item atual e devolve ao inventario.
        if (selectedItem == null)
        {
            if (_currentItem == null) return;

            bool added = InventoryManager.Instance.AddItem(_currentItem);
            if (!added) return; // inventario cheio, mantem o item no slot

            ClearMesh();
            _currentItem = null;
            EvaluateState();
            EvaluateGroup();
            return;
        }

        // So aceita itens de malha.
        if (selectedItem is not MeshSO) return;

        PlayerHandItem.Instance.DeliverTo(this);
    }

    public void ReceiveItem(ItemSO item)
    {
        if (item is not MeshSO mesh) return;

        MeshSO previous = _currentItem;

        // Remove o novo item primeiro para abrir espaco antes de devolver o anterior.
        InventoryManager.Instance.RemoveItem(item);
        _currentItem = mesh;
        SpawnMesh(mesh);

        if (previous != null)
            InventoryManager.Instance.AddItem(previous);

        EvaluateState();
        EvaluateGroup();
    }

    // Define o estado do slot a partir do item atual.
    private void EvaluateState()
    {
        if (_currentItem == null)
            state = MeshSlotState.Empty;
        else if (_currentItem == correctItem)
            state = MeshSlotState.Correct;
        else
            state = MeshSlotState.Wrong;
    }

    // Resolve o grupo inteiro quando todos os slots ativos estao corretos.
    private static void EvaluateGroup()
    {
        for (int i = 0; i < _all.Count; i++)
        {
            if (_all[i].state != MeshSlotState.Correct)
                return;
        }

        for (int i = 0; i < _all.Count; i++)
            _all[i].Solve();
    }

    private void Solve()
    {
        state = MeshSlotState.Solved;
        canInteract = false;
        OnCantInteract();
        OnSolved?.Invoke();
    }

    // Instancia a malha do item na ancora, removendo a anterior.
    private void SpawnMesh(MeshSO mesh)
    {
        ClearMesh();

        if (mesh.meshPrefab == null || meshAnchor == null) return;

        _currentMeshInstance = Instantiate(mesh.meshPrefab, meshAnchor);
        _currentMeshInstance.transform.localPosition = Vector3.zero;
        _currentMeshInstance.transform.localRotation = Quaternion.identity;
    }

    private void ClearMesh()
    {
        if (_currentMeshInstance != null)
        {
            Destroy(_currentMeshInstance);
            _currentMeshInstance = null;
        }
    }
}