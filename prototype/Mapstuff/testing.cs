using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class testing : MonoBehaviour
{
    class _roomDim {
        public int x, y, width, height;
        public int centreX, centreY;
        public bool selected = false;
        public _roomDim(int x, int y, int width, int height) {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;

            this.centreX = this.x + (this.width / 2);
            this.centreY = this.y + (this.height / 2);
        }

        public enum _directions
        {
            none = -1, up, down, left, right
        }
        public int DistFromStart = -1;
        public List<_directions> connectedDir = new List<_directions>() { };
        public List<_roomDim> connectedRooms = new List<_roomDim>() { };

        public void connectRoom(_directions dir, _roomDim room)
        {
            connectedDir.Add(dir);
            connectedRooms.Add(room);
        }

        public static int D2ToD1(int x, int y, int width){
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

    _roomDim[] rooms;
    _roomDim furthestRoom; //thisis boss room
    _roomDim shopRoom;

    void drawBlock(int x, int y, int width, int height, bool selected) {
        Color[] colours = new Color[width*height];
        for (int i = 0; i < colours.Length; i++) {
            if (i % width < 2 || i % width > width - 2) //for x
            {
                colours[i] = Color.black;
            }
            else if (i / width < 2 || i / width > height - 2)//for y
            {
                colours[i] = Color.black;
            }
            else
            {
                colours[i] = selected ? Color.green : Color.grey;
            }
        }
        DrawOnTex.SetPixels(x, y, width, height, colours);
    }
    void drawBlockBoss(int x, int y, int width, int height, bool selected)
    {
        Color[] colours = new Color[width * height];
        for (int i = 0; i < colours.Length; i++)
        {
            if (i % width < 2 || i % width > width - 2) //for x
            {
                colours[i] = Color.black;
            }
            else if (i / width < 2 || i / width > height - 2)//for y
            {
                colours[i] = Color.black;
            }
            else
            {
                colours[i] = selected ? Color.green : Color.red;
            }
        }
        DrawOnTex.SetPixels(x, y, width, height, colours);
    }
    void drawBlockShop(int x, int y, int width, int height, bool selected)
    {
        Color[] colours = new Color[width * height];
        for (int i = 0; i < colours.Length; i++)
        {
            if (i % width < 2 || i % width > width - 2) //for x
            {
                colours[i] = Color.black;
            }
            else if (i / width < 2 || i / width > height - 2)//for y
            {
                colours[i] = Color.black;
            }
            else
            {
                colours[i] = selected ? Color.green : Color.yellow;
            }
        }
        DrawOnTex.SetPixels(x, y, width, height, colours);
    }
    void drawRoom(_roomDim room, bool selected)
    {
        if (room.DistFromStart == 0) {
            drawBlock(room.x, room.y, room.width, room.height, true);
        }
        else if (room.Equals(furthestRoom)) {
            drawBlockBoss(room.x, room.y, room.width, room.height, selected);
        }
        else if (room.Equals(shopRoom)) {
            drawBlockShop(room.x, room.y, room.width, room.height, selected);
        }
        else {
            drawBlock(room.x, room.y, room.width, room.height, selected);
        }
    }

    void drawLineBetweenRooms(_roomDim room1, _roomDim room2, Color colour)
    {
        Vector2 p1 = new Vector2(room1.centreX, room1.centreY);
        Vector2 p2 = new Vector2(room2.centreX, room2.centreY);
        Vector2 t = p1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
        {
            t = Vector2.Lerp(p1, p2, ctr);
            ctr += frac;
            DrawOnTex.SetPixel((int)t.x, (int)t.y, Color.grey);
            DrawOnTex.SetPixel((int)t.x+1, (int)t.y + 1, Color.black);
            DrawOnTex.SetPixel((int)t.x-1, (int)t.y-1, Color.black);
            //DrawOnTex.SetPixel((int)t.x, (int)t.y+1, colour);
            //DrawOnTex.SetPixel((int)t.x, (int)t.y-1, colour);
        }
    }

    //void drawCorridors()
    //{
    //    for (int c = 0; c < mapheight*mapwidth; c++){
    //        drawLineBetweenRooms(rooms[roomConnection[c].roomDim1], rooms[roomConnection[c].roomDim2]);
        
    //    }
    //}

    void clear(Color colour) {
        Color[] colours = new Color[DrawOnTex.width * DrawOnTex.height];
        for (int i = 0; i < colours.Length; i++)
        {
            colours[i] = colour;
        }
        DrawOnTex.SetPixels(0, 0, DrawOnTex.width, DrawOnTex.height, colours);
    }

    void generateConnections() {

        for (int y = 0; y < mapheight; y++) {
            for (int x = 0; x < mapwidth; x++) {

                //gather connected and valid directions
                List<_roomDim._directions> validOpp = new List<_roomDim._directions>();
                List<_roomDim._directions> validDir = new List<_roomDim._directions>();
                List<_roomDim> validRooms = new List<_roomDim>();

                List<_roomDim._directions> connectedOpp = new List<_roomDim._directions>();
                List<_roomDim._directions> connectedDir = new List<_roomDim._directions>();
                List<_roomDim> connectedRooms = new List<_roomDim>();
                if (y < mapheight-1) { //up
                    int roomPos = _roomDim.D2ToD1(x, y + 1, mapwidth);
                    validDir.Add(_roomDim._directions.up);
                    validOpp.Add(_roomDim._directions.down);
                    validRooms.Add(rooms[roomPos]);
                    if (rooms[roomPos].connectedDir.Count > 0) { 
                        connectedDir.Add(_roomDim._directions.up);
                        connectedOpp.Add(_roomDim._directions.down);
                        connectedRooms.Add(rooms[roomPos]);
                    }
                }
                if (y > 0){ //down
                    int roomPos = _roomDim.D2ToD1(x, y - 1, mapwidth);
                    validDir.Add(_roomDim._directions.down);
                    validOpp.Add(_roomDim._directions.up);
                    validRooms.Add(rooms[roomPos]);
                    if (rooms[roomPos].connectedDir.Count > 0) { 
                        connectedDir.Add(_roomDim._directions.down);
                        connectedOpp.Add(_roomDim._directions.up);
                        connectedRooms.Add(rooms[roomPos]);
                    }
                }
                if (x < mapwidth-1){ //right
                    int roomPos = _roomDim.D2ToD1(x+1, y, mapwidth);
                    validDir.Add(_roomDim._directions.right);
                    validOpp.Add(_roomDim._directions.left);
                    validRooms.Add(rooms[roomPos]);
                    if (rooms[roomPos].connectedDir.Count > 0) { 
                        connectedDir.Add(_roomDim._directions.right);
                        connectedOpp.Add(_roomDim._directions.left);
                        connectedRooms.Add(rooms[roomPos]);
                    }
                }
                if (x > 0){ //left
                    int roomPos = _roomDim.D2ToD1(x-1, y, mapwidth);
                    validDir.Add(_roomDim._directions.left);
                    validOpp.Add(_roomDim._directions.right);
                    validRooms.Add(rooms[roomPos]);
                    if (rooms[roomPos].connectedDir.Count > 0) { 
                        connectedDir.Add(_roomDim._directions.left);
                        connectedOpp.Add(_roomDim._directions.right);
                        connectedRooms.Add(rooms[roomPos]);
                    }
                }

                //randomly pick a direction to connect to based on if there sre connected rooms avaliable
                if (connectedDir.Count > 0) {
                    int roomPos = _roomDim.D2ToD1(x, y, mapwidth);
                    int chosenIndex = Random.Range(0, connectedDir.Count);
                    rooms[roomPos].connectRoom(connectedDir[chosenIndex], connectedRooms[chosenIndex]);
                    if (rooms[roomPos].DistFromStart == -1) {//add distance 
                        rooms[roomPos].DistFromStart = connectedRooms[chosenIndex].DistFromStart + 1;
                        if (rooms[roomPos].DistFromStart >= furthestRoom.DistFromStart) {
                            furthestRoom = rooms[roomPos];
                        }
                    }
                    connectedRooms[chosenIndex].connectRoom(connectedOpp[chosenIndex], rooms[roomPos]);
                }
                else {
                    int roomPos = _roomDim.D2ToD1(x, y, mapwidth);
                    int chosenIndex = Random.Range(0, validDir.Count);
                    rooms[roomPos].connectRoom(validDir[chosenIndex], validRooms[chosenIndex]); //connect room to chosen room
                    rooms[roomPos].DistFromStart = 0;
                    validRooms[chosenIndex].connectRoom(validOpp[chosenIndex], rooms[roomPos]); //connect chosen room to current room
                    validRooms[chosenIndex].DistFromStart = 1;
                    furthestRoom = validRooms[chosenIndex];
                }
            }
        }
        int r = Random.Range(0, rooms.Length);
        while (rooms[r].Equals(furthestRoom) || rooms[r].DistFromStart == 0) {
            r = Random.Range(0, rooms.Length);
        }
        shopRoom = rooms[r];
    }

    void generateRooms() {
        //generate room
        mapwidth = Random.Range(2, 5);
        mapheight = Random.Range(2, 5);
        rooms = new _roomDim[mapheight* mapwidth];
        int cellWidth = DrawOnTex.width/mapwidth, cellHeight = DrawOnTex.height/mapheight;
        
        for (int y = 0, c = 0; y < mapheight; y++) {
            for (int x = 0; x < mapwidth; x++, c++) {
                int posx, posy, width, height;

                //generate random x y
                posx = Random.Range(3, cellWidth/2);
                posy = Random.Range(3, cellHeight/2);

                //gnerate random width, height
                width = Random.Range( (cellWidth / 2), (cellWidth-3)-posx);
                height = Random.Range((cellHeight / 2), (cellHeight - 3) - posy);

                //offset x and y to the correct grid space
                posx += (cellWidth * x);
                posy += (cellHeight * y);

                //store room for later drawing
                rooms[c] = new _roomDim(posx, posy, width, height);
                //c++;
            }
        }

    }

    void drawRoomsAndCorridors() {
        clear(new Color(0,0,0,0));
        for (int c = 0; c < rooms.Length; c++) {
            for (int i = 0; i < rooms[c].connectedRooms.Count; i++) {
                drawLineBetweenRooms(rooms[c], rooms[c].connectedRooms[i], Color.red);
            }
        }
        for (int c = 0; c < rooms.Length; c++) {
            drawRoom(rooms[c], false);
        }

        DrawOnTex.Apply();
    }

    void mouseOnRoom(Vector2 mousepos) {
        for (int c = 0; c < rooms.Length; c++)
        {

            if (mousepos.x > rooms[c].x && mousepos.x < rooms[c].x + rooms[c].width)
            {
                if (mousepos.y > rooms[c].y && mousepos.y < rooms[c].y + rooms[c].height)
                {
                    rooms[c].selected = !rooms[c].selected;
                    if (rooms[c].selected)
                    {
                        drawRoom(rooms[c], true);
                    }
                    else
                    {
                        drawRoom(rooms[c], false);
                    }
                }
            }

        }
        DrawOnTex.Apply();
    }


    // Start is called before the first frame update
    void Start()
    {
        rectTransform = panel.GetComponent<RectTransform>();
        DrawOnTex = new Texture2D((int)rectTransform.rect.width, (int)rectTransform.rect.height, TextureFormat.ARGB4444, true);
        textTex = Sprite.Create(DrawOnTex, new Rect(0, 0, DrawOnTex.width, DrawOnTex.height), Vector2.zero);
        panel.GetComponent<Image>().sprite = textTex;

        generateRooms();
        generateConnections();

        drawRoomsAndCorridors();

        Debug.Log(DrawOnTex.width + ":w h:" + DrawOnTex.height);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A)) {
            generateRooms();
            generateConnections();
            drawRoomsAndCorridors();
        }


        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mousePos = Input.mousePosition;
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            Vector3 scaleFactor = rectTransform.localScale;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePos, null, out localPoint);
            localPoint += new Vector2((rectTransform.rect.width )/ 2, (rectTransform.rect.height )/ 2);
         

            mouseOnRoom(localPoint);

            Debug.Log(localPoint + ":location of mouse in rect --- get pixel:" + DrawOnTex.GetPixel((int)localPoint.x, (int)localPoint.y).ToString() );
        }


    }
}
