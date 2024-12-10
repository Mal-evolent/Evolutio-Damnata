using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    public GameObject SelectedMonster;
    public GameObject EnemySelectedMonster;
    public GameObject map;

    [SerializeField]
    GameObject numberVis;

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
            currentRoom.attackEvent(playerMonsterId, enemyMonsterId, playerMonster.getAttackDamage());

            //create attack visualiser
            DamageVisualizer newVisualizer = new DamageVisualizer();
            newVisualizer.createDamageNumber(gameObject.GetComponent<MonoBehaviour>(), playerMonster.getAttackDamage(), enemyMonster.transform.position, numberVis);

            Debug.Log($"Player monster {playerMonsterId} did {playerMonster.getAttackDamage()} damage to enemy monster {enemyMonsterId}");

            SelectedMonster = null;
            EnemySelectedMonster = null;
            UpdateOutlines();

            // Loop through every enemy and attack random player monster
            List<GameObject> enemyMonsters = currentRoom.returnEnemies();
            List<GameObject> playerMonsters = currentRoom.returnPlayerMonsters();
            foreach(GameObject enemyMonsterObject in enemyMonsters)
            {
                MonsterScript em = enemyMonsterObject.GetComponent<MonsterScript>();
                if(playerMonsters.Count > 0 && !em.dead)
                {
                    int attepmts = 5;
                    int randomInt = Random.Range(0, playerMonsters.Count);
                    MonsterScript pm = playerMonsters[randomInt].GetComponent<MonsterScript>();
                    while (!pm.placed && attepmts > 0) {
                        randomInt = Random.Range(0, playerMonsters.Count);
                        pm = playerMonsters[randomInt].GetComponent<MonsterScript>();
                        attepmts--;
                    }
                    if (!pm.placed) { continue; }

                    currentRoom.attackEvent(em.getID(), pm.getID(), em.getAttackDamage());
                    newVisualizer.createDamageNumber(gameObject.GetComponent<MonoBehaviour>(), em.getAttackDamage(), pm.transform.position, numberVis);

                    Debug.Log($"Enemy monster {em.getID()} did {em.getAttackDamage()} damage to player monster {pm.getID()}");
                    if (pm.dead) {
                        currentRoom.regenerateDeadFriendly(pm.getID(), pm.gameObject.transform.position.x, pm.gameObject.transform.position.y, pm.gameObject.name);
                        //playerMonsters[randomInt].SetActive(true);
                    }
                }

                // Could consider writing an additional check here to check if the player has any monsters after the enemy has done their attack
            }

            // Draw one card
            CardManager cardManager = currentRoom.returnCardManager();
            cardManager.playerDeck.DrawOneCard();
            cardManager.RefreshUI();

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
