using UnityEngine;

public class BossRoomBehavior : BaseRoomBehavior
{
    public BossRoomBehavior(IRoomState roomState, Cell cellComponent, ICombatTrigger combatTrigger)
        : base(roomState, cellComponent, combatTrigger)
    {
    }

    public override void OnRoomEnter(RoomState fromRoom)
    {
        if (!CanEnterRoom(fromRoom)) return;
        combatTrigger.TriggerCombat(cellComponent.Index);
        roomState.SetAsCurrentRoom();
    }
} 