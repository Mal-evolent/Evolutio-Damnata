using System.Linq;
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
    }
} 