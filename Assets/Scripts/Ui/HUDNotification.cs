using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public enum KeySlotMessageId
{
    None,
    GreatLightningJar,
    AishaChestLocked,
}

// Pairs a KeySlotMessageId with its localized text. Order in the Inspector list does not matter.
[System.Serializable]
public struct KeySlotMessageEntry
{
    public KeySlotMessageId id;
    public LocalizedString message;
}

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
    [SerializeField] private LocalizedString wrongCodeMessage;
    [Tooltip("Mensagem padrao exibida pelo KeySlot quando messageId for None ou nao existir na lista abaixo.")]
    [SerializeField] private LocalizedString wrongKeyMessage;

    [Header("Key Slot Custom Messages")]
    [Tooltip("Cada entrada associa um KeySlotMessageId ao seu texto. A ordem na lista nao importa.")]
    [SerializeField] private KeySlotMessageEntry[] keySlotMessages;

    public string WrongCodeMessage => wrongCodeMessage.GetLocalizedString();
    public string WrongKeyMessage => wrongKeyMessage.GetLocalizedString();

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

    // Retorna a mensagem customizada associada ao id, ou a mensagem padrao
    // se o id for None, nao existir na lista, ou a entrada estiver vazia.
    public string GetKeySlotMessage(KeySlotMessageId id)
    {
        if (id == KeySlotMessageId.None) return WrongKeyMessage;

        if (keySlotMessages != null)
        {
            foreach (KeySlotMessageEntry entry in keySlotMessages)
            {
                if (entry.id == id && !entry.message.IsEmpty)
                    return entry.message.GetLocalizedString();
            }
        }

        return WrongKeyMessage;
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