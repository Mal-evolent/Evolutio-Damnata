using UnityEngine;

public class NormalRoomBehavior : BaseRoomBehavior
{
    public NormalRoomBehavior(IRoomState roomState, Cell cellComponent, ICombatTrigger combatTrigger) 
        : base(roomState, cellComponent, combatTrigger)
    {
    }

    public override void OnRoomEnter(RoomState fromRoom)
    {
        if (!CanEnterRoom(fromRoom)) return;

        if (Random.value < 0.7f) // 70% chance
        {
            combatTrigger.TriggerCombat(cellComponent.Index);
        }
        else
        {
            roomState.SetAsCleared();
        }

        roomState.SetAsCurrentRoom();
    }
} 