using UnityEngine;

// Dados da grid de level design. Cada celula representa 3x3 metros.
// Pintado via LevelGridEditor e consumido pelo LevelBuilder.
[CreateAssetMenu(menuName = "Level/Level Grid")]
public class LevelGridSO : ScriptableObject
{
    public int width = 10;
    public int height = 10;

    // Altura das paredes em metros (independente do tamanho da celula).
    public float wallHeight = 4.5f;

    // Grade de celulas, serializada como array 1D.
    // Indice: y * width + x
    public CellType[] cells;

    // Garante que o array tem o tamanho correto apos mudancas de dimensao.
    public void ValidateSize()
    {
        int expected = width * height;
        if (cells == null || cells.Length != expected)
        {
            CellType[] old = cells;
            cells = new CellType[expected];
            if (old != null)
            {
                int copy = Mathf.Min(old.Length, expected);
                for (int i = 0; i < copy; i++)
                    cells[i] = old[i];
            }
        }
    }

    public CellType GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return CellType.Empty;
        return cells[y * width + x];
    }

    public void SetCell(int x, int y, CellType type)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        cells[y * width + x] = type;
    }

    public bool IsFloor(int x, int y) => GetCell(x, y) == CellType.Floor;

    // Uma celula "de parede" e qualquer coisa que substitui parede:
    // Door e Window. Empty vira parede por deducao; estas sao pintadas.
    public bool IsDoor(int x, int y) => GetCell(x, y) == CellType.Door;
    public bool IsWindow(int x, int y) => GetCell(x, y) == CellType.Window;
}

public enum CellType
{
    Empty,
    Floor,
    Door,
    Window
}

// Direcao usada internamente pelo builder para orientar paredes.
public enum EdgeDirection
{
    North,  // Z+
    South,  // Z-
    East,   // X+
    West    // X-
}