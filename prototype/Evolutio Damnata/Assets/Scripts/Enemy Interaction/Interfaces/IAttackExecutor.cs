using System.Collections;
using UnityEngine;

namespace EnemyInteraction.Interfaces
{
    public interface IAttackExecutor
    {
        IEnumerator ExecuteAttack(EntityManager attacker, EntityManager target);
        bool AttackPlayerHealthIcon(EntityManager attacker, HealthIconManager healthIcon);
        float GetRandomizedDelay(float baseDelay);
    }
}