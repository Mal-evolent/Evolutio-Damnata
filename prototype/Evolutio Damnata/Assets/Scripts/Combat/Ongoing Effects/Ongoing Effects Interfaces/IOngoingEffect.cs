public interface IOngoingEffect
{
    void ApplyEffect(EntityManager entity);
    void DecreaseDuration();
    bool IsExpired();
    EntityManager TargetEntity { get; }
}