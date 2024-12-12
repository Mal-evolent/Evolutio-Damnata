using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;
using System;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MapScript : MonoBehaviour
{
    [SerializeField]
    GameObject roomPrefab;
    HashSet<GameObject> clearedRooms = new HashSet<GameObject>();
    //indexed y then x eg rooms[Y, X]. game objects useing RoomScript
    public GameObject activeRoom;


    //-------------------for minimap drawing-----------------------

    class _room
    {
        public int x, y, width, height;
        public int centreX, centreY;
        public bool selected = false;

        public GameObject room;

        public _room(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;

            this.centreX = this.x + (this.width / 2);
            this.centreY = this.y + (this.height / 2);
        }
        public void setRoom(GameObject roomObj) {
            room = roomObj;
        }

        public enum _directions
        {
            none = -1, up, down, left, right
        }
        public int DistFromStart = -1;
        public List<_directions> connectedDir = new List<_directions>() { };
        public List<_room> connectedRooms = new List<_room>() { };

        public void connectRoom(_directions dir, _room room)
        {
            connectedDir.Add(dir);
            connectedRooms.Add(room);
        }

        public static int D2ToD1(int x, int y, int width)
        {
            return width * y + x;
        }
    }

    Texture2D DrawOnTex;
    Sprite textTex;

    [SerializeField]
    GameObject panel;
    RectTransform rectTransform;

    [SerializeField]
    int mapwidth;
    int mapheight;

    _room[] rooms;//<------ rooms hold the gameobject for the actual room
    _room furthestRoom; //this is boss room
    _room shopRoom;

    //-----------------drawing functions for minimap----------------------
    void drawBlock(int x, int y, int width, int height, bool selected, float alpha)
    {
        Color newGrey = Color.grey; newGrey.a = alpha;
        Color newBlack = Color.grey; newBlack.a = alpha;
        Color newGreen = Color.green; newGreen.a = alpha;
        Color[] colours = new Color[width * height];
        for (int i = 0; i < colours.Length; i++)
        {
            if (i % width < 2 || i % width > width - 2) //for x
            {
                colours[i] = newBlack;
            }
            else if (i / width < 2 || i / width > height - 2)//for y
            {
                colours[i] = newBlack;
            }
            else
            {
                colours[i] = selected ? newGreen : newGrey;
            }
        }
        DrawOnTex.SetPixels(x, y, width, height, colours);
    }
    void drawBlockBoss(int x, int y, int width, int height, bool selected, float alpha)
    {
        Color newRed = Color.red; newRed.a = alpha;
        Color newBlack = Color.grey; newBlack.a = alpha;
        Color newGreen = Color.green; newGreen.a = alpha;
        Color[] colours = new Color[width * height];
        for (int i = 0; i < colours.Length; i++)
        {
            if (i % width < 2 || i % width > width - 2) //for x
            {
                colours[i] = newBlack;
            }
            else if (i / width < 2 || i / width > height - 2)//for y
            {
                colours[i] = newBlack;
            }
            else
            {
                colours[i] = selected ? newGreen : newRed;
            }
        }
        DrawOnTex.SetPixels(x, y, width, height, colours);
    }
    void drawBlockShop(int x, int y, int width, int height, bool selected, float alpha)
    {
        Color newYellow = Color.yellow; newYellow.a = alpha;
        Color newBlack = Color.grey; newBlack.a = alpha;
        Color newGreen = Color.green; newGreen.a = alpha;
        Color[] colours = new Color[width * height];
        for (int i = 0; i < colours.Length; i++)
        {
            if (i % width < 2 || i % width > width - 2) //for x
            {
                colours[i] = newBlack;
            }
            else if (i / width < 2 || i / width > height - 2)//for y
            {
                colours[i] = newBlack;
            }
            else
            {
                colours[i] = selected ? newGreen : newYellow;
            }
        }
        DrawOnTex.SetPixels(x, y, width, height, colours);
    }
    void drawRoom(_room room, bool selected, float alpha)
    {
        if (room.DistFromStart == 0)
        {
            drawBlock(room.x, room.y, room.width, room.height, true, alpha);
        }
        else if (room.Equals(furthestRoom))
        {
            drawBlockBoss(room.x, room.y, room.width, room.height, selected, alpha);
        }
        else if (room.Equals(shopRoom))
        {
            drawBlockShop(room.x, room.y, room.width, room.height, selected, alpha);
        }
        else
        {
            drawBlock(room.x, room.y, room.width, room.height, selected, alpha);
        }
    }
    void drawLineBetweenRooms(_room room1, _room room2, float alpha)
    {
        Color newGrey = Color.grey; newGrey.a = alpha;
        Color newBlack = Color.grey; newBlack.a = alpha;

        Vector2 p1 = new Vector2(room1.centreX, room1.centreY);
        Vector2 p2 = new Vector2(room2.centreX, room2.centreY);
        Vector2 t = p1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
        {
            t = Vector2.Lerp(p1, p2, ctr);
            ctr += frac;
            DrawOnTex.SetPixel((int)t.x, (int)t.y, newGrey);
            DrawOnTex.SetPixel((int)t.x + 1, (int)t.y + 1, newBlack);
            DrawOnTex.SetPixel((int)t.x - 1, (int)t.y - 1, newBlack);
            //DrawOnTex.SetPixel((int)t.x, (int)t.y+1, colour);
            //DrawOnTex.SetPixel((int)t.x, (int)t.y-1, colour);
        }
    }
    void clear(Color colour)
    {
        Color[] colours = new Color[DrawOnTex.width * DrawOnTex.height];
        for (int i = 0; i < colours.Length; i++)
        {
            colours[i] = colour;
        }
        DrawOnTex.SetPixels(0, 0, DrawOnTex.width, DrawOnTex.height, colours);
    }
    void drawRoomsAndCorridors()
    {
        clear(new Color(0, 0, 0, 0));
        for (int c = 0; c < rooms.Length; c++)
        {
            for (int i = 0; i < rooms[c].connectedRooms.Count; i++)
            {
                drawLineBetweenRooms(rooms[c], rooms[c].connectedRooms[i], 0.5f);
            }
        }
        for (int c = 0; c < rooms.Length; c++)
        {
            drawRoom(rooms[c], false, 0.5f);
        }

        DrawOnTex.Apply();
    }

    //-----------------generation functions-----------------------
    void generateConnectionsAndRooms()
    {

        for (int y = 0; y < mapheight; y++)
        {
            for (int x = 0; x < mapwidth; x++)
            {

                //gather connected and valid directions
                List<_room._directions> validOpp = new List<_room._directions>();
                List<_room._directions> validDir = new List<_room._directions>();
                List<_room> validRooms = new List<_room>();

                List<_room._directions> connectedOpp = new List<_room._directions>();
                List<_room._directions> connectedDir = new List<_room._directions>();
                List<_room> connectedRooms = new List<_room>();
                if (y < mapheight - 1)
                { //up
                    int roomPos = _room.D2ToD1(x, y + 1, mapwidth);
                    validDir.Add(_room._directions.up);
                    validOpp.Add(_room._directions.down);
                    validRooms.Add(rooms[roomPos]);
                    if (rooms[roomPos].connectedDir.Count > 0)
                    {
                        connectedDir.Add(_room._directions.up);
                        connectedOpp.Add(_room._directions.down);
                        connectedRooms.Add(rooms[roomPos]);
                    }
                }
                if (y > 0)
                { //down
                    int roomPos = _room.D2ToD1(x, y - 1, mapwidth);
                    validDir.Add(_room._directions.down);
                    validOpp.Add(_room._directions.up);
                    validRooms.Add(rooms[roomPos]);
                    if (rooms[roomPos].connectedDir.Count > 0)
                    {
                        connectedDir.Add(_room._directions.down);
                        connectedOpp.Add(_room._directions.up);
                        connectedRooms.Add(rooms[roomPos]);
                    }
                }
                if (x < mapwidth - 1)
                { //right
                    int roomPos = _room.D2ToD1(x + 1, y, mapwidth);
                    validDir.Add(_room._directions.right);
                    validOpp.Add(_room._directions.left);
                    validRooms.Add(rooms[roomPos]);
                    if (rooms[roomPos].connectedDir.Count > 0)
                    {
                        connectedDir.Add(_room._directions.right);
                        connectedOpp.Add(_room._directions.left);
                        connectedRooms.Add(rooms[roomPos]);
                    }
                }
                if (x > 0)
                { //left
                    int roomPos = _room.D2ToD1(x - 1, y, mapwidth);
                    validDir.Add(_room._directions.left);
                    validOpp.Add(_room._directions.right);
                    validRooms.Add(rooms[roomPos]);
                    if (rooms[roomPos].connectedDir.Count > 0)
                    {
                        connectedDir.Add(_room._directions.left);
                        connectedOpp.Add(_room._directions.right);
                        connectedRooms.Add(rooms[roomPos]);
                    }
                }

                //randomly pick a direction to connect to based on if there sre connected rooms avaliable
                if (connectedDir.Count > 0)
                {
                    int roomPos = _room.D2ToD1(x, y, mapwidth);
                    int chosenIndex = Random.Range(0, connectedDir.Count);
                    rooms[roomPos].connectRoom(connectedDir[chosenIndex], connectedRooms[chosenIndex]);
                    if (rooms[roomPos].DistFromStart == -1)
                    {//add distance 
                        rooms[roomPos].DistFromStart = connectedRooms[chosenIndex].DistFromStart + 1;
                        if (rooms[roomPos].DistFromStart >= furthestRoom.DistFromStart)
                        {
                            furthestRoom = rooms[roomPos];
                        }
                    }
                    connectedRooms[chosenIndex].connectRoom(connectedOpp[chosenIndex], rooms[roomPos]);
                }
                else
                {
                    int roomPos = _room.D2ToD1(x, y, mapwidth);
                    int chosenIndex = Random.Range(0, validDir.Count);
                    rooms[roomPos].connectRoom(validDir[chosenIndex], validRooms[chosenIndex]); //connect room to chosen room
                    rooms[roomPos].DistFromStart = 0;
                    validRooms[chosenIndex].connectRoom(validOpp[chosenIndex], rooms[roomPos]); //connect chosen room to current room
                    validRooms[chosenIndex].DistFromStart = 1;
                    furthestRoom = validRooms[chosenIndex];
                }
            }
        }
        //set shop on random  room 
        int r = Random.Range(0, rooms.Length);
        while (rooms[r].Equals(furthestRoom) || rooms[r].DistFromStart == 0)
        {
            r = Random.Range(0, rooms.Length);
        }
        shopRoom = rooms[r];

        //generate rooms and store in room classes
        for (r = 0; r < rooms.Length; r++) {
            GameObject newRoom = Instantiate(roomPrefab);
            if (rooms[r].Equals(furthestRoom)) {
                newRoom.GetComponent<RoomScript>().generateRoom(RoomScript._roomsType.boss);
            }
            else if (rooms[r].Equals(shopRoom)) {
                newRoom.GetComponent<RoomScript>().generateRoom(RoomScript._roomsType.shop);
            }
            else {
                newRoom.GetComponent<RoomScript>().generateRoom(RoomScript._roomsType.standard);
            }
            rooms[r].setRoom(newRoom);
            if (r == 0) {
                activeRoom = rooms[r].room;
                activeRoom.GetComponent<RoomScript>().loadRoom();
            }
        }
    }
    void generateRooms()
    {
        //generate room
        mapwidth = Random.Range(2, 5);
        mapheight = Random.Range(2, 5);
        rooms = new _room[mapheight * mapwidth];
        int cellWidth = DrawOnTex.width / mapwidth, cellHeight = DrawOnTex.height / mapheight;

        for (int y = 0, c = 0; y < mapheight; y++)
        {
            for (int x = 0; x < mapwidth; x++, c++)
            {
                int posx, posy, width, height;

                //generate random x y
                posx = Random.Range(3, cellWidth / 2);
                posy = Random.Range(3, cellHeight / 2);

                //gnerate random width, height
                width = Random.Range((cellWidth / 2), (cellWidth - 3) - posx);
                height = Random.Range((cellHeight / 2), (cellHeight - 3) - posy);

                //offset x and y to the correct grid space
                posx += (cellWidth * x);
                posy += (cellHeight * y);

                //store room for later drawing
                rooms[c] = new _room(posx, posy, width, height);
                //c++;
            }
        }

    }

    //-----------------used for user map interaction------------------
    void mouseOnRoom(Vector2 mousepos)
    {
        // Add a check to see if the room that was clicked on is adjacent to any of the cleared rooms
        //Debug.Log("Running mouse on room");
        bool roomChanged = false;
        RoomScript rs = activeRoom.GetComponent<RoomScript>();
        List<GameObject> enemies = rs.returnEnemies();

        bool roomCleared = true;
        foreach (GameObject enemy in enemies) // If any enemy monster in the room still has health, then the room has not been cleared
        {
            MonsterScript ms = enemy.GetComponent<MonsterScript>();
            if (ms.getHealth() > 0)
            {
                roomCleared = false;
                Debug.Log("Room hasn't been cleared.");
                break;
            }
        }
        if (!roomCleared) // If the room has not been cleared, then return early
        {
            return;
        }
        //if cleared add room to cleared rooms and clear the rooms of player monsters
        clearedRooms.Add(activeRoom);
        List<GameObject> playerMonsters = rs.returnPlayerMonsters();
        foreach (GameObject PlMonsters in playerMonsters) //sets all monsters to dead so they wont show up on the next time you visit
        {
            PlMonsters.GetComponent<MonsterScript>().dead = true;
        }

        // Loop through all the rooms
        for (int c = 0; c < rooms.Length; c++)
        {
            // If the mouse clicks within the current (iterative) room's boundaries on the map...
            if (mousepos.x > rooms[c].x && mousepos.x < rooms[c].x + rooms[c].width)
            {
                if (mousepos.y > rooms[c].y && mousepos.y < rooms[c].y + rooms[c].height)
                {
                    // Loop through each of the rooms connected to the current (iterative) room
                    foreach (_room connectedRoom in rooms[c].connectedRooms)
                    {
                        // If a connected room is cleared
                        if (clearedRooms.Contains(connectedRoom.room))
                        {
                            // Switch the active room to the current (iterative) room
                            activeRoom.GetComponent<RoomScript>().unloadRoom();

                            activeRoom = rooms[c].room;
                            rooms[c].selected = !rooms[c].selected;
                            if (rooms[c].selected)
                            {
                                drawRoom(rooms[c], true, 0.5f);
                            }
                            activeRoom.GetComponent<RoomScript>().loadRoom(); //load room that the mouse has clicked on
                            roomChanged = true;

                            // Reset the deck, populate deck and hand, shuffle the deck, and refresh UI for the new room
                            RoomScript currentRoom = activeRoom.GetComponent<RoomScript>();
                            CardManager cardManager = currentRoom.returnCardManager();
                            Deck deck = cardManager.playerDeck;
                            deck.Reset();
                            deck.PopulateDeck();
                            deck.Shuffle();
                            cardManager.RefreshUI();

                            // Reset the turn
                            activeRoom.GetComponent<RoomScript>().turn = 0;
                            activeRoom.GetComponent<RoomScript>().turnText.text = $"Turn: {activeRoom.GetComponent<RoomScript>().turn}";
                        }
                    }
                }
            }
        }
        if (roomChanged)
        {
            DrawOnTex.Apply();
        }
    }

    //this will decide where rooms will be place and use the room generate function
    public void generateMap() {
        rectTransform = panel.GetComponent<RectTransform>();
        DrawOnTex = new Texture2D((int)rectTransform.rect.width, (int)rectTransform.rect.height, TextureFormat.ARGB4444, true);
        textTex = Sprite.Create(DrawOnTex, new Rect(0, 0, DrawOnTex.width, DrawOnTex.height), Vector2.zero);
        panel.GetComponent<Image>().sprite = textTex;

        generateRooms();
        generateConnectionsAndRooms();

        drawRoomsAndCorridors();

        Debug.Log(DrawOnTex.width + ":w h:" + DrawOnTex.height);
    }

    //unload and load activeRoom
    public void displayRoom(int x) {
        activeRoom.GetComponent<RoomScript>().unloadRoom();
        activeRoom = rooms[x].room;
        activeRoom.GetComponent<RoomScript>().loadRoom();
    }

    // Start is called before the first frame update
    void Start() {

        generateMap();
    
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mousePos = Input.mousePosition;
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            Vector3 scaleFactor = rectTransform.localScale;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePos, null, out localPoint);
            localPoint += new Vector2((rectTransform.rect.width) , (rectTransform.rect.height) );//offset orgin to bottomn left of minimap istead of top right


            mouseOnRoom(localPoint);

            //Debug.Log(localPoint + ":location of mouse in rect --- get pixel:" + DrawOnTex.GetPixel((int)localPoint.x, (int)localPoint.y).ToString());
        }
    }
}
