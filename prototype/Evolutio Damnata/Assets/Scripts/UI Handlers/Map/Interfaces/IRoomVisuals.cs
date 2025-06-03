using UnityEngine.UI;

public interface IRoomVisuals
{
    void UpdateVisuals(bool isCurrentRoom, bool isCleared);
    void SetRoomImage(Image image);
} 