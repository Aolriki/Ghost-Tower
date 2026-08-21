using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    // Este método será chamado de dentro do clipe de animação
    public void PlayFootstep()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Passo);
        }
    }
}