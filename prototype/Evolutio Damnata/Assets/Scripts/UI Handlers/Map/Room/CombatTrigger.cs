using UnityEngine;

public class CombatTrigger : ICombatTrigger
{
    public void TriggerCombat(int roomIndex)
    {
        Debug.Log($"[CombatTrigger] Combat initiated in room {roomIndex} at position {Time.time:F2}s");
        
        // Find the CombatManager in the scene
        var combatManager = Object.FindObjectOfType<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogError("[CombatTrigger] Could not find CombatManager in the scene!");
            return;
        }

        // Reset the combat manager's state
        combatManager.ResetPhaseState();
        
        // Start the initialization chain
        combatManager.StartCoroutine(combatManager.WaitForInitialization());
        
        Debug.Log($"[CombatTrigger] Room state: {roomIndex} - Combat phase starting");
    }
} 