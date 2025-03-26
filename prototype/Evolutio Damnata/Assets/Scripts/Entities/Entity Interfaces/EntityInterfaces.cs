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