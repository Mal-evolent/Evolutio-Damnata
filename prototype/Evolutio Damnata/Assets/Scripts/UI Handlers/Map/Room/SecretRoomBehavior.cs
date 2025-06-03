using UnityEngine;

public class SecretRoomBehavior : BaseRoomBehavior
{
    public SecretRoomBehavior(IRoomState roomState, Cell cellComponent, ICombatTrigger combatTrigger)
        : base(roomState, cellComponent, combatTrigger)
    {
    }

    public override void OnRoomEnter(RoomState fromRoom)
    {
        if (!CanEnterRoom(fromRoom)) return;
        // TODO: Implement secret room logic
        roomState.SetAsCleared();
        roomState.SetAsCurrentRoom();
    }
} 