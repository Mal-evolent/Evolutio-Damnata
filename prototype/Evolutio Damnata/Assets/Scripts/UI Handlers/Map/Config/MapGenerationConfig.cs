using UnityEngine;

[System.Serializable]
public class MapGenerationConfig
{
    public int minRooms = 7;
    public int maxRooms = 15;
    public float cellSize = 60f;
    public int gridSize = 10;
    public int gridRows = 10;
}
