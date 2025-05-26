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
        set => roomType = value;
    }

    void Awake()
    {
        // Get reference to the Image component on this GameObject
        cellImage = GetComponent<Image>();

        if (cellImage == null)
        {
            Debug.LogError("Cell GameObject must have an Image component attached directly to it");
            enabled = false;
            return;
        }

        // Store the default sprite for reset functionality
        defaultSprite = cellImage.sprite;
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

    public void Reset()
    {
        if (cellImage != null)
        {
            cellImage.sprite = defaultSprite;
        }
        value = 0;
        roomType = null;
    }
}
