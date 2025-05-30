using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIMapVisualizer : IMapVisualizer
{
    private RectTransform container;
    private Cell cellPrefab;
    private Dictionary<RoomType, Sprite> roomSprites;
    private List<Cell> spawnedCells = new List<Cell>();

    public UIMapVisualizer(Cell cellPrefab, Dictionary<RoomType, Sprite> roomSprites)
    {
        this.cellPrefab = cellPrefab;
        this.roomSprites = roomSprites;
    }

    public void Initialize(RectTransform container)
    {
        this.container = container;
    }

    public void ClearMap()
    {
        foreach (var cell in spawnedCells)
        {
            Object.Destroy(cell.gameObject);
        }
        spawnedCells.Clear();
    }

    public void VisualizeRoom(int index, int x, int y, RoomType roomType = RoomType.Normal)
    {
        if (container == null || cellPrefab == null) return;

        // Create new cell as UI element
        Cell newCell = Object.Instantiate(cellPrefab, container);

        // Configure RectTransform
        RectTransform rectTransform = newCell.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(x * 60f, -y * 60f); // Using hardcoded 60f for now
        rectTransform.sizeDelta = new Vector2(60f, 60f);

        // Ensure the anchors are set properly for UI positioning
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Set cell properties
        newCell.Value = 1;
        newCell.Index = index;

        // Set special room sprite if applicable
        if (roomType != RoomType.Normal && roomSprites.ContainsKey(roomType))
        {
            newCell.SetSpecialRoomSprite(roomSprites[roomType]);
        }

        spawnedCells.Add(newCell);
    }

    public void CenterMapInContainer(float cellSize)
    {
        if (spawnedCells.Count == 0) return;

        // Find bounds of the map
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var cell in spawnedCells)
        {
            RectTransform rt = cell.GetComponent<RectTransform>();
            Vector2 pos = rt.anchoredPosition;

            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        // Calculate center offset
        Vector2 mapSize = new Vector2(maxX - minX + cellSize, maxY - minY + cellSize);
        Vector2 mapCenter = new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
        Vector2 containerCenter = Vector2.zero; // Center of the container

        // Calculate offset to center
        Vector2 offset = containerCenter - mapCenter;

        // Apply offset to all cells
        foreach (var cell in spawnedCells)
        {
            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.anchoredPosition += offset;
        }
    }
}
