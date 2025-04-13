using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyInteraction.Utilities
{
    /// <summary>
    /// Provides common utility functions for AI targeting and decision making
    /// </summary>
    public static class AIUtilities
    {
        /// <summary>
        /// Determines if a health icon can be targeted based on field entity presence
        /// </summary>
        /// <param name="fieldEntities">List of entities on the field</param>
        /// <returns>True if health icon can be targeted (no entities on field), false otherwise</returns>
        public static bool CanTargetHealthIcon(List<EntityManager> fieldEntities)
        {
            return fieldEntities == null || 
                   fieldEntities.Count == 0 || 
                   !fieldEntities.Any(e => e != null && e.placed && !e.dead && !e.IsFadingOut);
        }
        
        /// <summary>
        /// Validates that a target is appropriate for the given effect type based on allegiance
        /// </summary>
        /// <param name="target">The target to validate</param>
        /// <param name="effectType">The effect to be applied</param>
        /// <param name="isDamagingEffect">Whether the effect is damaging</param>
        /// <returns>True if the target is valid for the effect, false otherwise</returns>
        public static bool IsValidTargetForEffect(EntityManager target, SpellEffect effectType, bool isDamagingEffect)
        {
            if (target == null)
                return false;
                
            // Handle health icons specifically
            if (target is HealthIconManager healthIcon)
            {
                // For damaging effects, only target player health icon
                if (isDamagingEffect)
                    return healthIcon.IsPlayerIcon;
                // For healing effects, only target enemy health icon
                else
                    return !healthIcon.IsPlayerIcon;
            }
            
            // For monsters, check their type
            if (isDamagingEffect)
            {
                // Only target player (friendly) monsters with damaging effects
                return target.GetMonsterType() == EntityManager.MonsterType.Friendly;
            }
            else
            {
                // Only target enemy monsters with healing/buff effects
                return target.GetMonsterType() == EntityManager.MonsterType.Enemy;
            }
        }
    }
}
