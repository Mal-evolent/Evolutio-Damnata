public interface IAttackHandler
{
    void HandleAttack(EntityManager attacker, EntityManager target);
    bool CanAttack(EntityManager entity);
}