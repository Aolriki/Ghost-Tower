using UnityEngine;

// Collectable que abre um documento e opcionalmente entrega um item ao inventario.
public class DocItem : Collectable
{
    [Header("Doc Settings")]
    public bool isProp = false;

    public override InteractIcon Icon => InteractIcon.Eye;

    protected override bool TryInteract()
    {
        // Modo prop nao coleta, apenas abre o documento.
        if (!isProp)
        {
            // NOVO: Passa 'false' para silenciar o plim genérico.
            // O som do papel será tocado logo em seguida pelo ScreenManager!
            if (!Collect(false)) return false;
        }

        ReadMe();
        return true;
    }

    public void ReadMe()
    {
        if (item is not DocSO doc)
        {
            Debug.LogWarning($"[DocItem] {gameObject.name}: item nao e um DocSO.");
            return;
        }

        ScreenManager.OpenDocItem(doc);
    }
}