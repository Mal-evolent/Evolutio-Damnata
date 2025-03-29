public interface IOngoingEffect
{
    void ApplyEffect(EntityManager entity);
    void ResetRounds();
    bool IsExpired();
    EntityManager TargetEntity { get; }
}