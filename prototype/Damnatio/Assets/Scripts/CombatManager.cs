using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            RoomScript currentRoom = map.GetComponent<MapScript>().activeRoom.GetComponent<RoomScript>();
            MonsterScript playerMonster = SelectedMonster.GetComponent<MonsterScript>();
            MonsterScript enemyMonster = EnemySelectedMonster.GetComponent<MonsterScript>();

            int playerMonsterId = playerMonster.getID();
            int enemyMonsterId = enemyMonster.getID();
            currentRoom.attackEvent(playerMonsterId, enemyMonsterId, 1000f);

            SelectedMonster = null;
            EnemySelectedMonster = null;
            UpdateOutlines();

            // If the current room is the boss room and the enemy's health is 0 or less, go to the victory screen
            // For some reason, checking that the enemy is a boss does not work (enemyMonster.getMonsterType() == MonsterScript._monsterType.Boss)
            if (currentRoom.roomsType == RoomScript._roomsType.boss && enemyMonster.getHealth() <= 0)
            {
                SceneManager.LoadScene("victoryScene");
            }
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
