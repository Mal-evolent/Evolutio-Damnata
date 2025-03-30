using UnityEngine;

public class OngoingEffectManager : IOngoingEffect
{
    public SpellEffect EffectType { get; private set; }
    public int EffectValue { get; private set; }
    public EntityManager TargetEntity { get; private set; }
    public int InitialDuration { get; private set; }

    public OngoingEffectManager(SpellEffect effectType, int effectValue, int duration, EntityManager targetEntity)
    {
        EffectType = effectType;
        EffectValue = effectValue;
        InitialDuration = duration;
        TargetEntity = targetEntity;
    }

    public void ApplyEffect(EntityManager entity)
    {
        if (entity != TargetEntity || entity.dead) return;

        switch (EffectType)
        {
            case SpellEffect.Burn:
                entity.TakeDamage(EffectValue);
                // Show damage number directly if TakeDamage() doesn't handle it
                entity.ShowDamageNumber(EffectValue);
                Debug.Log($"Applying burn effect: {EffectValue} damage to {entity.name}");
                break;
        }
    }

    public bool IsExpired() => false; // StackManager handles expiration
}