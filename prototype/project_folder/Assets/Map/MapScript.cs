using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapScript : MonoBehaviour
{
    //indexed y then x eg rooms[Y, X]. game objects useing RoomScript
    GameObject[,] rooms;
    GameObject activeRoom;

    //this will decide where rooms will be place and use the room generate function
    public void generateMap() { }

    //unload and load activeRoom
    public void displayRoom(int x, int y) { }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
