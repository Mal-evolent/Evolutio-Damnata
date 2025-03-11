using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * This class is responsible for managing the ongoing effects of a spell on an entity.
 */

public class OngoingEffect
{
    public SpellEffect EffectType { get; private set; }
    public int EffectValue { get; private set; }
    public int Duration { get; private set; }
    public EntityManager TargetEntity { get; private set; }

    public OngoingEffect(SpellEffect effectType, int effectValue, int duration, EntityManager targetEntity) // Modify constructor
    {
        EffectType = effectType;
        EffectValue = effectValue;
        Duration = duration;
        TargetEntity = targetEntity;
    }

    public void ApplyEffect(EntityManager entity)
    {
        if (entity != TargetEntity) return;

        switch (EffectType)
        {
            case SpellEffect.Burn:
                entity.takeDamage(EffectValue);
                Debug.Log($"Applying burn effect: {EffectValue} damage to {entity.name}");
                break;
                // Add other effects as needed
        }
    }

    public void DecreaseDuration()
    {
        Duration--;
    }

    public bool IsExpired()
    {
        return Duration <= 0;
    }
}
