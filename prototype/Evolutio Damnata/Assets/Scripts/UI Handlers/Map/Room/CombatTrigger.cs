using GameManagement;
using UnityEngine;

public class CombatTrigger : ICombatTrigger
{
    private IRoomState currentCombatRoom;

    public void TriggerCombat(int roomIndex)
    {
        Debug.Log($"[CombatTrigger] Combat initiated in room {roomIndex} at position {Time.time:F2}s");

        // Store the current room reference
        currentCombatRoom = RoomState.GetCurrentRoom();

        // Set the combat active flag to prevent room transitions during combat
        GameStateManager.IsCombatActive = true;

        // Find the CombatManager in the scene
        var combatManager = Object.FindObjectOfType<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogError("[CombatTrigger] Could not find CombatManager in the scene!");
            GameStateManager.IsCombatActive = false; // Reset flag if manager not found
            return;
        }

        // Subscribe to enemy defeated event
        combatManager.OnEnemyDefeated += OnEnemyDefeated;

        // Reset the combat manager's state
        combatManager.ResetPhaseState();

        // Start the initialization chain
        combatManager.StartCoroutine(combatManager.WaitForInitialization());

        Debug.Log($"[CombatTrigger] Room state: {roomIndex} - Combat phase starting");
    }

    private void OnEnemyDefeated()
    {
        Debug.Log("[CombatTrigger] Enemy defeated, marking room as cleared");

        if (currentCombatRoom != null)
        {
            currentCombatRoom.SetAsCleared();

            // Make sure it's still current
            if (currentCombatRoom is RoomState roomState)
            {
                roomState.SetAsCurrentRoom();
            }

            // Reset the combat active flag to allow room transitions
            GameStateManager.IsCombatActive = false;

            // Unsubscribe from the event to avoid memory leaks
            var combatManager = Object.FindObjectOfType<CombatManager>();
            if (combatManager != null)
            {
                combatManager.OnEnemyDefeated -= OnEnemyDefeated;
            }
        }
    }
}
