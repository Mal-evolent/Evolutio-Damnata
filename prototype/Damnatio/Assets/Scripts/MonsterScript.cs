using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using UnityEngine.UI;

//---------------interfaces for different attributes--------------------------------//


public class MonsterScript : MonoBehaviour, IDamageable, IIdentifiable, IAttacker
{
    [SerializeField]
    public GameObject img;

    [SerializeField]
    ResourceManager resourceManager;

    public enum _monsterType
    {
        player,
        Friendly,
        Enemy,
        Boss
    }

    GameObject room; //<--- set on generateMonster, gets passed in
    int ID; // ID is the position of the monster in the room's entities array
    _monsterType monsterType; //<--- set on generateMonster, passed in from room(determins how attributes are assigned)

    float health; //<--- set on generateMonster
    float maxHealth = 10;
    float atkDamage; //<--- set on generateMonster
    float atkDamageMulti = 1.0f;


    // Monster needs room passed so they can get information on what's going on
    public void GenerateMonster(GameObject roomObj, int monsterID, _monsterType monsterType)
    {
        ID = monsterID;
        room = roomObj;
        this.monsterType = monsterType;
        health = maxHealth;
        //health = 0; // Remove comment as necessary, e.g. if you need to test different rooms.

        //picks a random monster image from global resources
        GlobalResources globalResources = GameObject.Find("ResourceManagaer").GetComponent<GlobalResources>();
        img.GetComponent<Image>().sprite = globalResources.monsters[Random.Range(0, globalResources.monsters.Count)];
        //img.GetComponent<Image>().sortingOrder = 2;

        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -1);//puts infront of background

        unloadMonster();
    }


    //overloaded function to allow for custom image
    public void GenerateMonster(GameObject roomObj, int monsterID, _monsterType monsterType, Sprite EnemyImg)
    {
        ID = monsterID;
        room = roomObj;
        this.monsterType = monsterType;

        //picks a random monster image from global resources
        img.GetComponent<Image>().sprite = EnemyImg;
        //img.GetComponent<Image>().sortingOrder = 2;

        //TODO -- this needs to be updated so obejct get placed in at the correct coords for the level(minght need to hadn mpick leves images so that blaty boards are roughly the same)
        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -1);//puts inform of background

        unloadMonster();
    }




    public void loadMonster()
    {
        gameObject.SetActive(true);
    }

    public void unloadMonster()
    {
        gameObject.SetActive(false);
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
