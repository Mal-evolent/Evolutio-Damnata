using UnityEngine;


public class OngoingEffectManager : IOngoingEffect
{
    public SpellEffect EffectType { get; private set; }
    public int EffectValue { get; private set; }
    public int Duration { get; private set; }
    public EntityManager TargetEntity { get; private set; }
    private int _roundsApplied = 0;

    public OngoingEffectManager(SpellEffect effectType, int effectValue, int duration, EntityManager targetEntity)
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
        }

        _roundsApplied++;
    }

    public void ResetRounds()
    {
        _roundsApplied = 0;
    }

    public bool IsExpired()
    {
        // Effect expires after it's been applied Duration times
        return _roundsApplied >= Duration;
    }
}
