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
    GameObject RoomID;

    GameObject enemyImg; //<--- this has been set in the editor
    _monsterType monsterType;//<--- set on generateMonster;

    float damage;//<--- set on generateMonster;
    float damageMulti = 1;

    //monster needs room passed so they can get information on what going on
    public void generateMonster(GameObject room) { }

    //    \/ tabed to be able to attach script to objs 
    //public int getMonsterID() { }


    //used the rooms attackEvent
    public void attack(int attackedID, float damage) { }
    
    //used to buff this monster
    public void buff(float buff) { }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
