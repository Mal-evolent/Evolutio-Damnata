using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles procedural generation and visualization of the dungeon map.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    /// <summary>
    /// Configuration settings for map generation.
    /// </summary>
    [SerializeField] private MapGenerationConfig config = new MapGenerationConfig();

    /// <summary>
    /// Parent transform for the generated map (usually a panel under Canvas).
    /// </summary>
    [Tooltip("Parent transform for the generated map (usually a panel under Canvas)")]
    [SerializeField] private RectTransform mapContainer;

    /// <summary>
    /// Prefab for individual cells that make up the map.
    /// </summary>
    [SerializeField] private Cell cellPrefab;

    [Header("Sprite References")]
    /// <summary>
    /// Sprite used for item rooms.
    /// </summary>
    [SerializeField] private Sprite itemSprite;

    /// <summary>
    /// Sprite used for shop rooms.
    /// </summary>
    [SerializeField] private Sprite shopSprite;

    /// <summary>
    /// Sprite used for boss rooms.
    /// </summary>
    [SerializeField] private Sprite bossSprite;

    /// <summary>
    /// Sprite used for secret rooms.
    /// </summary>
    [SerializeField] private Sprite secretSprite;

    [Header("Strategy Dependencies")]
    /// <summary>
    /// Strategy that handles the generation of the floor plan.
    /// </summary>
    [SerializeField] private IMapGenerationStrategy mapGenerationStrategy;

    /// <summary>
    /// Strategy that selects which rooms should be special rooms.
    /// </summary>
    [SerializeField] private IRoomSelectionStrategy roomSelectionStrategy;

    /// <summary>
    /// Handler for visualizing the map in the UI.
    /// </summary>
    [SerializeField] private IMapVisualizer mapVisualizer;

    [Header("Background Settings")]
    /// <summary>
    /// Canvas that will receive the background image.
    /// </summary>
    [SerializeField] private Canvas mainCanvas;

    /// <summary>
    /// Currently selected room background image name.
    /// </summary>
    [Tooltip("Reference to the canvas that will receive the background image")]
    public string currentSelectedRoom = "None";

    /// <summary>
    /// Array representation of the floor plan where 1 indicates a room.
    /// </summary>
    private int[] floorPlan;

    /// <summary>
    /// Dictionary mapping room types to their indices in the floor plan.
    /// </summary>
    private Dictionary<RoomType, int> specialRooms = new Dictionary<RoomType, int>();

    /// <summary>
    /// Reference to the canvas RectTransform for background generation.
    /// </summary>
    private RectTransform canvasRectTransform;

    /// <summary>
    /// Set of room indices that have already been processed during visualization.
    /// </summary>
    private HashSet<int> processedRoomIndices = new HashSet<int>();

    /// <summary>
    /// Validates that the map generation configuration is valid.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when grid size or minimum rooms is invalid.</exception>
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

    /// <summary>
    /// Validates and initializes required dependencies for map generation.
    /// </summary>
    /// <exception cref="System.NullReferenceException">Thrown when required dependencies are missing.</exception>
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

    /// <summary>
    /// Initializes the map visualizer with room sprites.
    /// </summary>
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

        // Set the cell size from the configuration
        mapVisualizer.SetCellSize(config.cellSize);
    }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Validates configuration and dependencies.
    /// </summary>
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

            // Make sure the map visualizer has the correct cell size
            if (mapVisualizer != null)
            {
                mapVisualizer.SetCellSize(config.cellSize);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize MapGenerator: {e.Message}");
            enabled = false;
        }
    }

    /// <summary>
    /// Called on the frame when a script is enabled.
    /// Generates the initial map.
    /// </summary>
    void Start()
    {
        GenerateMap();
    }

    /// <summary>
    /// Called every frame.
    /// Regenerates the map when space is pressed.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateMap();
        }
    }

    /// <summary>
    /// Generates a new procedural map with rooms.
    /// </summary>
    public void GenerateMap()
    {
        try
        {
            // Ensure visualizer has the correct cell size
            mapVisualizer.SetCellSize(config.cellSize);

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

    /// <summary>
    /// Generates a random background image for the dungeon.
    /// </summary>
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

    /// <summary>
    /// Validates that the generated floor plan meets minimum requirements.
    /// </summary>
    /// <param name="endRooms">List of room indices that are end rooms (dead ends).</param>
    /// <returns>True if the floor plan is valid, false otherwise.</returns>
    private bool ValidateFloorPlan(List<int> endRooms)
    {
        int roomCount = floorPlan.Count(cell => cell == 1);
        if (roomCount < config.minRooms)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Selects and validates which rooms should be special rooms (boss, item, shop, secret).
    /// </summary>
    /// <param name="endRooms">List of room indices that are end rooms (dead ends).</param>
    /// <returns>True if all required special rooms were successfully assigned, false otherwise.</returns>
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

    /// <summary>
    /// Creates visual representations of rooms in the UI based on the floor plan.
    /// </summary>
    private void VisualizeMap()
    {
        // Clear the processed indices set
        processedRoomIndices.Clear();

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

                // Skip if this room has already been processed
                if (processedRoomIndices.Contains(i))
                {
                    Debug.LogWarning($"[MapGenerator] Room at index {i} (x:{x}, y:{y}) was already processed. Skipping duplicate.");
                    continue;
                }

                Cell cell = mapVisualizer.VisualizeRoom(i, x, y);
                processedRoomIndices.Add(i);

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

                // Skip if this room has already been processed
                if (processedRoomIndices.Contains(index))
                {
                    Debug.LogWarning($"[MapGenerator] Special room {roomEntry.Key} at index {index} (x:{x}, y:{y}) was already processed. Skipping duplicate.");
                    continue;
                }

                mapVisualizer.VisualizeRoom(index, x, y, roomEntry.Key);
                processedRoomIndices.Add(index);
            }
        }

        // Center the map (will use the correct cell size)
        mapVisualizer.CenterMapInContainer(config.cellSize);
    }
}
