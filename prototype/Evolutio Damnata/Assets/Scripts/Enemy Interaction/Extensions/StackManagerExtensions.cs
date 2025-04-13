using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyInteraction.Extensions
{
    public static class StackManagerExtensions
    {
        public static bool HasEffect(this StackManager stackManager, EntityManager target, SpellEffect effect)
        {
            if (stackManager == null || target == null) return false;

            // Check if any TimedEffect in the stack matches our target and effect type
            return stackManager.StackView.Any(timedEffect =>
                timedEffect.effect.TargetEntity == target &&
                timedEffect.effect.EffectType == effect &&
                timedEffect.remainingTurns > 0);
        }

        /// <summary>
        /// Gets all ongoing effects of the specified type for the target entity
        /// </summary>
        /// <param name="stackManager">The stack manager instance</param>
        /// <param name="target">The entity to check for effects</param>
        /// <param name="effect">The type of effect to look for</param>
        /// <returns>A list of ongoing effects of the specified type</returns>
        public static List<IOngoingEffect> GetEffectsOfType(this StackManager stackManager, EntityManager target, SpellEffect effect)
        {
            if (stackManager == null || target == null)
                return new List<IOngoingEffect>();

            // Find all effects matching the target entity and effect type
            return stackManager.StackView
                .Where(timedEffect =>
                    timedEffect.effect != null &&
                    timedEffect.effect.TargetEntity == target &&
                    timedEffect.effect.EffectType == effect &&
                    timedEffect.remainingTurns > 0)
                .Select(timedEffect => timedEffect.effect)
                .ToList();
        }

        /// <summary>
        /// Gets the remaining duration for a specific effect
        /// </summary>
        /// <param name="stackManager">The stack manager instance</param>
        /// <param name="effect">The effect to check</param>
        /// <returns>The remaining number of turns for the effect, or 0 if not found</returns>
        public static int GetRemainingDuration(this StackManager stackManager, IOngoingEffect effect)
        {
            if (stackManager == null || effect == null)
                return 0;

            // Find the timed effect that contains this effect
            var timedEffect = stackManager.StackView
                .FirstOrDefault(te => te.effect == effect);

            // Return remaining turns or 0 if not found
            return timedEffect?.remainingTurns ?? 0;
        }
    }
}
