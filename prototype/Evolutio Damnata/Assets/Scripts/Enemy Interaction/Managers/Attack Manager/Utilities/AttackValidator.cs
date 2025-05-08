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

        // Check for valid phase
        try
        {
            bool isValidPhase = combatManager.IsEnemyCombatPhase();
            if (!isValidPhase)
            {
                Debug.LogWarning($"[AttackValidator] Not in enemy combat phase. Current phase: {combatManager.CurrentPhase}");
            }
            return isValidPhase;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AttackValidator] Error checking phase: {e.Message}");
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

        if (hasCombatManager && hasSpritePositioning)
            return ValidateCombatState(combatManager);

        return false;
    }
}
