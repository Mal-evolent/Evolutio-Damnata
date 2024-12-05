using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public GameObject SelectedMonster;
    public GameObject EnemySelectedMonster;
    public GameObject map;

    public void UpdateOutlines() {
        //toggel the selected monster . if it a reselect then set sleceted monster to null so nothing gets selected
        if (SelectedMonster != null)
        {
            bool selected = SelectedMonster.GetComponent<MonsterScript>().OutlineSelect();
            if (!selected) { SelectedMonster = null; }
        }

        GameObject currentRoom = map.GetComponent<MapScript>().activeRoom;
        List<GameObject> playerMonsters = currentRoom.GetComponent<RoomScript>().returnPlayerMonsters();
        List<GameObject> enemyMonsters = currentRoom.GetComponent<RoomScript>().returnEnemies();
        foreach (GameObject mon in playerMonsters)
        {
            if (SelectedMonster == mon) {
                mon.GetComponent<MonsterScript>().ShowOutline();
            }
            else { 
                mon.GetComponent<MonsterScript>().HideOutline();
            }
        }
            
        foreach (GameObject mon in enemyMonsters)
        {
            if (SelectedMonster != null)
            {
                mon.GetComponent<MonsterScript>().ShowOutline();
            }
            else
            {
                mon.GetComponent<MonsterScript>().HideOutline();
            }
        }
    }
    public void TriggerAttack() {
        if (SelectedMonster != null) {
            GameObject currentRoom = map.GetComponent<MapScript>().activeRoom;
            int playerMonsterId = SelectedMonster.GetComponent<MonsterScript>().getID();
            int enemyMonsterId = EnemySelectedMonster.GetComponent<MonsterScript>().getID();
            currentRoom.GetComponent<RoomScript>().attackEvent(playerMonsterId, enemyMonsterId, 1000f);

            SelectedMonster = null;
            EnemySelectedMonster = null;
            UpdateOutlines();
        }

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
