using UnityEngine;

namespace EnemyInteraction.Extensions
{
    public static class CombatManagerExtensions
    {
        /// <summary>
        /// Checks if the current phase is the enemy preparation phase
        /// with additional robustness for scene transitions
        /// </summary>
        public static bool IsEnemyPrepPhase(this ICombatManager combatManager)
        {
            if (combatManager == null) return false;
            
            try
            {
                return combatManager.CurrentPhase == CombatPhase.EnemyPrep;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CombatManagerExtensions] Error checking phase: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the current phase is the enemy combat phase
        /// with additional robustness for scene transitions
        /// </summary>
        public static bool IsEnemyCombatPhase(this ICombatManager combatManager)
        {
            if (combatManager == null) return false;
            
            try
            {
                return combatManager.CurrentPhase == CombatPhase.EnemyCombat;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CombatManagerExtensions] Error checking phase: {e.Message}");
                return false;
            }
        }
    }
}
