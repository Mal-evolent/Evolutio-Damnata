using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//---------------interfaces for different attributes--------------------------------//


public class MonsterScript : MonoBehaviour, IDamageable, IIdentifiable, IAttacker
{
    [SerializeField]
    GameObject img;

    enum _monsterType
    {
        Friendly, // <-- player's side
        Enemy,
        Boss
    }

    GameObject room; //<--- set on generateMonster, gets passed in
    int ID; // ID is the position of the monster in the room's entities array

    float health; //<--- set on generateMonster
    float atkDamage; //<--- set on generateMonster
    float atkdamageMulti = 1.0f;

    // Monster needs room passed so they can get information on what's going on
    public void GenerateMonster(GameObject roomObj, int monsterID)
    {
        ID = monsterID;
        room = roomObj;
    }

    //returns monsters total attack
    public float getAttackDamage()
    {
        return atkDamage * atkdamageMulti;
    }



    //-------------------- IDamageable Implementation --------------------//
    public void takeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (health <= 0)
        {
            Debug.Log("Monster is dead.");
            // Handle monster death logic here
        }
    }

    public void heal(float healAmount)
    {
        health += healAmount;
    }

    public float getHealth()
    {
        return health;
    }

    //-------------------- IAttacker Implementation --------------------//
    public float attackDamage()
    {
        return atkDamage * atkdamageMulti; // Total attack damage calculation
    }

    public void attackBuff(float buffAmount)
    {
        atkDamage += buffAmount;
    }

    public void attackDebuff(float buffAmount)
    {
        atkDamage -= buffAmount;
    }

    public void attack(int targetID)
    {
        if (room != null && room.GetComponent<RoomScript>() != null)
        {
            room.GetComponent<RoomScript>().attackEvent(ID, targetID, attackDamage());
        }
        else
        {
            Debug.LogError("Room or RoomScript not found!");
        }
    }

    //-------------------- IIdentifiable Implementation --------------------//
    public int getID()
    {
        return ID;
    }

    // Other methods, Start, and Update logic can remain unchanged
    void Start()
    {
        // Initialization logic if needed
    }

    void Update()
    {
        // Game logic per frame
    }
}
