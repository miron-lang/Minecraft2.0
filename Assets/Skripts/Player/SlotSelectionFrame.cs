using UnityEngine;
using UnityEngine.UI;

public class SlotSelectionFrame : MaskableGraphic
{
    public const float BorderWidth = 3f;// Ширина рамки

    protected override void OnPopulateMesh(VertexHelper mesh)// Рисуем рамку слота
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 2f * BorderWidth || rect.height <= 2f * BorderWidth)
            return;

        float edge = BorderWidth / 6f;
        rect = AddRing(mesh, rect, edge, new Color32(53, 53, 53, 255));// Тёмный край
        rect = AddRing(mesh, rect, BorderWidth - 2f * edge, new Color32(250, 250, 250, 255));// Светлая рамка
        AddRing(mesh, rect, edge, new Color32(95, 95, 95, 255));// Тень внутри рамки
    }

    private static Rect AddRing(VertexHelper mesh, Rect outer, float width, Color32 color)// Полоска по краю рамки
    {
        Rect inner = Rect.MinMaxRect(outer.xMin + width, outer.yMin + width,
            outer.xMax - width, outer.yMax - width);
        int start = mesh.currentVertCount;
        AddCorners(mesh, outer, color);
        AddCorners(mesh, inner, color);

        for (int side = 0; side < 4; side++)
        {
            int a = start + side;
            int b = start + (side + 1) % 4;
            mesh.AddTriangle(a, b, b + 4);
            mesh.AddTriangle(a, b + 4, a + 4);
        }

        return inner;
    }

    private static void AddCorners(VertexHelper mesh, Rect rect, Color32 color)
    {
        mesh.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);
    }
}
