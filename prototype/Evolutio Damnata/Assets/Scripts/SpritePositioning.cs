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
    [SerializeField]
    public bool roomReady = false;

    public Dictionary<string, List<PositionData>> roomPositions;
    public Dictionary<string, List<PositionData>> enemyRoomPositions;
    public List<GameObject> playerEntities;
    public List<GameObject> enemyEntities;

    void Start()
    {
        InitializePlayerRoomPositions();
        InitializeEnemyRoomPositions();
        playerEntities = new List<GameObject>();
        enemyEntities = new List<GameObject>();
        StartCoroutine(WaitForRoomSelection());
        roomReady = false;
    }

    void InitializePlayerRoomPositions()
    {
        roomPositions = new Dictionary<string, List<PositionData>>();

        // Example configuration for room "Main Map"
        roomPositions["Main Map"] = new List<PositionData>
        {
            new PositionData(new Vector2(-553, -384), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-417.94f, -250.38f), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-306.8797f, -384), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0))
        };

        // Add more rooms and their positions as needed
    }

    void InitializeEnemyRoomPositions()
    {
        enemyRoomPositions = new Dictionary<string, List<PositionData>>();

        // Example configuration for room "Main Map"
        enemyRoomPositions["Main Map"] = new List<PositionData>
        {
            new PositionData(new Vector2(269, -384), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 180, 0)),
            new PositionData(new Vector2(417.940002f, -250.380005f), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 180, 0)),
            new PositionData(new Vector2(606, -384), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 180, 0))
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
        DisplayEnemyPlaceHolders(); // Display enemy placeholders permanently
        roomReady = true;
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
        foreach (GameObject placeHolder in playerEntities)
        {
            Destroy(placeHolder);
        }
        playerEntities.Clear();

        // Instantiate new placeholders and store them in the list
        List<PositionData> positions = GetPlayerPositionsForCurrentRoom();
        for (int i = 0; i < positions.Count; i++)
        {
            PositionData position = positions[i];
            GameObject placeHolder = Instantiate(placeHolderPrefab, mainCanvas.transform);
            placeHolder.GetComponent<RectTransform>().anchoredPosition = position.Position;
            placeHolder.GetComponent<RectTransform>().sizeDelta = position.Size;
            placeHolder.transform.localScale = position.Scale;
            placeHolder.transform.rotation = position.Rotation;
            placeHolder.name = $"Player_Placeholder_{i}";
            placeHolder.SetActive(show);
            playerEntities.Add(placeHolder);
        }
    }

    public void DisplayEnemyPlaceHolders()
    {
        // Clear previously instantiated enemy placeholders
        foreach (GameObject placeHolder in enemyEntities)
        {
            Destroy(placeHolder);
        }
        enemyEntities.Clear();

        // Instantiate new enemy placeholders and store them in the list
        List<PositionData> positions = GetEnemyPositionsForCurrentRoom();
        for (int i = 0; i < positions.Count; i++)
        {
            PositionData position = positions[i];
            GameObject placeHolder = Instantiate(placeHolderPrefab, mainCanvas.transform);
            placeHolder.GetComponent<RectTransform>().anchoredPosition = position.Position;
            placeHolder.GetComponent<RectTransform>().sizeDelta = position.Size;
            placeHolder.transform.localScale = position.Scale;
            placeHolder.transform.rotation = position.Rotation;
            placeHolder.name = $"Enemy_Placeholder_{i}";
            placeHolder.SetActive(true);
            enemyEntities.Add(placeHolder);
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
        while (playerEntities.Count == 0)
        {
            yield return null; // Wait for the next frame
        }

        foreach (GameObject placeHolder in playerEntities)
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
    public Quaternion Rotation;

    public PositionData(Vector2 position, Vector2 size, Vector3 scale, Quaternion rotation)
    {
        Position = position;
        Size = size;
        Scale = scale;
        Rotation = rotation;
    }
}
