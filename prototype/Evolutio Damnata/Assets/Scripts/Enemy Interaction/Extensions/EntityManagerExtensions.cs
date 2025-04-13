using UnityEngine;

namespace EnemyInteraction.Extensions
{
    public static class EntityManagerExtensions
    {
        public static float GetAttackPower(this EntityManager entity)
        {
            if (entity == null) return 0;
            return entity.GetAttackDamage();  // Using the public method from EntityManager
        }

        public static float GetHealth(this EntityManager entity)
        {
            if (entity == null) return 0;
            return entity.GetHealth();  // Using the public method from EntityManager
        }

        public static float GetMaxHealth(this EntityManager entity)
        {
            if (entity == null) return 0;
            return entity.GetMaxHealth();  // Using the public method from EntityManager
        }

        public static bool HasKeyword(this EntityManager entity, Keywords.MonsterKeyword keyword)
        {
            if (entity == null) return false;
            return entity.HasKeyword(keyword);  // Using the public method from EntityManager
        }
    }
} 