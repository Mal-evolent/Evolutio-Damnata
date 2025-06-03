using UnityEngine;

public class CombatTrigger : ICombatTrigger
{
    public void TriggerCombat(int roomIndex)
    {
        // TODO: Implement combat initialization
        Debug.Log($"Triggering combat in room {roomIndex}");
    }
} 