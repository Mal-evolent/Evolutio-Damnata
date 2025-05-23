using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ICell
{
    int Index { get; set; }
    int Value { get; set; }
    void SetSpecialRoomSprite(Sprite icon);
}

public class Cell : MonoBehaviour, ICell
{
    [SerializeField] private int index;
    [SerializeField] private int value;

    private Image cellImage;

    public int Index
    {
        get => index;
        set => index = value;
    }

    public int Value
    {
        get => value;
        set => this.value = value;
    }

    void Awake()
    {
        // Get reference to the Image component on this GameObject
        cellImage = GetComponent<Image>();

        if (cellImage == null)
        {
            Debug.LogError("Cell GameObject must have an Image component attached directly to it");
        }
    }

    public void SetSpecialRoomSprite(Sprite icon)
    {
        if (cellImage != null)
        {
            cellImage.sprite = icon;
        }
    }
}
