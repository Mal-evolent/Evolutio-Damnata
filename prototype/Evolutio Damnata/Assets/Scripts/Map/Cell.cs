using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    public int index;
    public int value;

    private Image cellImage;

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
