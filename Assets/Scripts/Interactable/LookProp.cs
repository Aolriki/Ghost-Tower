using UnityEngine;

// Classe base para props observaveis: leva a camera ate um anchor e expoe hooks de input do LookMode.
public class LookProp : Interactable
{
    [Header("Look")]
    [Tooltip("Posicao e rotacao da camera ao observar este prop. Crie um filho 'CamAnchor' na frente do prop.")]
    public Transform camAnchor;

    [Tooltip("Canvas world space deste prop, exibido durante a observacao. Opcional.")]
    public Transform lookCanvas;

    public Transform CamAnchor => camAnchor;
    public bool IsLooking { get; private set; }

    private Camera _mainCamera;

    // ── Unity ─────────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        _mainCamera = Camera.main;
    }

    protected virtual void LateUpdate()
    {
        if (!IsLooking || lookCanvas == null || _mainCamera == null) return;

        // Mantem o canvas sempre virado para a camera, igual ao InteractionUI.
        lookCanvas.LookAt(
            lookCanvas.position + _mainCamera.transform.rotation * Vector3.forward,
            _mainCamera.transform.rotation * Vector3.up
        );
    }

    // ── Interactable override ─────────────────────────────────────────────────

    public override void Interact()
    {
        if (!canInteract) return;
        LookMode.Instance?.Enter(this);
    }

    // ── Hooks de ciclo de vida (chamados pelo LookMode) ───────────────────────

    // Chamado ao entrar na observacao. Subclasses estendem para preparar seu estado.
    public virtual void OnEnterLook()
    {
        IsLooking = true;
        if (lookCanvas != null) lookCanvas.gameObject.SetActive(true);
    }

    // Chamado ao sair da observacao. Subclasses estendem para limpar seu estado.
    public virtual void OnExitLook()
    {
        IsLooking = false;
        if (lookCanvas != null) lookCanvas.gameObject.SetActive(false);
    }

    // ── Hooks de input (repassados pelo LookMode via UIInputRouter) ───────────

    public virtual void OnLookConfirm() { }

    public virtual void OnLookNavigate(Vector2 input) { }

    // Por padrao, qualquer prop sai da observacao no cancel.
    public virtual void OnLookCancel() => LookMode.Instance?.Exit();
}