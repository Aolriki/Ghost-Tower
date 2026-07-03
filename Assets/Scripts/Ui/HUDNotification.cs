using System.Collections;
using TMPro;
using UnityEngine;

// Singleton global (DontDestroyOnLoad). Exibe notificacoes de feedback na HUD com fade de saida.
// Chamado por qualquer sistema que precise notificar o jogador de forma nao bloqueante.
public class HUDNotification : MonoBehaviour
{
    public static HUDNotification Instance { get; private set; }

    [Header("Notification")]
    [SerializeField] private TMP_Text notificationLabel;

    [Header("Timing")]
    [Tooltip("Tempo em segundos que a notificacao permanece em alpha cheio antes de sumir.")]
    [SerializeField] private float holdDuration = 1f;
    [Tooltip("Duracao do fade de saida em segundos.")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Default Messages")]
    [Tooltip("Mensagem exibida pelo CodeSlot ao errar a senha.")]
    [SerializeField] private string wrongCodeMessage = "Wrong combination.";
    [Tooltip("Mensagem exibida pelo KeySlot ao tentar um item errado, quando o slot nao tiver texto customizado.")]
    [SerializeField] private string wrongKeyMessage = "This key does not fit.";

    public string WrongCodeMessage => wrongCodeMessage;
    public string WrongKeyMessage => wrongKeyMessage;

    private Coroutine _showCoroutine;
    private bool _isShowing;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (notificationLabel != null)
            notificationLabel.gameObject.SetActive(false);
    }

    // Exibe a notificacao com o texto fornecido.
    // Ignora a chamada se uma notificacao ja estiver sendo exibida.
    public void Show(string message)
    {
        if (_isShowing) return;
        if (notificationLabel == null) return;

        notificationLabel.text = message;
        notificationLabel.alpha = 1f;
        notificationLabel.gameObject.SetActive(true);

        _showCoroutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        _isShowing = true;

        yield return new WaitForSeconds(holdDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            notificationLabel.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        notificationLabel.alpha = 0f;
        notificationLabel.gameObject.SetActive(false);
        _isShowing = false;
    }
}
