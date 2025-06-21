using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackValidator
{
    public static bool ValidateAttackScenario(List<EntityManager> enemyEntities,
                                            List<EntityManager> playerEntities,
                                            HealthIconManager playerHealthIcon)
    {
        bool hasEnemyEntities = enemyEntities != null && enemyEntities.Count > 0;
        bool hasTargets = (playerEntities != null && playerEntities.Count > 0) || playerHealthIcon != null;

        // Log issues for debugging
        if (!hasEnemyEntities)
            Debug.Log("[AttackValidator] No enemy entities available to attack");

        if (!hasTargets)
            Debug.Log("[AttackValidator] No valid targets available");

        // Both conditions must be true for a valid attack scenario
        return hasEnemyEntities && hasTargets;
    }

    public static bool ValidateCombatState(ICombatManager combatManager)
    {
        // Check for null manager first
        if (combatManager == null)
        {
            Debug.LogWarning("[AttackValidator] Cannot validate combat state: manager is null");
            return false;
        }

        try
        {
            // Check if we're in enemy combat phase
            bool isValidPhase = combatManager.IsEnemyCombatPhase();
            
            // IMPROVED: Explicitly check that it's NOT the player's turn
            bool isEnemyTurn = !combatManager.PlayerTurn;
            
            // Log detailed phase and turn information for debugging
            if (!isValidPhase)
            {
                Debug.LogWarning($"[AttackValidator] Not in enemy combat phase. Current phase: {combatManager.CurrentPhase}");
            }
            
            if (!isEnemyTurn)
            {
                Debug.LogWarning($"[AttackValidator] It's the player's turn (PlayerTurn: {combatManager.PlayerTurn}), enemy cannot attack");
            }
            
            // Both phase and turn must be correct to allow attack
            return isValidPhase && isEnemyTurn;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AttackValidator] Error checking combat state: {e.Message}");
            return false;
        }
    }

    public static bool ValidateAttackPrerequisites(ICombatManager combatManager, SpritePositioning spritePositioning)
    {
        bool hasCombatManager = combatManager != null;
        bool hasSpritePositioning = spritePositioning != null;

        if (!hasCombatManager)
            Debug.LogWarning("[AttackValidator] CombatManager is null");

        if (!hasSpritePositioning)
            Debug.LogWarning("[AttackValidator] SpritePositioning is null");

        // IMPROVED: Add more detailed logging with current turn and phase information
        if (hasCombatManager)
        {
            Debug.Log($"[AttackValidator] Combat state check - Phase: {combatManager.CurrentPhase}, " +
                      $"PlayerTurn: {combatManager.PlayerTurn}, PlayerGoesFirst: {combatManager.PlayerGoesFirst}");
            
            return ValidateCombatState(combatManager);
        }

        return false;
    }

    // NEW: Helper method that can be called at room transitions to log state
    public static void LogCombatStateTransition(ICombatManager combatManager, string context)
    {
        if (combatManager == null)
        {
            Debug.LogWarning("[AttackValidator] Cannot log state: manager is null");
            return;
        }

        Debug.Log($"[AttackValidator] {context} - Phase: {combatManager.CurrentPhase}, " +
                  $"Turn: {combatManager.TurnCount}, " +
                  $"PlayerTurn: {combatManager.PlayerTurn}, " +
                  $"PlayerGoesFirst: {combatManager.PlayerGoesFirst}");
    }

    // NEW: Add a general-purpose method for validating any enemy action
    public static bool ValidateEnemyAction(ICombatManager combatManager, CombatPhase requiredPhase)
    {
        // Check for null manager first
        if (combatManager == null)
        {
            Debug.LogWarning("[AttackValidator] Cannot validate enemy action: manager is null");
            return false;
        }

        try
        {
            bool isValidPhase = false;
            
            // Check if we're in the specified phase, or any enemy phase if none specified
            if (requiredPhase == CombatPhase.EnemyCombat)
            {
                isValidPhase = combatManager.IsEnemyCombatPhase();
            }
            else if (requiredPhase == CombatPhase.EnemyPrep)
            {
                isValidPhase = combatManager.IsEnemyPrepPhase();
            }
            else
            {
                // If no specific phase required, check for any enemy phase
                isValidPhase = combatManager.IsEnemyPrepPhase() || combatManager.IsEnemyCombatPhase();
            }
            
            // Explicitly check that it's NOT the player's turn
            bool isEnemyTurn = !combatManager.PlayerTurn;
            
            // Log detailed phase and turn information for debugging
            if (!isValidPhase || !isEnemyTurn)
            {
                Debug.LogWarning($"[AttackValidator] Enemy action validation failed - " +
                                 $"Phase: {combatManager.CurrentPhase} (required: {requiredPhase}), " +
                                 $"PlayerTurn: {combatManager.PlayerTurn}, " +
                                 $"Valid: {isValidPhase && isEnemyTurn}");
            }
            
            // Both phase and turn must be correct to allow action
            return isValidPhase && isEnemyTurn;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AttackValidator] Error validating enemy action: {e.Message}");
            return false;
        }
    }
}
