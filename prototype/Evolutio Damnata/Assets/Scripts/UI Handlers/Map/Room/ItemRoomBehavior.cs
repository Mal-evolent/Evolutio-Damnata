using UnityEngine;

public class ItemRoomBehavior : BaseRoomBehavior
{
    public ItemRoomBehavior(IRoomState roomState, Cell cellComponent, ICombatTrigger combatTrigger)
        : base(roomState, cellComponent, combatTrigger)
    {
    }

    public override void OnRoomEnter(RoomState fromRoom)
    {
        if (!CanEnterRoom(fromRoom)) return;
        // TODO: Implement item room logic
        roomState.SetAsCleared();
        roomState.SetAsCurrentRoom();
    }
} 