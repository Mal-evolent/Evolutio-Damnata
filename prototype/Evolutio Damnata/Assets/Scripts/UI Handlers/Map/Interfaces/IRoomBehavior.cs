public interface IRoomBehavior
{
    void OnRoomEnter(RoomState fromRoom);
    bool CanEnterRoom(IRoomState fromRoom);
} 