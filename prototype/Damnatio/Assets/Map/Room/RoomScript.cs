using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class RoomScript : MonoBehaviour
{
    enum _objectives{ 
        clear,
        boss
    }

    [SerializeField]
    GameObject backgroundImg; //<--- this has been set in the editor

    //this needs to be set in generate room, need to be even
    //also used to show where the monster is on the map reference design doc
    //index 0 == player them selves
    //index last == enemy if we have attacking wizards
    //the rest are monsters split evenyl
    //  \-so player side monster are 1 -> (size-2)/2
    //  |-enemys are ((size-2)/2)+1 -> size size - 2
    GameObject[] entities; 

    //this is a self setup function, use enemy generate
    //will need to somehow scale the backgroun obj with the screen size
    public void generateRoom() { }

    public void loadRoom() { }

    public void unloadRoom() { }

    //----------events used by monsters to affect other monsters-----------------
    public void attackEvent(int AttackingID, int AttackedID, float Damage) {
        float atkDamage = entities[AttackedID].GetComponent<MonsterScript>().getAttackDamage();
        entities[AttackedID].GetComponent<MonsterScript>().damage(atkDamage);
    }
    public void attackBuffEvent(int BuffingID, float buff) {
        entities[BuffingID].GetComponent<MonsterScript>().attackBuff(buff);
    }
    public void healEvent(int healingID, float health)
    {
        entities[healingID].GetComponent<MonsterScript>().heal(health);
    }

    //---------------------------fucntions used by mosnters to get information about other monsters
    //returns array with all of enemy monsters
    public GameObject[] returnEnemys() {
        int size = entities.Length;
        GameObject[] enemysObj = new GameObject[(size - 2) / 2];
        for (int i = ((size - 2) / 2) + 1; i < size - 2; i++) {
            enemysObj[i - ((size - 2) / 2) + 1] = entities[i];
        }
        return enemysObj;
    }
    //returns array with all of the player's spawned monsters
    public GameObject[] returnPlayerMonsters() {
        int size = entities.Length;
        GameObject[] playerMonsters = new GameObject[(size - 2) / 2];
        for (int i = 1; i < (size - 2) / 2; i++)
        {
            playerMonsters[i - 1] = entities[i];
        }
        return playerMonsters;
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
