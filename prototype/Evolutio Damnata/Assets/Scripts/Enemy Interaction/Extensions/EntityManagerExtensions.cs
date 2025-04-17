using UnityEngine;

namespace EnemyInteraction.Extensions
{
    public static class EntityManagerExtensions
    {
        public static float GetAttackPower(this EntityManager entity)
        {
            if (entity == null) return 0;
            return entity.GetAttackDamage();
        }
    }
}