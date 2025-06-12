using UnityEngine;

public class NormalRoomBehavior : BaseRoomBehavior
{
    // Default combat chance of 70%
    public static float DefaultCombatChance => 0.5f;

    // Public property for combat encounter chance that can be set by external classes
    public float CombatChance { get; set; } = DefaultCombatChance;

    // Protected virtual property for random value generation (for testing purposes)
    protected virtual float RandomValue => Random.value;

    public NormalRoomBehavior(IRoomState roomState, Cell cellComponent, ICombatTrigger combatTrigger)
        : base(roomState, cellComponent, combatTrigger)
    {
    }

    public override void OnRoomEnter(RoomState fromRoom)
    {
        if (!CanEnterRoom(fromRoom)) return;

        // First mark this as the current room
        roomState.SetAsCurrentRoom();

        // Only attempt to trigger combat if room is not already cleared
        if (!roomState.IsCleared)
        {
            // Use the combat chance property instead of hardcoded value
            if (RandomValue < CombatChance)
            {
                combatTrigger.TriggerCombat(cellComponent.Index);
                // Don't automatically clear the room - this will happen when combat ends
            }
            else
            {
                // No combat occurs, so mark the room as cleared immediately
                // This allows the player to progress to adjacent rooms
                Debug.Log($"[NormalRoomBehavior] No combat in room {cellComponent.Index}, marking as cleared");
                roomState.SetAsCleared();
                // Set it back as the current room since SetAsCleared() may reset it
                roomState.SetAsCurrentRoom();
            }
        }
    }
}
