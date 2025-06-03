using UnityEngine;

public abstract class BaseRoomBehavior : IRoomBehavior
{
    protected readonly IRoomState roomState;
    protected readonly Cell cellComponent;
    protected readonly ICombatTrigger combatTrigger;

    protected BaseRoomBehavior(IRoomState roomState, Cell cellComponent, ICombatTrigger combatTrigger)
    {
        this.roomState = roomState;
        this.cellComponent = cellComponent;
        this.combatTrigger = combatTrigger;
    }

    public virtual bool CanEnterRoom(IRoomState fromRoom)
    {
        if (roomState.IsCurrentRoom)
        {
            Debug.LogWarning("Cannot enter current room");
            return false;
        }
        
        if (roomState.IsCleared) return true;
        
        if (fromRoom != null && fromRoom.IsCleared)
        {
            var fromRoomState = fromRoom as RoomState;
            if (fromRoomState != null && cellComponent.IsAdjacentTo(fromRoomState.CellComponent))
            {
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot enter room: Must be adjacent to a cleared room");
                return false;
            }
        }
        
        Debug.LogWarning("Cannot enter room: Must be adjacent to a cleared room or the room must be cleared");
        return false;
    }

    public abstract void OnRoomEnter(RoomState fromRoom);
} 