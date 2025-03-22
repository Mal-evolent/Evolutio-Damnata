using UnityEngine;

/**
 * Handles the attack between two monsters.
 * 
 * @param playerEntity The attacking monster.
 * @param enemyEntity The defending monster.
 */

public class AttackHandler
{
    private AttackLimiter attackLimiter;

    public AttackHandler(AttackLimiter attackLimiter)
    {
        this.attackLimiter = attackLimiter;
    }

    public void HandleMonsterAttack(EntityManager playerEntity, EntityManager enemyEntity)
    {
        if (playerEntity == null || enemyEntity == null)
        {
            Debug.LogError("One of the entities is null!");
            return;
        }

        if (!attackLimiter.CanAttack(playerEntity))
        {
            Debug.LogWarning($"{playerEntity.name} cannot attack anymore this turn.");
            return;
        }

        // Both entities take damage according to their attack values
        float playerAttackDamage = playerEntity.getAttackDamage();
        float enemyAttackDamage = enemyEntity.getAttackDamage();

        playerEntity.takeDamage(enemyAttackDamage);
        enemyEntity.takeDamage(playerAttackDamage);

        attackLimiter.RegisterAttack(playerEntity);

        Debug.Log($"Player monster attacked enemy monster. Player monster took {enemyAttackDamage} damage. Enemy monster took {playerAttackDamage} damage.");
    }
}
