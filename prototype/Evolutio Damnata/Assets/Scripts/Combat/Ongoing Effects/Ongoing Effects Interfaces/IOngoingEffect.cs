public interface IOngoingEffect
{
    SpellEffect EffectType { get; }
    int EffectValue { get; }
    EntityManager TargetEntity { get; }
    int InitialDuration { get; }

    void ApplyEffect(EntityManager entity);
    bool IsExpired();
}