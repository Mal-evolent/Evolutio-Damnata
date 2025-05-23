using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class MapGenerator : MonoBehaviour
{
    private int[] floorPlan;
    private int floorPlanCount;
    private int minRooms;
    private int maxRooms;
    private List<int> endRooms;

    private int bossRoomIndex;
    private int secretRoomIndex;
    private int shopRoomIndex;
    private int itemRoomIndex;

    public Cell cellPrefab;
    private float cellSize;
    private Queue<int> cellQueue;
    private List<Cell> spawnedCells;

    [Tooltip("Parent transform for the generated map (usually a panel under Canvas)")]
    [SerializeField] private RectTransform mapContainer;

    [Header("Sprite References")]
    [SerializeField] private Sprite item;
    [SerializeField] private Sprite shop;
    [SerializeField] private Sprite boss;
    [SerializeField] private Sprite secret;

    void Awake()
    {
        // If no map container is assigned, use this object's RectTransform
        if (mapContainer == null)
        {
            mapContainer = GetComponent<RectTransform>();

            if (mapContainer == null)
            {
                Debug.LogError("MapGenerator must be attached to a GameObject with a RectTransform or have a mapContainer assigned.");
            }
        }
    }

    void Start()
    {
        minRooms = 7;
        maxRooms = 15;
        cellSize = 60f;
        spawnedCells = new();

        SetupDungeon();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetupDungeon();
        }
    }

    void SetupDungeon()
    {
        for (int i = 0; i < spawnedCells.Count; i++)
        {
            Destroy(spawnedCells[i].gameObject);
        }

        spawnedCells.Clear();

        floorPlan = new int[100];
        floorPlanCount = default;
        cellQueue = new Queue<int>();
        endRooms = new List<int>();

        // Center the map by starting from the middle
        VisitCell(45);

        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        while (cellQueue.Count > 0)
        {
            int index = cellQueue.Dequeue();
            int x = index % 10;

            bool created = false;

            if (x > 1) created |= VisitCell(index - 1);
            if (x < 9) created |= VisitCell(index + 1);
            if (index > 20) created |= VisitCell(index - 10);
            if (index < 70) created |= VisitCell(index + 10);

            if (created == false)
                endRooms.Add(index);
        }

        if (floorPlanCount < minRooms)
        {
            SetupDungeon();
            return;
        }

        CleanEndRoomsList();
        SetupSpecialRooms();

        // Center the map in the container
        CenterMapInContainer();
    }

    // Centers the generated map within its container
    private void CenterMapInContainer()
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

    void CleanEndRoomsList()
    {
        endRooms.RemoveAll(item => GetNeighbourCount(item) > 1);
    }

    void SetupSpecialRooms()
    {
        // Create a list of normal room indices to potentially convert to special rooms
        List<int> normalRooms = new List<int>();
        for (int i = 0; i < floorPlan.Length; i++)
        {
            if (floorPlan[i] == 1 && !endRooms.Contains(i))
            {
                normalRooms.Add(i);
            }
        }

        // Shuffle normal rooms
        normalRooms = normalRooms.OrderBy(_ => Random.value).ToList();

        // Assign boss room - can be any room
        if (normalRooms.Count > 0)
        {
            bossRoomIndex = normalRooms[0];
            normalRooms.RemoveAt(0);
        }
        else if (endRooms.Count > 0)
        {
            bossRoomIndex = endRooms[0];
            endRooms.RemoveAt(0);
        }
        else
        {
            bossRoomIndex = -1;
        }

        // Assign item room
        if (normalRooms.Count > 0)
        {
            itemRoomIndex = normalRooms[0];
            normalRooms.RemoveAt(0);
        }
        else if (endRooms.Count > 0)
        {
            itemRoomIndex = endRooms[0];
            endRooms.RemoveAt(0);
        }
        else
        {
            itemRoomIndex = -1;
        }

        // Assign shop room
        if (normalRooms.Count > 0)
        {
            shopRoomIndex = normalRooms[0];
            normalRooms.RemoveAt(0);
        }
        else if (endRooms.Count > 0)
        {
            shopRoomIndex = endRooms[0];
            endRooms.RemoveAt(0);
        }
        else
        {
            shopRoomIndex = -1;
        }

        // Find suitable secret room location
        secretRoomIndex = FindSecretRoomLocation();

        if (itemRoomIndex == -1 || shopRoomIndex == -1 || bossRoomIndex == -1 || secretRoomIndex == -1)
        {
            SetupDungeon();
            return;
        }

        SpawnRoom(secretRoomIndex);
        UpdateSpecialRoomVisuals();
    }

    void UpdateSpecialRoomVisuals()
    {
        foreach (var cell in spawnedCells)
        {
            if (cell.index == itemRoomIndex)
            {
                cell.SetSpecialRoomSprite(item);
            }
            else if (cell.index == shopRoomIndex)
            {
                cell.SetSpecialRoomSprite(shop);
            }
            else if (cell.index == bossRoomIndex)
            {
                cell.SetSpecialRoomSprite(boss);
            }
            else if (cell.index == secretRoomIndex)
            {
                cell.SetSpecialRoomSprite(secret);
            }
        }
    }

    int FindSecretRoomLocation()
    {
        // Try to find an empty cell that has at least one adjacent room
        List<int> potentialSecretRooms = new List<int>();

        for (int i = 0; i < floorPlan.Length; i++)
        {
            if (floorPlan[i] == 0 && IsValidSecretRoomLocation(i))
            {
                potentialSecretRooms.Add(i);
            }
        }

        if (potentialSecretRooms.Count > 0)
        {
            return potentialSecretRooms[Random.Range(0, potentialSecretRooms.Count)];
        }

        // Fallback to original method if nothing found
        return PickSecretRoom();
    }

    bool IsValidSecretRoomLocation(int index)
    {
        // Check bounds
        if (index % 10 == 0 || index % 10 == 9 || index < 10 || index >= 90)
        {
            return false;
        }

        // Check if there's at least one adjacent room
        return GetNeighbourCount(index) > 0;
    }

    int PickSecretRoom()
    {
        for (int attempt = 0; attempt < 900; attempt++)
        {
            int x = Mathf.FloorToInt(Random.Range(0f, 1f) * 9) + 1;
            int y = Mathf.FloorToInt(Random.Range(0f, 1f) * 8) + 2;

            int index = y * 10 + x;

            if (floorPlan[index] != 0)
            {
                continue;
            }

            if (bossRoomIndex == index - 1 || bossRoomIndex == index + 1 ||
                bossRoomIndex == index + 10 || bossRoomIndex == index - 10)
            {
                continue;
            }

            if (index - 1 < 0 || index + 1 > floorPlan.Length ||
                index - 10 < 0 || index + 10 > floorPlan.Length)
            {
                continue;
            }

            int neighbours = GetNeighbourCount(index);

            if (neighbours >= 1)
            {
                return index;
            }
        }

        return -1;
    }

    private int GetNeighbourCount(int index)
    {
        return floorPlan[index - 10] + floorPlan[index - 1] + floorPlan[index + 1] + floorPlan[index + 10];
    }

    private bool VisitCell(int index)
    {
        if (floorPlan[index] != 0 || GetNeighbourCount(index) > 1 ||
            floorPlanCount > maxRooms || Random.value < 0.5f)
            return false;

        cellQueue.Enqueue(index);
        floorPlan[index] = 1;
        floorPlanCount++;

        SpawnRoom(index);

        return true;
    }

    private void SpawnRoom(int index)
    {
        // Calculate grid position
        int x = index % 10;
        int y = index / 10;

        // Create new cell as UI element
        Cell newCell = Instantiate(cellPrefab, mapContainer);

        // Configure RectTransform
        RectTransform rectTransform = newCell.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
        rectTransform.sizeDelta = new Vector2(cellSize, cellSize);

        // Ensure the anchors are set properly for UI positioning
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Set cell properties
        newCell.value = 1;
        newCell.index = index;

        spawnedCells.Add(newCell);
    }
}
