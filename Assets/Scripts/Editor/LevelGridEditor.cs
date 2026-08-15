using UnityEngine;
using UnityEditor;

// EditorWindow para pintar a grid 2D do LevelGridSO.
// Abrir via: Window -> Level Builder -> Grid Editor
public class LevelGridEditor : EditorWindow
{
    private LevelGridSO _grid;
    private LevelBuilder _builder;

    private const float CellPixels = 24f;
    private const float Padding = 8f;

    private PaintMode _paintMode = PaintMode.Floor;
    private Vector2 _scroll;

    private enum PaintMode
    {
        Floor,
        Door,
        Window,
        Erase
    }

    [MenuItem("Window/Level Builder/Grid Editor")]
    public static void Open()
    {
        GetWindow<LevelGridEditor>("Grid Editor");
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_grid == null)
        {
            EditorGUILayout.HelpBox("Selecione um LevelGridSO para comecar.", MessageType.Info);
            return;
        }

        _grid.ValidateSize();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawGrid();
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginChangeCheck();
        _grid = (LevelGridSO)EditorGUILayout.ObjectField(
            _grid, typeof(LevelGridSO), false, GUILayout.Width(160));
        if (EditorGUI.EndChangeCheck() && _grid != null)
            _grid.ValidateSize();

        _builder = (LevelBuilder)EditorGUILayout.ObjectField(
            _builder, typeof(LevelBuilder), true, GUILayout.Width(160));

        GUILayout.Space(8);

        // Botao de rebuild.
        GUI.enabled = _builder != null && _grid != null;
        if (GUILayout.Button("Rebuild", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            _builder.grid = _grid;
            _builder.Rebuild();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("Limpar grid",
                "Apagar todas as celulas da grid?", "Sim", "Cancelar"))
            {
                Undo.RecordObject(_grid, "Clear Grid");
                for (int i = 0; i < _grid.cells.Length; i++)
                    _grid.cells[i] = CellType.Empty;
                EditorUtility.SetDirty(_grid);
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Segunda linha: modos de pintura.
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Pintar:", GUILayout.Width(45));
        if (GUILayout.Toggle(_paintMode == PaintMode.Floor, "Chao", EditorStyles.toolbarButton)) _paintMode = PaintMode.Floor;
        if (GUILayout.Toggle(_paintMode == PaintMode.Door, "Porta", EditorStyles.toolbarButton)) _paintMode = PaintMode.Door;
        if (GUILayout.Toggle(_paintMode == PaintMode.Window, "Janela", EditorStyles.toolbarButton)) _paintMode = PaintMode.Window;
        if (GUILayout.Toggle(_paintMode == PaintMode.Erase, "Apagar", EditorStyles.toolbarButton)) _paintMode = PaintMode.Erase;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Dimensoes.
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        int newW = EditorGUILayout.IntField("Largura", _grid != null ? _grid.width : 10, GUILayout.Width(160));
        int newH = EditorGUILayout.IntField("Altura", _grid != null ? _grid.height : 10, GUILayout.Width(160));
        if (EditorGUI.EndChangeCheck() && _grid != null)
        {
            Undo.RecordObject(_grid, "Resize Grid");
            _grid.width = Mathf.Max(1, newW);
            _grid.height = Mathf.Max(1, newH);
            _grid.ValidateSize();
            EditorUtility.SetDirty(_grid);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid()
    {
        if (_grid == null) return;

        Event e = Event.current;

        float totalW = _grid.width * CellPixels + Padding * 2;
        float totalH = _grid.height * CellPixels + Padding * 2;
        Rect gridRect = GUILayoutUtility.GetRect(totalW, totalH);

        for (int y = 0; y < _grid.height; y++)
        {
            for (int x = 0; x < _grid.width; x++)
            {
                int displayY = _grid.height - 1 - y;

                Rect cellRect = new Rect(
                    gridRect.x + Padding + x * CellPixels,
                    gridRect.y + Padding + displayY * CellPixels,
                    CellPixels - 1,
                    CellPixels - 1
                );

                EditorGUI.DrawRect(cellRect, ColorForCell(_grid.GetCell(x, y)));
                DrawRectOutline(cellRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));

                // Pintura: botao esquerdo aplica o modo atual.
                if (cellRect.Contains(e.mousePosition) &&
                    (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) &&
                    e.button == 0)
                {
                    Undo.RecordObject(_grid, "Paint Grid");
                    _grid.SetCell(x, y, CellForMode(_paintMode));
                    EditorUtility.SetDirty(_grid);
                    e.Use();
                    Repaint();
                }

                // Botao direito sempre apaga.
                if (cellRect.Contains(e.mousePosition) &&
                    (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) &&
                    e.button == 1)
                {
                    Undo.RecordObject(_grid, "Erase Grid");
                    _grid.SetCell(x, y, CellType.Empty);
                    EditorUtility.SetDirty(_grid);
                    e.Use();
                    Repaint();
                }
            }
        }

        // Legenda.
        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        LegendSwatch(ColorForCell(CellType.Floor), "Chao");
        LegendSwatch(ColorForCell(CellType.Door), "Porta");
        LegendSwatch(ColorForCell(CellType.Window), "Janela");
        LegendSwatch(ColorForCell(CellType.Empty), "Vazio");
        EditorGUILayout.EndHorizontal();
    }

    private CellType CellForMode(PaintMode mode) => mode switch
    {
        PaintMode.Floor => CellType.Floor,
        PaintMode.Door => CellType.Door,
        PaintMode.Window => CellType.Window,
        _ => CellType.Empty
    };

    private Color ColorForCell(CellType type) => type switch
    {
        CellType.Floor => new Color(0.4f, 0.7f, 0.4f),
        CellType.Door => new Color(0.8f, 0.5f, 0.2f),
        CellType.Window => new Color(0.3f, 0.6f, 0.9f),
        _ => new Color(0.2f, 0.2f, 0.2f)
    };

    private void LegendSwatch(Color color, string label)
    {
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16)), color);
        GUILayout.Label(" " + label, GUILayout.Width(55));
    }

    private void DrawRectOutline(Rect rect, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
    }
}