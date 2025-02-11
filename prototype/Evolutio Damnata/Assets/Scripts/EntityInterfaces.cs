public interface IDamageable
{
    void takeDamage(float damageAmount);
    void heal(float healAmount);
    float getHealth();
}

public interface IAttacker
{
    void attackBuff(float buffAmount);
    void attackDebuff(float buffAmount);
    void attack(int damage);
    float getAttackDamage();
}

public interface IIdentifiable
{
    int getID();
}
