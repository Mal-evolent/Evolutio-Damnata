using UnityEngine;
using UnityEngine.UI;

public class RoomVisuals : IRoomVisuals
{
    private Image roomImage;
    private Color normalColor;
    private Color currentRoomColor;
    private Color clearedColor;
    private Color currentClearedColor;

    public RoomVisuals(Color normal, Color current, Color cleared, Color currentCleared)
    {
        normalColor = normal;
        currentRoomColor = current;
        clearedColor = cleared;
        currentClearedColor = currentCleared;
    }

    public void SetRoomImage(Image image)
    {
        roomImage = image;
    }

    public void UpdateVisuals(bool isCurrentRoom, bool isCleared)
    {
        if (roomImage == null) return;

        if (isCurrentRoom && isCleared)
        {
            roomImage.color = currentClearedColor; // Yellow for current cleared room
        }
        else if (isCurrentRoom)
        {
            roomImage.color = currentRoomColor; // Red for current uncleared room
        }
        else if (isCleared)
        {
            roomImage.color = clearedColor; // Green for non-current cleared room
        }
        else
        {
            roomImage.color = normalColor; // White for normal room
        }
    }
}
