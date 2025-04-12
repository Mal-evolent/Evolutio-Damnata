using UnityEngine;


public class AttackLimiter
{
    public bool CanAttack(EntityManager entity)
    {
        if (entity == null) return false;
        return entity.GetRemainingAttacks() > 0;
    }

    public void RegisterAttack(EntityManager entity)
    {
        if (entity == null) return;
        entity.UseAttack();
    }

    public void ResetAttacks(EntityManager entity)
    {
        if (entity == null) return;
        entity.ResetAttacks();
    }

    public void ModifyAllowedAttacks(EntityManager entity, int newAllowedAttacks)
    {
        if (entity == null) return;
        entity.SetAllowedAttacks(newAllowedAttacks);
    }
}
