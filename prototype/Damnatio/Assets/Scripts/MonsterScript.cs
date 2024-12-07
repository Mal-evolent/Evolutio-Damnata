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
    public GameObject outlineImg;

    [SerializeField]
    ResourceManager resourceManager;
    bool selected = false;

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

    [SerializeField]
    float health;
    float maxHealth = 10;
    float atkDamage; //<--- set on generateMonster
    float atkDamageMulti = 1.0f;

    public bool dead = false;
    public bool placed = false;

    // Monster needs room passed so they can get information on what's going on
    public void GenerateMonster(GameObject roomObj, int monsterID, _monsterType monsterType)
    {
        ID = monsterID;
        room = roomObj;
        this.monsterType = monsterType;

        health = maxHealth;
        //health = 0; // Comment in/out as necessary, e.g. if you need to test different rooms quickly.

        //picks a random monster image from global resources
        GlobalResources globalResources = GameObject.Find("ResourceManagaer").GetComponent<GlobalResources>();
        img.GetComponent<Image>().sprite = globalResources.monsters[Random.Range(0, globalResources.monsters.Count)]; // Assigns a random sprite
        //img.GetComponent<Image>().sortingOrder = 2;

        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -1);//puts infront of background

        //remove button components if its an enemy
        if (monsterType == MonsterScript._monsterType.Enemy) {
            Destroy(gameObject.GetComponent<Button>());

            outlineImg.GetComponent<Image>().color = new Color(0xFF, 0x00, 0x00);

        }
        else if (monsterType == _monsterType.Friendly) {
            outlineImg.GetComponent<Image>().color = new Color(0xB0, 0x00, 0xFF);
        }

        unloadMonster();
    }


    //overloaded function to allow for custom image
    public void GenerateMonster(GameObject roomObj, int monsterID, _monsterType monsterType, Sprite EnemyImg)
    {
        ID = monsterID;
        room = roomObj;
        this.monsterType = monsterType;

        img.GetComponent<Image>().sprite = EnemyImg;
        //img.GetComponent<Image>().sortingOrder = 2;

        health = maxHealth;
        //health = 0; // Comment in/out as necessary

        //TODO -- this needs to be updated so obejct get placed in at the correct coords for the level(minght need to hadn mpick leves images so that blaty boards are roughly the same)
        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -1);//puts inform of background

        if (monsterType == MonsterScript._monsterType.Enemy)
        {
            Destroy(gameObject.GetComponent<Button>());
            outlineImg.GetComponent<Image>().color = new Color(0xFF, 0x00, 0x00);
        }
        else if (monsterType == _monsterType.Friendly)
        {
            outlineImg.GetComponent<Image>().color = new Color(0xB0, 0x00, 0xFF);
        }

        unloadMonster();
    }

    //toggle switch
    public bool OutlineSelect()
    {
        selected = !selected;
        return selected;
    }
    public void ShowOutline() {
        selected = true;
        outlineImg.SetActive(true);
    }
    public void HideOutline()
    {
        selected = false;
        outlineImg.SetActive(false);
    }

    public void setHealth(float hlth)
    {
        health = hlth;
    }
    public void SetAttackDamage(float dmg)
    {
        atkDamage = dmg;
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
        Debug.Log($"Health is now {health}");
        if (health <= 0)
        {
            Debug.Log("Monster is dead.");
            // Handle monster death logic here
            //Destroy(gameObject);
            gameObject.SetActive(false);
            dead = true;
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
