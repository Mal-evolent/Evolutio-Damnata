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
    public List<GameObject> instantiatedPlaceHolders;

    void Start()
    {
        InitializeRoomPositions();
        instantiatedPlaceHolders = new List<GameObject>();
        StartCoroutine(WaitForRoomSelection());
    }

    void InitializeRoomPositions()
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

    public IEnumerator WaitForRoomSelection()
    {
        while (mapScript.currentSelectedRoom == "None")
        {
            yield return null; // Wait for the next frame
        }
        togglePlaceHolders(true); // Set placeholders to be visible once the room is selected
    }

    public List<PositionData> GetPositionsForCurrentRoom()
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

    public void togglePlaceHolders(bool show)
    {
        // Clear previously instantiated placeholders
        foreach (GameObject placeHolder in instantiatedPlaceHolders)
        {
            Destroy(placeHolder);
        }
        instantiatedPlaceHolders.Clear();

        // Instantiate new placeholders and store them in the list
        List<PositionData> positions = GetPositionsForCurrentRoom();
        foreach (PositionData position in positions)
        {
            GameObject placeHolder = Instantiate(placeHolderPrefab, mainCanvas.transform);
            placeHolder.GetComponent<RectTransform>().anchoredPosition = position.Position;
            placeHolder.GetComponent<RectTransform>().sizeDelta = position.Size;
            placeHolder.transform.localScale = position.Scale;
            placeHolder.SetActive(show);
            instantiatedPlaceHolders.Add(placeHolder);
        }
    }

    public IEnumerator placeHolderActiveState(bool active)
    {
        // Wait until the list is populated
        while (instantiatedPlaceHolders.Count == 0)
        {
            yield return null; // Wait for the next frame
        }

        foreach (GameObject placeHolder in instantiatedPlaceHolders)
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
