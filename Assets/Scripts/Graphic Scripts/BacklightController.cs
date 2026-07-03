using UnityEngine;

// Controla o rim light direcional global do shader Toon.
// Seta os globais _BackLightDirWS, _BackLightColor, _BackLightPower e
// _BackLightIntensity uma unica vez no Start (e no editor via OnValidate).
// Nao tem custo de Update — a backlight e fixa durante a gameplay.
public class BackLightController : MonoBehaviour
{
    [Header("Direction")]
    [Tooltip("Direcao da backlight em angulos de Euler (graus). " +
             "X = pitch (cima/baixo), Y = yaw (esquerda/direita), Z = roll.")]
    public Vector3 eulerAngles = new Vector3(30f, 180f, 0f);

    [Header("Appearance")]
    public Color color = Color.white;
    [Range(0f, 10f)]
    public float intensity = 1f;
    [Range(0.01f, 10f)]
    [Tooltip("Expoente do Fresnel. Valores altos = contorno mais fino e concentrado nas bordas.")]
    public float power = 3f;

    // IDs das propriedades globais do shader (cache para evitar lookup por string em runtime)
    private static readonly int ID_Dir = Shader.PropertyToID("_BackLightDirWS");
    private static readonly int ID_Color = Shader.PropertyToID("_BackLightColor");
    private static readonly int ID_Power = Shader.PropertyToID("_BackLightPower");
    private static readonly int ID_Intensity = Shader.PropertyToID("_BackLightIntensity");

    void Start()
    {
        Apply();
    }

    // Aplica os valores ao shader.
    private void Apply()
    {
        // Converte euler angles para um vetor de direcao em world space.
        // Quaternion.Euler * Vector3.forward da a direcao para onde a luz "aponta".
        Vector3 direction = Quaternion.Euler(eulerAngles) * Vector3.forward;

        Shader.SetGlobalVector(ID_Dir, new Vector4(direction.x, direction.y, direction.z, 0f));
        Shader.SetGlobalColor(ID_Color, color);
        Shader.SetGlobalFloat(ID_Power, power);
        Shader.SetGlobalFloat(ID_Intensity, intensity);
    }

    // Atualiza o shader em tempo real enquanto ajusta os valores no Inspector.
    void OnValidate()
    {
        Apply();
    }
}