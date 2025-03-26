public interface IEffectApplier
{
    void ApplyEffects(EntityManager entity);
    void AddEffect(IOngoingEffect effect);
}