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

        // First stop all enemy-related coroutines to prevent stale execution
        var enemyActions = Object.FindObjectOfType<EnemyInteraction.EnemyActions>();
        if (enemyActions != null)
        {
            // Stop all coroutines on the EnemyActions to prevent stale state execution
            ((MonoBehaviour)enemyActions).StopAllCoroutines();
            Debug.Log("[CombatTrigger] Stopped all EnemyActions coroutines");

            // Use the public method to reset state
            enemyActions.ResetActionState();
        }

        // Also try to find and stop any AttackManager coroutines
        var attackManager = Object.FindObjectOfType<EnemyInteraction.Managers.AttackManager>();
        if (attackManager != null)
        {
            ((MonoBehaviour)attackManager).StopAllCoroutines();
            Debug.Log("[CombatTrigger] Stopped all AttackManager coroutines");
        }

        // FIX: Ensure clean state for each new combat by stopping all old coroutines
        // and resetting the state manually.
        combatManager.StopAllCoroutines();
        combatManager.TurnCount = 0;
        combatManager.PlayerGoesFirst = true;
        combatManager.PlayerTurn = true;
        combatManager.ResetPhaseState(); // This sets CurrentPhase to None

        // Now, resubscribe to the event after stopping coroutines
        combatManager.OnEnemyDefeated += OnEnemyDefeated;

        Debug.Log("[CombatTrigger] Reset combat state for new room - PlayerGoesFirst: true");

        // Log detailed combat state using our new helper
        AttackValidator.LogCombatStateTransition(combatManager, $"Room {roomIndex} combat initialization");

        // Reset the combat manager's state
        // combatManager.ResetPhaseState(); // This is now called above

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