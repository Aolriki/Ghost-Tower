using UnityEngine;

/// <summary>
/// Ação executável anexada a uma Sentence ou BranchOption.
/// Crie subclasses concretas (ex: UnlockDoorAction, PlaySoundAction)
/// via Assets > Create > Dialogue > Dialogue Action.
/// Padrão idêntico ao projeto original.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueAction", menuName = "Dialogue/Dialogue Action")]
public abstract class DialogueAction : ScriptableObject
{
    public abstract void Execute();
}
