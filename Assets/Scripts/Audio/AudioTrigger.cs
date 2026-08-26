using UnityEngine;
using System;

[Serializable]
public struct AudioMapping
{
    public string triggerName;
    public SFXType soundType;

    [Tooltip("Deixe -1 para tocar um som aleatório da lista, ou use 0, 1, 2... para escolher o som exato.")]
    public int specificClipIndex;
}

public class AudioTrigger : MonoBehaviour
{
    [Header("Áudios Deste Objeto")]
    public AudioMapping[] audioMappings;

    public void Play(string triggerName)
    {
        AudioMapping mapping = Array.Find(audioMappings, m => m.triggerName == triggerName);

        if (!string.IsNullOrEmpty(mapping.triggerName))
        {
            if (AudioManager.Instance != null)
            {
                // NOVO: Agora enviamos também o número do índice para o Manager
                AudioManager.Instance.PlaySFX(mapping.soundType, mapping.specificClipIndex);
            }
        }
    }
}