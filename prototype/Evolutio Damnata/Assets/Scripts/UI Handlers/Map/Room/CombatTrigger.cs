using UnityEngine;

public class CombatTrigger : ICombatTrigger
{
    public void TriggerCombat(int roomIndex)
    {
        // TODO: Implement combat initialization
        Debug.LogWarning($"[CombatTrigger] Combat initiated in room {roomIndex} at position {Time.time:F2}s");
        Debug.LogWarning($"[CombatTrigger] Room state: {roomIndex} - Combat phase starting");
    }
} 