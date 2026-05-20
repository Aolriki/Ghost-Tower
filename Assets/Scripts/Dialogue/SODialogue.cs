using Characters;
using UnityEngine;

/// <summary>
/// Uma única fala dentro de um bloco de diálogo.
/// </summary>
[System.Serializable]
public class Sentence
{
    [Tooltip("Quem está falando.")]
    public CharacterName talker;

    [TextArea(2, 4)]
    public string sentenceText;

    [Tooltip("Ação executada ao exibir esta frase (opcional).")]
    public DialogueAction sentenceAction;
}

/// <summary>
/// Um bloco de diálogo: uma sequência de Sentences seguida de um
/// encadeamento opcional para o próximo bloco.
///
/// Bifurcação NÃO acontece aqui. Ela acontece ANTES de iniciar
/// o diálogo — um sistema externo (puzzle, item entregue, flag)
/// decide qual SODialogue chamar ou qual estado do NPC ativar.
///
/// Crie via: Assets > Create > Dialogue > SODialogue
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/SODialogue")]
public class SODialogue : ScriptableObject
{
    [Tooltip("Sequência de falas deste bloco.")]
    public Sentence[] sentences;

    [Tooltip("Bloco executado automaticamente após a última frase. " +
             "Null = fim do diálogo.")]
    public SODialogue nextDialogue;
}