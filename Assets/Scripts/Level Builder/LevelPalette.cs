using UnityEngine;

// Mapeia cada tipo de peca do kit modular para um prefab.
// Troque os prefabs para mudar o visual de toda a cena sem tocar no builder.
[CreateAssetMenu(menuName = "Level/Level Palette")]
public class LevelPalette : ScriptableObject
{
    [Header("Floor")]
    public GameObject floor;

    [Header("Walls")]
    public GameObject wall;
    public GameObject windowWall;
    public GameObject doorWall;
    public GameObject mainDoorWall;

    [Header("Corners")]
    // Ambos os prefabs sao na orientacao Noroeste (rotacao 0).
    // O builder aplica a rotacao correta para cada quina.
    public GameObject outsideCorner;
    public GameObject insideCorner;
}
