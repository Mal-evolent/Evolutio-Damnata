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
        // Skip effect application if:
        // 1. Entity is null
        // 2. Entity is dead
        // 3. Entity is fading out (in the process of dying)
        // 4. The entity passed doesn't match our target entity
        if (entity == null ||
            entity.dead ||
            entity.IsFadingOut ||
            entity != TargetEntity)
        {
            return;
        }

        switch (EffectType)
        {
            case SpellEffect.Burn:
                float healthBeforeDamage = entity.GetHealth();

                // Calculate the damage to display (considering Tough keyword)
                int displayDamage = EffectValue;
                if (entity.HasKeyword(Keywords.MonsterKeyword.Tough))
                {
                    displayDamage = Mathf.FloorToInt(EffectValue / 2f);
                    Debug.Log($"[OngoingEffectManager] {entity.name} is tough and reduces burn damage by half! Taking {displayDamage} damage.");
                }

                // Apply the damage (TakeDamage will handle the actual damage reduction for Tough)
                entity.TakeDamage(EffectValue);

                // Don't call ShowDamageNumber - EntityManager.TakeDamage will handle this
                // and already applies the Tough reduction

                // Record the effect application in history with the correct value
                if (CardHistory.Instance != null)
                {
                    CardHistory.Instance.RecordEffectApplication(EffectType, entity, displayDamage);
                }

                Debug.Log($"Applying burn effect: {displayDamage} damage to {entity.name}");

                // Check if the effect killed the entity
                if (healthBeforeDamage > 0 && entity.GetHealth() <= 0)
                {
                    if (GraveYard.Instance != null)
                    {
                        GraveYard.Instance.AddSpellKill(entity, "Burn Effect", displayDamage, true);
                    }
                }
                break;
        }
    }

    public bool IsExpired() => false; // StackManager handles expiration
}