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

    [SerializeField] private Cell cellPrefab;

    [Header("Sprite References")]
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private Sprite shopSprite;
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private Sprite secretSprite;

    [Header("Strategy Dependencies")]
    [SerializeField] private IMapGenerationStrategy mapGenerationStrategy;
    [SerializeField] private IRoomSelectionStrategy roomSelectionStrategy;
    [SerializeField] private IMapVisualizer mapVisualizer;

    private int[] floorPlan;
    private Dictionary<RoomType, int> specialRooms = new Dictionary<RoomType, int>();

    private void ValidateConfiguration()
    {
        if (config == null)
        {
            throw new System.ArgumentNullException(nameof(config), "MapGenerationConfig is required");
        }

        if (config.gridSize <= 0)
        {
            throw new System.ArgumentException("Grid size must be greater than 0", nameof(config.gridSize));
        }

        if (config.minRooms <= 0)
        {
            throw new System.ArgumentException("Minimum rooms must be greater than 0", nameof(config.minRooms));
        }
    }

    private void ValidateDependencies()
    {
        if (mapContainer == null)
        {
            mapContainer = GetComponent<RectTransform>();
            if (mapContainer == null)
            {
                throw new System.NullReferenceException("MapGenerator must be attached to a GameObject with a RectTransform or have a mapContainer assigned.");
            }
        }

        if (cellPrefab == null)
        {
            throw new System.NullReferenceException("Cell prefab must be assigned");
        }

        if (mapGenerationStrategy == null)
        {
            mapGenerationStrategy = new RandomDungeonGenerationStrategy();
        }

        if (roomSelectionStrategy == null)
        {
            roomSelectionStrategy = new StandardRoomSelectionStrategy(config);
        }

        if (mapVisualizer == null)
        {
            InitializeMapVisualizer();
        }
    }

    private void InitializeMapVisualizer()
    {
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

    void Awake()
    {
        try
        {
            ValidateConfiguration();
            ValidateDependencies();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize MapGenerator: {e.Message}");
            enabled = false;
        }
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
        try
        {
            // Clear previous map
            mapVisualizer.ClearMap();

            // Generate floor plan
            List<int> endRooms;
            floorPlan = mapGenerationStrategy.GenerateFloorPlan(config, out endRooms);

            // Validate floor plan
            if (!ValidateFloorPlan(endRooms))
            {
                GenerateMap();
                return;
            }

            // Select and validate special rooms
            if (!SelectAndValidateSpecialRooms(endRooms))
            {
                GenerateMap();
                return;
            }

            // Visualize the map
            VisualizeMap();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to generate map: {e.Message}");
        }
    }

    private bool ValidateFloorPlan(List<int> endRooms)
    {
        int roomCount = floorPlan.Count(cell => cell == 1);
        if (roomCount < config.minRooms)
        {
            return false;
        }
        return true;
    }

    private bool SelectAndValidateSpecialRooms(List<int> endRooms)
    {
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
            Debug.LogWarning("Failed to assign all special rooms");
            return false;
        }
        return true;
    }

    private void VisualizeMap()
    {
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
