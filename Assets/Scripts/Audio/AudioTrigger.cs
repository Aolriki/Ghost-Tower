using UnityEngine;
using System;

[Serializable]
public struct AudioMapping
{
    public string triggerName;
    public SFXType soundType;
}

public class AudioTrigger : MonoBehaviour
{
    [Header("Áudios Deste Objeto")]
    public AudioMapping[] audioMappings;

    public void Play(string triggerName)
    {
        // 1. Verifica se o Unity Event disparou e chamou a função
        Debug.Log($"<color=yellow>[AudioTrigger]</color> O evento chamou Play('{triggerName}') no objeto {gameObject.name}");

        AudioMapping mapping = Array.Find(audioMappings, m => m.triggerName == triggerName);

        if (!string.IsNullOrEmpty(mapping.triggerName))
        {
            // 2. Verifica se encontrou a palavra na lista
            Debug.Log($"<color=green>[AudioTrigger]</color> Encontrou '{triggerName}'! O som mapeado é: {mapping.soundType}");

            if (AudioManager.Instance != null)
            {
                // 3. Verifica se o AudioManager existe
                Debug.Log($"<color=cyan>[AudioTrigger]</color> Mandando o AudioManager tocar: {mapping.soundType}");
                AudioManager.Instance.PlaySFX(mapping.soundType);
            }
            else
            {
                // 4. Erro se o Manager não estiver na cena
                Debug.LogError("<color=red>[AudioTrigger] FALHA:</color> AudioManager.Instance está NULO! O Manager está na cena?");
            }
        }
        else
        {
            // 5. Erro se a palavra foi digitada errada
            Debug.LogError($"<color=red>[AudioTrigger] FALHA:</color> A palavra '{triggerName}' não está na lista de Audio Mappings deste objeto!");
        }
    }
}