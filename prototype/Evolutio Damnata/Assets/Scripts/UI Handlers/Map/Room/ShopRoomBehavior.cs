using UnityEngine;

public class ShopRoomBehavior : BaseRoomBehavior
{
    public ShopRoomBehavior(IRoomState roomState, Cell cellComponent, ICombatTrigger combatTrigger)
        : base(roomState, cellComponent, combatTrigger)
    {
    }

    public override void OnRoomEnter(RoomState fromRoom)
    {
        if (!CanEnterRoom(fromRoom)) return;
        // TODO: Implement shop logic
        roomState.SetAsCleared();
        roomState.SetAsCurrentRoom();
    }
} 