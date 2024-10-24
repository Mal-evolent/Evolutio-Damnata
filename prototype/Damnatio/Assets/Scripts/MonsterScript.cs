using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//---------------interfaces for different attributes--------------------------------//


public class MonsterScript : MonoBehaviour, IDamageable, IIdentifiable, IAttacker
{
    [SerializeField]
    GameObject img;

    public enum _monsterType
    {
        player,
        Friendly, // <-- player's side
        Enemy,
        Boss
    }

    GameObject room; //<--- set on generateMonster, gets passed in
    int ID; // ID is the position of the monster in the room's entities array
    _monsterType monsterType; //<--- set on generateMonster, passed in from room(determins how attributes are assigned)

    float health; //<--- set on generateMonster
    float atkDamage; //<--- set on generateMonster
    float atkDamageMulti = 1.0f;

    // Monster needs room passed so they can get information on what's going on
    public void GenerateMonster(GameObject roomObj, int monsterID, _monsterType monsterType)
    {
        ID = monsterID;
        room = roomObj;
        this.monsterType = monsterType;
    }

    public GameObject getRoom()
    {
        return room;
    }

    public _monsterType getMonsterType()
    {
        return monsterType;
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

    //heals the monster by amount
    public void heal(float healAmount)
    {
        health += healAmount;
    }

    //returns monsters current health
    public float getHealth()
    {
        return health;
    }

    //-------------------- IAttacker Implementation --------------------//
    //returns monsters total attack
    public float getAttackDamage()
    {
        return atkDamage * atkDamageMulti; // Total attack damage calculation
    }

    //buffs monster attakc by amount (additive not replacment)
    public void attackBuff(float buffAmount)
    {
        atkDamage += buffAmount;
    }

    //same as attackBuff but remove instead of adds
    public void attackDebuff(float buffAmount)
    {
        atkDamage -= buffAmount;
    }

    //send attack event to room to apply damage
    public void attack(int targetID)
    {
        if (room != null && room.GetComponent<RoomScript>() != null)
        {
            room.GetComponent<RoomScript>().attackEvent(ID, targetID, getAttackDamage());
        }
        else
        {
            Debug.LogError("Room or RoomScript not found!");
        }
    }

    //-------------------- IIdentifiable Implementation --------------------//
    //returns ID for the current monster
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
