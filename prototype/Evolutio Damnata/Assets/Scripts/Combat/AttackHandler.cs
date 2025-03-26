using UnityEngine;

public class AttackHandler : IAttackHandler
{
    private readonly AttackLimiter _attackLimiter;

    public AttackHandler(AttackLimiter attackLimiter)
    {
        _attackLimiter = attackLimiter;
    }

    public void HandleAttack(EntityManager attacker, EntityManager target)
    {
        if (attacker == null || target == null)
        {
            Debug.LogError("One of the entities is null!");
            return;
        }

        if (!CanAttack(attacker))
        {
            Debug.LogWarning($"{attacker.name} cannot attack anymore this turn.");
            return;
        }

        float attackerDamage = attacker.GetAttackDamage();
        float targetDamage = target.GetAttackDamage();

        attacker.TakeDamage(targetDamage);
        target.TakeDamage(attackerDamage);

        _attackLimiter.RegisterAttack(attacker);

        Debug.Log($"{attacker.name} attacked {target.name}. " +
                 $"{attacker.name} took {targetDamage} damage. " +
                 $"{target.name} took {attackerDamage} damage.");
    }

    public bool CanAttack(EntityManager entity)
    {
        return _attackLimiter.CanAttack(entity);
    }
}