using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterScript : MonoBehaviour
{
    [SerializeField]
    GameObject img;

    enum _monsterType { 
        friendly, // <-- players side
        enemy,
        boss
    }

    GameObject room; //<--- set on generateMonster, gets passed in;
    int ID; //ID is the position the moster is in in the rooms enetits array

    GameObject enemyImg; //<--- this has been set in the editor
    _monsterType monsterType;//<--- set on generateMonster;

    float health;//<--- set on generateMonster;

    float atkDamage;//<--- set on generateMonster;
    float atkdamageMulti = 1;

    //monster needs room passed so they can get information on what going on
    public void generateMonster(GameObject roomObj, int monsterID) {
        ID = monsterID;
        room = roomObj;
    }

    //---------------functions to get the moster different attributes-------------------
    //returns monsters total Attack
    public float getAttackDamage() {
        return atkDamage * atkdamageMulti;
    }
    //returns mosters health
    public float getHealth() {
        return health;
    }
    //returns the mosters room ID
    public int getMonsterID() {
        return ID;
    }
    
    //--------------------functions that affect the current monster(mainly used by room)-------------
    //monster take damage by damageAmount
    public void damage(float damageAmount) {
        health -= damageAmount;
    }
    //heal the monster by healAmount
    public void heal(float healAmount) {
        health += healAmount;
    }
    //used to buff this monster
    public void attackBuff(float buff) {
        atkDamage += buff;
    }

    //-------------------functions to send events to room to damage/heal/buff other monsters------------
    //send attack event to room to attack an enemy
    public void attack(int attackedID) {
        room.GetComponent<RoomScript>().attackEvent(ID, attackedID, getAttackDamage());
    }
    




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
