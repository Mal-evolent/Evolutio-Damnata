using System.Collections.Generic;

public interface IRoomSelectionStrategy
{
    Dictionary<RoomType, int> SelectSpecialRooms(int[] floorPlan, List<int> endRooms);
    int FindSecretRoomLocation(int[] floorPlan, Dictionary<RoomType, int> specialRooms);
}
