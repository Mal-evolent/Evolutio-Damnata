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
}

public class Cell : MonoBehaviour, ICell
{
    [SerializeField] private int index;
    [SerializeField] private int value;
    [SerializeField] private RoomType? roomType;

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
}
