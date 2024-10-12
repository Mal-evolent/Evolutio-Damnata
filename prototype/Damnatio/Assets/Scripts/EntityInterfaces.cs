using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void takeDamage(float damageAmount);
    void heal(float healAmount);
    float getHealth();
}

public interface IAttacker
{
    float attackDamage();
    void attackBuff(float buffAmount);
    void attackDebuff(float buffAmount);
    void attack(int targetID);
    float getAttackDamage();
}

public interface IIdentifiable
{
    int getID();
}
