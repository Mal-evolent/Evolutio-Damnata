using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Background Settings")]
    [SerializeField] private Canvas mainCanvas;
    [Tooltip("Reference to the canvas that will receive the background image")]
    public string currentSelectedRoom = "None";

    private int[] floorPlan;
    private Dictionary<RoomType, int> specialRooms = new Dictionary<RoomType, int>();
    private RectTransform canvasRectTransform;

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

        if (mainCanvas == null)
        {
            Debug.LogWarning("Main canvas not assigned. Background image will not be generated.");
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

            if (mainCanvas != null)
            {
                canvasRectTransform = mainCanvas.GetComponent<RectTransform>();
            }
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
            // Generate background first (if canvas is assigned)
            GenerateBackgroundImage();

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

    private void GenerateBackgroundImage()
    {
        if (mainCanvas == null || canvasRectTransform == null) return;

        // Create initial texture
        Texture2D drawOnTex = new Texture2D((int)canvasRectTransform.rect.width, (int)canvasRectTransform.rect.height, TextureFormat.ARGB4444, true);
        Sprite textTex = Sprite.Create(drawOnTex, new Rect(0, 0, drawOnTex.width, drawOnTex.height), Vector2.zero);
        mainCanvas.GetComponent<Image>().sprite = textTex;

        // Set a random background image for the mainCanvas
        GlobalResources globalResources = GameObject.Find("ResourceManagaer")?.GetComponent<GlobalResources>();
        if (globalResources == null)
        {
            Debug.LogError("GlobalResources not found!");
            return;
        }

        if (globalResources.dungeonRooms == null || globalResources.dungeonRooms.Count == 0)
        {
            Debug.LogError("Dungeon rooms list is null or empty!");
            return;
        }

        Sprite newBackgroundImage = globalResources.dungeonRooms[Random.Range(0, globalResources.dungeonRooms.Count)];
        currentSelectedRoom = newBackgroundImage.name;
        mainCanvas.GetComponent<Image>().sprite = newBackgroundImage;
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
        // Find the starting room (middle of the grid)
        int startIndex = (config.gridRows / 2) * config.gridSize + (config.gridSize / 2);
        Room startingRoom = null;

        // Visualize regular rooms
        for (int i = 0; i < floorPlan.Length; i++)
        {
            if (floorPlan[i] == 1)
            {
                int x = i % config.gridSize;
                int y = i / config.gridSize;
                Cell cell = mapVisualizer.VisualizeRoom(i, x, y);
                
                // If this is the starting room, mark it as cleared and current using the new API
                if (i == startIndex)
                {
                    startingRoom = cell.GetComponent<Room>();
                    if (startingRoom != null)
                    {
                        startingRoom.ForceClearAndSetCurrent();
                    }
                }
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
