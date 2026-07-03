using UnityEngine;

// Detecta colisao com o player e solicita ao GameManager a troca para a cena definida no Inspector.
public class StageClear : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Build index da cena a ser carregada ao colidir com o player.")]
    public int targetSceneIndex = 0;

    [Header("Filter")]
    [Tooltip("Tag do objeto que aciona a transicao.")]
    public string playerTag = "Player";

    private bool _triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(playerTag)) return;

        _triggered = true;

        if (GameManager.Instance == null)
        {
            Debug.LogError("[StageClear] GameManager.Instance nao encontrado.");
            return;
        }

        GameManager.Instance.LoadScene(targetSceneIndex); 
    }
}