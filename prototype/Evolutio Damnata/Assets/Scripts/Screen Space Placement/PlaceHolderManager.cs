using System.Collections.Generic;
using UnityEngine;

/**
 * This class is used to position the sprites on the map.
 * It generates placeholders for the player and enemy entities.
 */

public class PlaceHolderManager
{
    private GameObject placeHolderPrefab;
    private Canvas mainCanvas;
    private List<GameObject> playerEntities;
    private Dictionary<string, List<PositionData>> roomPositions;

    public PlaceHolderManager(GameObject placeHolderPrefab, Canvas mainCanvas, List<GameObject> playerEntities, Dictionary<string, List<PositionData>> roomPositions)
    {
        this.placeHolderPrefab = placeHolderPrefab;
        this.mainCanvas = mainCanvas;
        this.playerEntities = playerEntities;
        this.roomPositions = roomPositions;
    }

    public void TogglePlaceHolders(bool show, string currentRoom)
    {
        // Clear previously instantiated placeholders
        foreach (GameObject placeHolder in playerEntities)
        {
            Object.Destroy(placeHolder);
        }
        playerEntities.Clear();

        // Instantiate new placeholders and store them in the list
        if (roomPositions.ContainsKey(currentRoom))
        {
            List<PositionData> positions = roomPositions[currentRoom];
            for (int i = 0; i < positions.Count; i++)
            {
                PositionData position = positions[i];
                GameObject placeHolder = Object.Instantiate(placeHolderPrefab, mainCanvas.transform);
                placeHolder.GetComponent<RectTransform>().anchoredPosition = position.Position;
                placeHolder.GetComponent<RectTransform>().sizeDelta = position.Size;
                placeHolder.transform.localScale = position.Scale;
                placeHolder.transform.rotation = position.Rotation;
                placeHolder.name = $"Player_Placeholder_{i}";
                placeHolder.SetActive(show);
                playerEntities.Add(placeHolder);
            }
        }
        else
        {
            Debug.LogError($"No position data found for room: {currentRoom}");
        }
    }
}

