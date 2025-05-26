using UnityEngine;

public interface IMapVisualizer
{
    void Initialize(RectTransform container);
    void ClearMap();
    void VisualizeRoom(int index, int x, int y, RoomType roomType = RoomType.Normal);
    void CenterMapInContainer(float cellSize);
}
