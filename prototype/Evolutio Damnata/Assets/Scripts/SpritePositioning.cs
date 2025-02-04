using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpritePositioning : MonoBehaviour
{
    [SerializeField]
    GameObject placeHolderPrefab;
    [SerializeField]
    MapScript mapScript;
    [SerializeField]
    Canvas mainCanvas;

    public Dictionary<string, List<PositionData>> roomPositions;
    public Dictionary<string, List<PositionData>> enemyRoomPositions;
    public List<GameObject> activeEntities;

    void Start()
    {
        InitializePlayerRoomPositions();
        InitializeEnemyRoomPositions();
        activeEntities = new List<GameObject>();
        StartCoroutine(WaitForRoomSelection());
    }

    void InitializePlayerRoomPositions()
    {
        roomPositions = new Dictionary<string, List<PositionData>>();

        // Example configuration for room "Main Map"
        roomPositions["Main Map"] = new List<PositionData>
        {
            new PositionData(new Vector2(-553, -384), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f)),
            new PositionData(new Vector2(-417.94f, -250.38f), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f)),
            new PositionData(new Vector2(-306.8797f, -384), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f))
        };

        // Add more rooms and their positions as needed
    }

    void InitializeEnemyRoomPositions()
    {
        enemyRoomPositions = new Dictionary<string, List<PositionData>>();

        // Example configuration for room "Main Map"
        enemyRoomPositions["Main Map"] = new List<PositionData>
        {
            new PositionData(new Vector2(100, 100), new Vector2(50, 50), new Vector3(0.5826045f, 0.5826045f, 0.5826045f)),
            new PositionData(new Vector2(200, 200), new Vector2(50, 50), new Vector3(0.5826045f, 0.5826045f, 0.5826045f)),
            new PositionData(new Vector2(300, 300), new Vector2(50, 50), new Vector3(0.5826045f, 0.5826045f, 0.5826045f))
        };

        // Add more rooms and their positions as needed
    }

    public IEnumerator WaitForRoomSelection()
    {
        while (mapScript.currentSelectedRoom == "None")
        {
            yield return null; // Wait for the next frame
        }
        togglePlaceHolders(true); // Set placeholders to be visible once the room is selected
    }

    public List<PositionData> GetPlayerPositionsForCurrentRoom()
    {
        string currentRoom = mapScript.currentSelectedRoom;
        if (roomPositions.ContainsKey(currentRoom))
        {
            return roomPositions[currentRoom];
        }
        else
        {
            Debug.LogError($"No position data found for room: {currentRoom}");
            return new List<PositionData>();
        }
    }

    public List<PositionData> GetEnemyPositionsForCurrentRoom()
    {
        string currentRoom = mapScript.currentSelectedRoom;
        if (enemyRoomPositions.ContainsKey(currentRoom))
        {
            return enemyRoomPositions[currentRoom];
        }
        else
        {
            Debug.LogError($"No enemy position data found for room: {currentRoom}");
            return new List<PositionData>();
        }
    }

    public void togglePlaceHolders(bool show)
    {
        // Clear previously instantiated placeholders
        foreach (GameObject placeHolder in activeEntities)
        {
            Destroy(placeHolder);
        }
        activeEntities.Clear();

        // Instantiate new placeholders and store them in the list
        List<PositionData> positions = GetPlayerPositionsForCurrentRoom();
        foreach (PositionData position in positions)
        {
            GameObject placeHolder = Instantiate(placeHolderPrefab, mainCanvas.transform);
            placeHolder.GetComponent<RectTransform>().anchoredPosition = position.Position;
            placeHolder.GetComponent<RectTransform>().sizeDelta = position.Size;
            placeHolder.transform.localScale = position.Scale;
            placeHolder.SetActive(show);
            activeEntities.Add(placeHolder);
        }
    }

    public Vector2 GetFirstPlaceholderSize()
    {
        List<PositionData> positions = GetPlayerPositionsForCurrentRoom();
        if (positions.Count > 0)
        {
            return positions[0].Size;
        }
        return Vector2.zero;
    }

    public Vector3 GetFirstPlaceholderScale()
    {
        List<PositionData> positions = GetPlayerPositionsForCurrentRoom();
        if (positions.Count > 0)
        {
            return positions[0].Scale;
        }
        return Vector3.one;
    }

    public IEnumerator placeHolderActiveState(bool active)
    {
        // Wait until the list is populated
        while (activeEntities.Count == 0)
        {
            yield return null; // Wait for the next frame
        }

        foreach (GameObject placeHolder in activeEntities)
        {
            placeHolder.SetActive(active);
        }
    }

    public IEnumerator SetAllPlaceHoldersInactive()
    {
        yield return StartCoroutine(placeHolderActiveState(false));
        Debug.Log("All placeholders set to inactive!");
    }

    public IEnumerator SetAllPlaceHoldersActive()
    {
        yield return StartCoroutine(placeHolderActiveState(true));
        Debug.Log("All placeholders set to active!");
    }
}

[System.Serializable]
public class PositionData
{
    public Vector2 Position;
    public Vector2 Size;
    public Vector3 Scale;

    public PositionData(Vector2 position, Vector2 size, Vector3 scale)
    {
        Position = position;
        Size = size;
        Scale = scale;
    }
}
