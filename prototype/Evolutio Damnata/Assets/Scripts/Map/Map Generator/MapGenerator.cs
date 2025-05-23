using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private MapGenerationConfig config = new MapGenerationConfig();

    [Tooltip("Parent transform for the generated map (usually a panel under Canvas)")]
    [SerializeField] private RectTransform mapContainer;

    public Cell cellPrefab;

    [Header("Sprite References")]
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private Sprite shopSprite;
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private Sprite secretSprite;

    // Strategy implementations
    private IMapGenerationStrategy mapGenerationStrategy;
    private IRoomSelectionStrategy roomSelectionStrategy;
    private IMapVisualizer mapVisualizer;

    private int[] floorPlan;
    private Dictionary<RoomType, int> specialRooms = new Dictionary<RoomType, int>();

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

        // Initialize strategies
        InitializeStrategies();
    }

    private void InitializeStrategies()
    {
        mapGenerationStrategy = new RandomDungeonGenerationStrategy();
        roomSelectionStrategy = new StandardRoomSelectionStrategy(config);

        // Set up sprite dictionary
        Dictionary<RoomType, Sprite> roomSprites = new Dictionary<RoomType, Sprite>
        {
            { RoomType.Boss, bossSprite },
            { RoomType.Item, itemSprite },
            { RoomType.Shop, shopSprite },
            { RoomType.Secret, secretSprite }
        };

        mapVisualizer = new UIMapVisualizer(cellPrefab, roomSprites);
        mapVisualizer.Initialize(mapContainer);
    }

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateMap();
        }
    }

    public void GenerateMap()
    {
        // Clear previous map
        mapVisualizer.ClearMap();

        // Generate floor plan
        List<int> endRooms;
        floorPlan = mapGenerationStrategy.GenerateFloorPlan(config, out endRooms);

        // Check if we have enough rooms
        int roomCount = floorPlan.Count(cell => cell == 1);
        if (roomCount < config.minRooms)
        {
            GenerateMap();
            return;
        }

        // Select special rooms
        specialRooms = roomSelectionStrategy.SelectSpecialRooms(floorPlan, endRooms);

        // Find secret room
        int secretRoomIndex = roomSelectionStrategy.FindSecretRoomLocation(floorPlan, specialRooms);
        if (secretRoomIndex != -1)
        {
            specialRooms[RoomType.Secret] = secretRoomIndex;
        }

        // Verify all special rooms were assigned
        if (specialRooms.Values.Any(index => index == -1) || secretRoomIndex == -1)
        {
            GenerateMap();
            return;
        }

        // Visualize regular rooms
        for (int i = 0; i < floorPlan.Length; i++)
        {
            if (floorPlan[i] == 1)
            {
                int x = i % config.gridSize;
                int y = i / config.gridSize;
                mapVisualizer.VisualizeRoom(i, x, y);
            }
        }

        // Visualize special rooms
        foreach (var roomEntry in specialRooms)
        {
            int index = roomEntry.Value;
            if (index != -1)
            {
                int x = index % config.gridSize;
                int y = index / config.gridSize;
                mapVisualizer.VisualizeRoom(index, x, y, roomEntry.Key);
            }
        }

        // Center the map
        mapVisualizer.CenterMapInContainer(config.cellSize);
    }
}
