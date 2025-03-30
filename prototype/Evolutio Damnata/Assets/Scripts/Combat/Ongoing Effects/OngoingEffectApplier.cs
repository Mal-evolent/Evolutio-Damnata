using UnityEngine;

public class OngoingEffectApplier : IEffectApplier
{
    public void ApplyEffects(EntityManager entity)
    {
        if (entity == null || entity.dead) return;

        // Process all stack effects for this entity
        StackManager.Instance?.ProcessStackForEntity(entity);
    }

    public void AddEffect(IOngoingEffect effect, int duration)
    {
        if (effect == null || effect.TargetEntity == null || effect.TargetEntity.dead) return;

        // Add effect directly to the stack system
        StackManager.Instance?.PushEffect(effect, duration);

        Debug.Log($"Added {effect.EffectType} effect to {effect.TargetEntity.name} " +
                 $"with value {effect.EffectValue} for {duration} turns");
    }

    public void RemoveEffectsForEntity(EntityManager entity)
    {
        if (entity == null || entity.dead) return;

        // Clean up all effects for this entity
        StackManager.Instance?.RemoveEffectsForEntity(entity);

        Debug.Log($"Removed all ongoing effects from {entity.name}");
    }
}