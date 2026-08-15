using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Constroi a geometria 3D a partir de um LevelGridSO.
// Tudo em celulas inteiras (multiplos de cellSize), sem offsets fracionarios.
//
// Tipos de celula:
//   Floor  -> chao
//   Empty  -> vira parede/coluna por deducao dos vizinhos
//   Door   -> substitui parede por porta (3 alinhadas = MainDoor de 9m)
//   Window -> substitui parede por janela
//
// Regras de deducao (avaliadas em celulas Empty/Door/Window):
//   - Parede/Door/Window: 1 vizinho ortogonal Floor -> face aponta para o Floor
//   - Outside Corner:      0 ortogonais Floor + exatamente 1 diagonal Floor
//   - Inside Corner:       2 ortogonais adjacentes Floor + a diagonal entre eles
//
// Gerado vai para o container "Generated". Props fora dele sobrevivem ao Rebuild.
[ExecuteInEditMode]
public class LevelBuilder : MonoBehaviour
{
    public LevelGridSO grid;
    public LevelPalette palette;

    public float cellSize = 3f;

    private Transform _generated;

    // Marca celulas Door ja consumidas por uma MainDoor, para nao duplicar.
    private System.Collections.Generic.HashSet<Vector2Int> _consumedDoors
        = new System.Collections.Generic.HashSet<Vector2Int>();

    // ---- API publica ----

    [ContextMenu("Rebuild Level")]
    public void Rebuild()
    {
        if (grid == null || palette == null)
        {
            Debug.LogWarning("LevelBuilder: grid ou palette nao atribuidos.");
            return;
        }

        grid.ValidateSize();
        ClearGenerated();
        GetOrCreateGenerated();
        _consumedDoors.Clear();

        BuildFloors();
        BuildMainDoors();   // antes das paredes: consome as Doors que viram MainDoor
        BuildWallsAndCorners();

        Debug.Log("LevelBuilder: rebuild concluido.");
    }

    [ContextMenu("Clear Generated")]
    public void ClearGenerated()
    {
        Transform gen = transform.Find("Generated");
        if (gen != null)
            DestroyImmediate(gen.gameObject);
        _generated = null;
    }

    // ---- Conversao grid -> mundo ----

    private Vector3 CellCenter(int x, int y)
    {
        return new Vector3(x * cellSize, 0f, y * cellSize);
    }

    // ---- Construcao ----

    private void BuildFloors()
    {
        for (int y = 0; y < grid.height; y++)
            for (int x = 0; x < grid.width; x++)
                if (grid.IsFloor(x, y))
                    Spawn(palette.floor, CellCenter(x, y), Quaternion.identity);
    }

    // Detecta trios de Doors alinhadas apontando para o mesmo Floor e instancia
    // uma MainDoor de 9m no centro, consumindo as 3 celulas.
    private void BuildMainDoors()
    {
        for (int y = 0; y < grid.height; y++)
        {
            for (int x = 0; x < grid.width; x++)
            {
                if (!grid.IsDoor(x, y)) continue;
                Vector2Int c = new Vector2Int(x, y);
                if (_consumedDoors.Contains(c)) continue;

                // Direcao do Floor a partir desta Door.
                EdgeDirection floorDir;
                if (!TryGetSingleFloorDir(x, y, out floorDir)) continue;

                // Tenta um trio HORIZONTAL (x-1, x, x+1) todos Door com mesmo floorDir.
                if (IsMainDoorTrio(x, y, 1, 0, floorDir))
                {
                    PlaceMainDoor(x, y, 1, 0, floorDir);
                    continue;
                }
                // Tenta um trio VERTICAL (y-1, y, y+1).
                if (IsMainDoorTrio(x, y, 0, 1, floorDir))
                {
                    PlaceMainDoor(x, y, 0, 1, floorDir);
                    continue;
                }
            }
        }
    }

    // Verifica se (cx,cy) e o CENTRO de um trio de Doors ao longo do eixo (dx,dy),
    // todas com o mesmo floorDir. Assim cada trio e detectado uma unica vez.
    private bool IsMainDoorTrio(int cx, int cy, int dx, int dy, EdgeDirection floorDir)
    {
        int ax = cx - dx, ay = cy - dy; // celula anterior
        int bx = cx + dx, by = cy + dy; // celula seguinte

        if (!grid.IsDoor(ax, ay) || !grid.IsDoor(bx, by)) return false;

        // As tres precisam apontar para o mesmo Floor.
        EdgeDirection da, db;
        if (!TryGetSingleFloorDir(ax, ay, out da) || da != floorDir) return false;
        if (!TryGetSingleFloorDir(bx, by, out db) || db != floorDir) return false;

        return true;
    }

    private void PlaceMainDoor(int cx, int cy, int dx, int dy, EdgeDirection floorDir)
    {
        // Consome as 3 celulas.
        _consumedDoors.Add(new Vector2Int(cx - dx, cy - dy));
        _consumedDoors.Add(new Vector2Int(cx, cy));
        _consumedDoors.Add(new Vector2Int(cx + dx, cy + dy));

        // MainDoor tem pivo no centro -> vai na celula do meio.
        float rotY = WallRotationForFloor(floorDir);
        Spawn(palette.mainDoorWall, CellCenter(cx, cy), Quaternion.Euler(0f, rotY, 0f));
    }

    // Percorre cada celula que NAO e Floor e decide parede/porta/janela/coluna.
    private void BuildWallsAndCorners()
    {
        for (int y = -1; y <= grid.height; y++)
        {
            for (int x = -1; x <= grid.width; x++)
            {
                if (grid.IsFloor(x, y)) continue;

                // Doors ja consumidas por MainDoor sao puladas.
                if (grid.IsDoor(x, y) && _consumedDoors.Contains(new Vector2Int(x, y)))
                    continue;

                bool n = grid.IsFloor(x, y + 1);
                bool s = grid.IsFloor(x, y - 1);
                bool e = grid.IsFloor(x + 1, y);
                bool w = grid.IsFloor(x - 1, y);

                bool ne = grid.IsFloor(x + 1, y + 1);
                bool nw = grid.IsFloor(x - 1, y + 1);
                bool se = grid.IsFloor(x + 1, y - 1);
                bool sw = grid.IsFloor(x - 1, y - 1);

                int orthoCount = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);

                // --- Parede / Door / Window: 1 vizinho ortogonal Floor ---
                if (orthoCount == 1)
                {
                    EdgeDirection floorDir =
                        n ? EdgeDirection.North :
                        s ? EdgeDirection.South :
                        e ? EdgeDirection.East :
                            EdgeDirection.West;

                    float rotY = WallRotationForFloor(floorDir);

                    GameObject prefab =
                        grid.IsDoor(x, y) ? palette.doorWall :
                        grid.IsWindow(x, y) ? palette.windowWall :
                                              palette.wall;

                    Spawn(prefab, CellCenter(x, y), Quaternion.Euler(0f, rotY, 0f));
                    continue;
                }

                // Colunas so aparecem em celulas Empty (nao em Door/Window pintadas).
                if (grid.IsDoor(x, y) || grid.IsWindow(x, y))
                    continue;

                // --- Inside Corner: 2 ortogonais adjacentes + diagonal entre eles ---
                if (orthoCount == 2)
                {
                    if (n && e && ne) { PlaceInsideCorner(x, y, 270f); continue; }
                    if (n && w && nw) { PlaceInsideCorner(x, y, 180f); continue; }
                    if (s && e && se) { PlaceInsideCorner(x, y, 0f); continue; }
                    if (s && w && sw) { PlaceInsideCorner(x, y, 90f); continue; }
                    continue;
                }

                // --- Outside Corner: 0 ortogonais + exatamente 1 diagonal Floor ---
                if (orthoCount == 0)
                {
                    int diagCount = (ne ? 1 : 0) + (nw ? 1 : 0) + (se ? 1 : 0) + (sw ? 1 : 0);
                    if (diagCount == 1)
                    {
                        if (se) PlaceOutsideCorner(x, y, 180f);
                        else if (sw) PlaceOutsideCorner(x, y, 270f);
                        else if (nw) PlaceOutsideCorner(x, y, 0f);
                        else if (ne) PlaceOutsideCorner(x, y, 90f);
                    }
                    continue;
                }
            }
        }
    }

    // ---- Colocacao de pecas ----

    private void PlaceOutsideCorner(int x, int y, float rotY)
    {
        Spawn(palette.outsideCorner, CellCenter(x, y), Quaternion.Euler(0f, rotY, 0f));
    }

    private void PlaceInsideCorner(int x, int y, float rotY)
    {
        Spawn(palette.insideCorner, CellCenter(x, y), Quaternion.Euler(0f, rotY, 0f));
    }

    // ---- Rotacao de parede a partir da direcao do Floor ----
    // Face interna do prefab aponta para +Z em rotacao 0.
    //   Floor ao Norte (+Z) -> 0
    //   Floor ao Sul   (-Z) -> 180
    //   Floor ao Leste (+X) -> 90
    //   Floor ao Oeste (-X) -> 270
    private float WallRotationForFloor(EdgeDirection floorDir) => floorDir switch
    {
        EdgeDirection.North => 0f,
        EdgeDirection.South => 180f,
        EdgeDirection.East => 90f,
        EdgeDirection.West => 270f,
        _ => 0f
    };

    // ---- Helpers ----

    // Retorna true e a direcao se a celula tem EXATAMENTE 1 vizinho ortogonal Floor.
    private bool TryGetSingleFloorDir(int x, int y, out EdgeDirection dir)
    {
        bool n = grid.IsFloor(x, y + 1);
        bool s = grid.IsFloor(x, y - 1);
        bool e = grid.IsFloor(x + 1, y);
        bool w = grid.IsFloor(x - 1, y);

        int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);
        if (count != 1)
        {
            dir = EdgeDirection.North;
            return false;
        }

        dir = n ? EdgeDirection.North :
              s ? EdgeDirection.South :
              e ? EdgeDirection.East :
                  EdgeDirection.West;
        return true;
    }

    private void GetOrCreateGenerated()
    {
        _generated = transform.Find("Generated");
        if (_generated == null)
        {
            GameObject go = new GameObject("Generated");
            go.transform.SetParent(transform, false);
            _generated = go.transform;
        }
    }

    private GameObject Spawn(GameObject prefab, Vector3 localPos, Quaternion localRot)
    {
        if (prefab == null)
        {
            Debug.LogWarning("LevelBuilder: prefab nulo na palette.");
            return null;
        }

#if UNITY_EDITOR
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _generated);
#else
        GameObject go = Instantiate(prefab, _generated);
#endif
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one;
        return go;
    }
}