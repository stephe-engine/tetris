using UnityEngine;

public class Board : MonoBehaviour
{
    public int width = 10;
    public int height = 20;

    private int[,] cells;

    public RectInt Bounds
    {
        get
        {
            Vector2Int position = new Vector2Int(-width / 2, -height / 2);
            return new RectInt(position, new Vector2Int(width, height));
        }
    }

    private void Awake()
    {
        cells = new int[width, height];
    }

    public bool IsValidPosition(Vector2Int position)
    {
        RectInt bounds = Bounds;

        if (position.x < bounds.xMin || position.x >= bounds.xMax)
            return false;
        if (position.y < bounds.yMin || position.y >= bounds.yMax)
            return false;

        int x = position.x - bounds.xMin;
        int y = position.y - bounds.yMin;

        return cells[x, y] == 0;
    }

    public int GetCell(int x, int y)
    {
        return cells[x, y];
    }

    public void SetCell(int x, int y, int value)
    {
        cells[x, y] = value;
    }
}
