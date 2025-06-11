using Sirenix.OdinInspector.Editor.Validation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ICell
{
    int Index { get; set; }
    int Value { get; set; }
    RoomType? RoomType { get; set; }
    void SetSpecialRoomSprite(Sprite icon);
    void Reset();
    void SetCellSize(float size);
    float CellSize { get; }
}

public class Cell : MonoBehaviour, ICell
{
    [SerializeField] private int index;
    [SerializeField] private int value;
    [SerializeField] private RoomType? roomType;

    [Tooltip("Size of the cell in grid units. Used to determine adjacency.")]
    [SerializeField] private float cellSize = 60f;

    private Image cellImage;
    private Sprite defaultSprite;
    private Button button;
    private Room room;

    public int Index
    {
        get => index;
        set
        {
            if (value < 0)
            {
                Debug.LogWarning($"Attempted to set negative index: {value}");
                return;
            }
            index = value;
        }
    }

    public int Value
    {
        get => value;
        set
        {
            if (value < 0)
            {
                Debug.LogWarning($"Attempted to set negative value: {value}");
                return;
            }
            this.value = value;
        }
    }

    public RoomType? RoomType
    {
        get => roomType;
        set
        {
            roomType = value;
            if (room != null)
            {
                room.ResetRoom();
            }
        }
    }

    /// <summary>
    /// Sets the cell size value used for adjacency calculations.
    /// </summary>
    /// <param name="size">The size of the cell in grid units</param>
    public void SetCellSize(float size)
    {
        if (size <= 0)
        {
            Debug.LogWarning($"Attempted to set invalid cell size: {size}");
            return;
        }
        cellSize = size;
    }

    /// <summary>
    /// Gets the current cell size value.
    /// </summary>
    public float CellSize => cellSize;

    void Awake()
    {
        // Get reference to the Image component on this GameObject
        cellImage = GetComponent<Image>();
        button = GetComponent<Button>();
        room = GetComponent<Room>();

        if (cellImage == null)
        {
            Debug.LogError("Cell GameObject must have an Image component attached directly to it");
            enabled = false;
            return;
        }

        if (button == null)
        {
            Debug.LogError("Cell GameObject must have a Button component attached directly to it");
            enabled = false;
            return;
        }

        if (room == null)
        {
            Debug.LogError("Cell GameObject must have a Room component attached directly to it");
            enabled = false;
            return;
        }

        // Store the default sprite for reset functionality
        defaultSprite = cellImage.sprite;

        // Add click listener
        button.onClick.AddListener(OnCellClicked);
    }

    private void OnCellClicked()
    {
        if (room != null)
        {
            room.OnRoomEnter();
        }
    }

    public void SetSpecialRoomSprite(Sprite icon)
    {
        if (cellImage == null)
        {
            Debug.LogError("Cannot set sprite: Image component is missing");
            return;
        }

        if (icon == null)
        {
            Debug.LogWarning("Attempted to set null sprite");
            return;
        }

        cellImage.sprite = icon;
    }

    public void DisplayRoomType()
    {
        Debug.LogWarning($"Room type: {RoomType}");
        // Do something with the index here
    }

    public void Reset()
    {
        if (cellImage != null)
        {
            cellImage.sprite = defaultSprite;
        }
        value = 0;
        roomType = null;
        if (room != null)
        {
            room.ResetRoom();
        }
    }

    /// <summary>
    /// Checks if this cell is adjacent to another cell based on the cell size.
    /// </summary>
    /// <param name="other">The cell to check adjacency with</param>
    /// <returns>True if the cells are adjacent, false otherwise</returns>
    public bool IsAdjacentTo(Cell other)
    {
        if (other == null) return false;

        // Get the positions of both cells
        RectTransform thisRect = GetComponent<RectTransform>();
        RectTransform otherRect = other.GetComponent<RectTransform>();

        if (thisRect == null || otherRect == null) return false;

        Vector2 thisPos = thisRect.anchoredPosition;
        Vector2 otherPos = otherRect.anchoredPosition;

        // Calculate the distance between cells
        float distance = Vector2.Distance(thisPos, otherPos);

        // Cells are adjacent if they are exactly cellSize units apart
        return Mathf.Approximately(distance, cellSize);
    }
}
