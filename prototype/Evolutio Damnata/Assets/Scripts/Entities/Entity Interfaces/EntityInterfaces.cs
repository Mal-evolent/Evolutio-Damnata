using UnityEngine;

namespace EnemyInteraction.Interfaces
{
    public interface IDamageable
    {
        void TakeDamage(float damageAmount);
        void Heal(float healAmount);
        float GetHealth();
        void ModifyAttack(float modifier);
    }

    public interface IAttacker
    {
        void AttackBuff(float buffAmount);
        void AttackDebuff(float debuffAmount);
        void Attack(int damage);
        float GetAttackDamage();
        void SetDoubleAttack(int duration);
    }

    public interface ICombatEntity : IDamageable, IAttacker
    {
        string Name { get; }
        bool placed { get; }
        bool dead { get; }
        bool IsFadingOut { get; }
        EntityManager.MonsterType GetMonsterType();
        bool HasKeyword(Keywords.MonsterKeyword keyword);
        void SetKilledBy(EntityManager killer);
    }
}