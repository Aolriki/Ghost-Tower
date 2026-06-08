using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia a tela de fundo dinâmica com transição suave de cores entre painéis.
/// Em modo World Space, o Canvas acompanha a câmera com offset fixo e sempre
/// fica perpendicular a ela (face-to-camera), criando efeito de fundo estático.
/// </summary>
public class DynamicBackground : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Enum de Painéis
    // ─────────────────────────────────────────────
    public enum PanelScreen
    {
        ScreenX,
        ScreenY,
        ScreenZ
    }

    // ─────────────────────────────────────────────
    //  Estrutura de cores por painel
    // ─────────────────────────────────────────────
    [System.Serializable]
    public class PanelColors
    {
        [Tooltip("Qual painel recebe este conjunto de cores.")]
        public PanelScreen panel;

        [Tooltip("Cor da Image de fundo sólida (2560x1440 branca).")]
        public Color backgroundColor = Color.white;

        [Tooltip("Cor da Image com degradê de alpha (parte de baixo).")]
        public Color bottomGradientColor = Color.white;

        [Tooltip("Cor da Image com textura de bolinhas (parte de baixo).")]
        public Color bottomTextureColor = Color.white;

        [Tooltip("Cor da Image com degradê de alpha (parte de cima).")]
        public Color topGradientColor = Color.white;

        [Tooltip("Cor da Image com textura de bolinhas (parte de cima).")]
        public Color topTextureColor = Color.white;
    }

    // ─────────────────────────────────────────────
    //  Referências das Images no Inspector
    // ─────────────────────────────────────────────
    [Header("Referências das Images")]
    [Tooltip("Image de fundo sólida (2560x1440 branca).")]
    public Image backgroundImage;

    [Tooltip("Image com degradê de alpha vindo de baixo para cima.")]
    public Image bottomGradientImage;

    [Tooltip("Image com textura de bolinhas (parte de baixo).")]
    public Image bottomTextureImage;

    [Tooltip("Image com degradê de alpha vindo de cima para baixo.")]
    public Image topGradientImage;

    [Tooltip("Image com textura de bolinhas (parte de cima).")]
    public Image topTextureImage;

    // ─────────────────────────────────────────────
    //  Configurações de Cor por Painel
    // ─────────────────────────────────────────────
    [Header("Configurações de Cor por Painel")]
    [Tooltip("Lista de configurações de cor. Adicione uma entrada por painel do Enum.")]
    public List<PanelColors> panelColorSettings = new List<PanelColors>();

    // ─────────────────────────────────────────────
    //  Configurações de Transição
    // ─────────────────────────────────────────────
    [Header("Transição")]
    [Tooltip("Duração da transição de cores em segundos.")]
    [Range(0.1f, 5f)]
    public float transitionDuration = 0.8f;

    [Tooltip("Curva de animação da transição (ease in/out recomendado).")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ─────────────────────────────────────────────
    //  Câmera e Posicionamento World Space
    // ─────────────────────────────────────────────
    [Header("Câmera / World Space")]
    [Tooltip("Câmera que o fundo deve seguir. Se vazio, usa Camera.main automaticamente.")]
    public Camera targetCamera;

    [Tooltip("Distância à frente da câmera onde o Canvas será posicionado.")]
    public float distanceFromCamera = 10f;

    [Tooltip("Offset adicional em relação ao centro da câmera (espaço local da câmera). " +
             "X = direita, Y = cima, Z = frente (soma com distanceFromCamera).")]
    public Vector3 localOffset = Vector3.zero;



    // ─────────────────────────────────────────────
    //  Estado Interno
    // ─────────────────────────────────────────────
    [Header("Estado Atual (somente leitura)")]
    [SerializeField, Tooltip("Painel atualmente ativo.")]
    private PanelScreen currentPanel = PanelScreen.ScreenX;

    private Coroutine _transitionCoroutine;
    private Transform _camTransform;

    // ─────────────────────────────────────────────
    //  Inicialização
    // ─────────────────────────────────────────────
    private void Start()
    {
        ResolveCamera();
        ApplyColorsImmediate(currentPanel);

        // Posiciona imediatamente no frame inicial para evitar flash de posição errada
        FollowCamera();
    }

    // ─────────────────────────────────────────────
    //  Loop — acompanha câmera todo frame
    // ─────────────────────────────────────────────

    /// <summary>
    /// LateUpdate garante que a câmera já terminou de se mover neste frame
    /// antes de o fundo copiar sua posição/rotação.
    /// </summary>
    private void LateUpdate()
    {
        FollowCamera();
    }

    // ─────────────────────────────────────────────
    //  Lógica de posicionamento
    // ─────────────────────────────────────────────
    private void FollowCamera()
    {
        if (_camTransform == null)
        {
            ResolveCamera();
            if (_camTransform == null) return;
        }

        // ── Posição ──────────────────────────────
        // Calcula a posição à frente da câmera + offset no espaço local da câmera
        Vector3 forward = _camTransform.forward;
        Vector3 worldPosition = _camTransform.position
                                + forward * distanceFromCamera
                                + _camTransform.right * localOffset.x
                                + _camTransform.up * localOffset.y
                                + forward * localOffset.z;

        transform.position = worldPosition;

        // ── Rotação ──────────────────────────────
        // Copia toda a rotação da câmera (yaw, pitch e roll),
        // mantendo o Canvas sempre paralelo ao plano de projeção.
        transform.rotation = _camTransform.rotation;
    }

    /// <summary>Resolve a referência da câmera, buscando Camera.main se necessário.</summary>
    private void ResolveCamera()
    {
        if (targetCamera != null)
        {
            _camTransform = targetCamera.transform;
            return;
        }

        Camera main = Camera.main;
        if (main != null)
        {
            targetCamera = main;
            _camTransform = main.transform;
        }
        else
        {
            Debug.LogWarning("[DynamicBackground] Nenhuma câmera encontrada. " +
                             "Atribua 'Target Camera' no Inspector ou adicione a tag 'MainCamera'.");
        }
    }

    // ─────────────────────────────────────────────
    //  API Pública — chamada pelos botões ou outros scripts
    // ─────────────────────────────────────────────

    /// <summary>Troca para ScreenX com transição suave.</summary>
    public void GoToScreenX() => TransitionToPanel(PanelScreen.ScreenX);

    /// <summary>Troca para ScreenY com transição suave.</summary>
    public void GoToScreenY() => TransitionToPanel(PanelScreen.ScreenY);

    /// <summary>Troca para ScreenZ com transição suave.</summary>
    public void GoToScreenZ() => TransitionToPanel(PanelScreen.ScreenZ);

    /// <summary>Troca para qualquer painel via código.</summary>
    public void TransitionToPanel(PanelScreen targetPanel)
    {
        if (targetPanel == currentPanel) return;

        PanelColors from = GetPanelColors(currentPanel);
        PanelColors to = GetPanelColors(targetPanel);

        if (from == null || to == null)
        {
            Debug.LogWarning($"[DynamicBackground] Configuração de cores não encontrada para '{targetPanel}'. " +
                             "Verifique a lista 'Panel Color Settings' no Inspector.");
            return;
        }

        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        currentPanel = targetPanel;
        _transitionCoroutine = StartCoroutine(TransitionCoroutine(from, to));
    }

    // ─────────────────────────────────────────────
    //  Coroutine de Transição
    // ─────────────────────────────────────────────
    private IEnumerator TransitionCoroutine(PanelColors from, PanelColors to)
    {
        // Captura as cores ATUAIS das images (suporta interrupção no meio da animação)
        Color startBg = backgroundImage != null ? backgroundImage.color : from.backgroundColor;
        Color startBotGr = bottomGradientImage != null ? bottomGradientImage.color : from.bottomGradientColor;
        Color startBotTex = bottomTextureImage != null ? bottomTextureImage.color : from.bottomTextureColor;
        Color startTopGr = topGradientImage != null ? topGradientImage.color : from.topGradientColor;
        Color startTopTex = topTextureImage != null ? topTextureImage.color : from.topTextureColor;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float curvedT = transitionCurve.Evaluate(t);

            SetImageColor(backgroundImage, Color.Lerp(startBg, to.backgroundColor, curvedT));
            SetImageColor(bottomGradientImage, Color.Lerp(startBotGr, to.bottomGradientColor, curvedT));
            SetImageColor(bottomTextureImage, Color.Lerp(startBotTex, to.bottomTextureColor, curvedT));
            SetImageColor(topGradientImage, Color.Lerp(startTopGr, to.topGradientColor, curvedT));
            SetImageColor(topTextureImage, Color.Lerp(startTopTex, to.topTextureColor, curvedT));

            yield return null;
        }

        // Garante valores finais exatos
        SetImageColor(backgroundImage, to.backgroundColor);
        SetImageColor(bottomGradientImage, to.bottomGradientColor);
        SetImageColor(bottomTextureImage, to.bottomTextureColor);
        SetImageColor(topGradientImage, to.topGradientColor);
        SetImageColor(topTextureImage, to.topTextureColor);

        _transitionCoroutine = null;
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private void ApplyColorsImmediate(PanelScreen panel)
    {
        PanelColors config = GetPanelColors(panel);
        if (config == null) return;

        SetImageColor(backgroundImage, config.backgroundColor);
        SetImageColor(bottomGradientImage, config.bottomGradientColor);
        SetImageColor(bottomTextureImage, config.bottomTextureColor);
        SetImageColor(topGradientImage, config.topGradientColor);
        SetImageColor(topTextureImage, config.topTextureColor);
    }

    private PanelColors GetPanelColors(PanelScreen panel)
    {
        foreach (var config in panelColorSettings)
            if (config.panel == panel) return config;
        return null;
    }

    private void SetImageColor(Image image, Color color)
    {
        if (image != null) image.color = color;
    }

    // ─────────────────────────────────────────────
    //  Validação no Editor
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (panelColorSettings == null || panelColorSettings.Count == 0)
        {
            panelColorSettings = new List<PanelColors>();
            foreach (PanelScreen screen in System.Enum.GetValues(typeof(PanelScreen)))
                panelColorSettings.Add(new PanelColors { panel = screen });
        }
    }
#endif
}