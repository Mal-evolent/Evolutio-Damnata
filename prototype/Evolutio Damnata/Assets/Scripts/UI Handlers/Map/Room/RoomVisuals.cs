using UnityEngine;
using UnityEngine.UI;

public class RoomVisuals : IRoomVisuals
{
    private Image roomImage;
    private Color normalColor;
    private Color currentRoomColor;
    private Color clearedColor;

    public RoomVisuals(Color normal, Color current, Color cleared)
    {
        normalColor = normal;
        currentRoomColor = current;
        clearedColor = cleared;
    }

    public void SetRoomImage(Image image)
    {
        roomImage = image;
    }

    public void UpdateVisuals(bool isCurrentRoom, bool isCleared)
    {
        if (roomImage == null) return;

        if (isCurrentRoom)
        {
            roomImage.color = currentRoomColor;
        }
        else if (isCleared)
        {
            roomImage.color = clearedColor;
        }
        else
        {
            roomImage.color = normalColor;
        }
    }
} 