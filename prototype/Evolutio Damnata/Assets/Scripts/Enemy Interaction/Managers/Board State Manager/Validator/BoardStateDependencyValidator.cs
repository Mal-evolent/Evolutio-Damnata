using UnityEngine;
using EnemyInteraction.Interfaces;

namespace EnemyInteraction.Managers
{
    public class BoardStateDependencyValidator
    {
        /// <summary>
        /// Validates that all required dependencies are available
        /// </summary>
        public bool ValidateDependencies(
            bool isInitialized,
            ICombatManager combatManager,
            IEntityCacheManager entityCacheManager,
            SpritePositioning spritePositioning,
            BoardStateEvaluator boardStateEvaluator)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[BoardStateDependencyValidator] Not yet initialized");
                return false;
            }

            if (combatManager == null)
            {
                Debug.LogWarning("[BoardStateDependencyValidator] CombatManager reference missing");
                return false;
            }

            if (entityCacheManager == null)
            {
                Debug.LogWarning("[BoardStateDependencyValidator] EntityCacheManager reference missing");
                return false;
            }

            if (spritePositioning == null)
            {
                Debug.LogWarning("[BoardStateDependencyValidator] SpritePositioning reference missing");
                return false;
            }

            if (boardStateEvaluator == null)
            {
                Debug.LogWarning("[BoardStateDependencyValidator] BoardStateEvaluator not initialized");
                return false;
            }

            return true;
        }
    }
}
