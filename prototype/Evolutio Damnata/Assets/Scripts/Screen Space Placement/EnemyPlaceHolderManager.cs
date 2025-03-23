using System.Collections.Generic;
using UnityEngine;


/**
 * This class is used to position the sprites on the map.
 * It generates placeholders for the enemy entities.
 */

public class EnemyPlaceHolderManager
{
    private GameObject placeHolderPrefab;
    private Canvas mainCanvas;
    private List<GameObject> enemyEntities;
    private Dictionary<string, List<PositionData>> enemyRoomPositions;

    public EnemyPlaceHolderManager(GameObject placeHolderPrefab, Canvas mainCanvas, List<GameObject> enemyEntities, Dictionary<string, List<PositionData>> enemyRoomPositions)
    {
        this.placeHolderPrefab = placeHolderPrefab;
        this.mainCanvas = mainCanvas;
        this.enemyEntities = enemyEntities;
        this.enemyRoomPositions = enemyRoomPositions;
    }

    public void DisplayEnemyPlaceHolders(string currentRoom)
    {
        // Clear previously instantiated enemy placeholders
        foreach (GameObject placeHolder in enemyEntities)
        {
            Object.Destroy(placeHolder);
        }
        enemyEntities.Clear();

        // Instantiate new enemy placeholders and store them in the list
        if (enemyRoomPositions.ContainsKey(currentRoom))
        {
            List<PositionData> positions = enemyRoomPositions[currentRoom];
            for (int i = 0; i < positions.Count; i++)
            {
                PositionData position = positions[i];
                GameObject placeHolder = Object.Instantiate(placeHolderPrefab, mainCanvas.transform);
                placeHolder.GetComponent<RectTransform>().anchoredPosition = position.Position;
                placeHolder.GetComponent<RectTransform>().sizeDelta = position.Size;
                placeHolder.transform.localScale = position.Scale;
                placeHolder.transform.rotation = position.Rotation;
                placeHolder.name = $"Enemy_Placeholder_{i}";
                placeHolder.SetActive(false);
                enemyEntities.Add(placeHolder);
            }
        }
        else
        {
            Debug.LogError($"No enemy position data found for room: {currentRoom}");
        }
    }
}

