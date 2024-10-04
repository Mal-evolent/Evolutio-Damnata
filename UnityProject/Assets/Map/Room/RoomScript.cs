using System.Collections;
using System.Collections.Generic;
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

    public void attackEvent(int AttackingID, int AttackedID, float Damage) { }
    public void buffEvent(int BuffingID, float buff) { }

    //will be used by monsters do direct attacks
    //    \/ tabed to be able to attach script to objs 
    //public GameObject[] returnEnemys() { }
    //public GameObject[] returnPlayerMonsters() { }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
