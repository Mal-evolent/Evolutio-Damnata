using System.Collections.Generic;
using UnityEngine;

public class EnemyPlaceHolderManager : IPlaceholderManager
{
    private readonly GameObject _placeHolderPrefab;
    private readonly Canvas _mainCanvas;
    private readonly List<GameObject> _enemyEntities;
    private readonly Dictionary<string, List<PositionData>> _enemyRoomPositions;

    public EnemyPlaceHolderManager(
        GameObject placeHolderPrefab,
        Canvas mainCanvas,
        List<GameObject> enemyEntities,
        Dictionary<string, List<PositionData>> enemyRoomPositions)
    {
        _placeHolderPrefab = placeHolderPrefab;
        _mainCanvas = mainCanvas;
        _enemyEntities = enemyEntities;
        _enemyRoomPositions = enemyRoomPositions;
    }

    public void DisplayPlaceHolders(string currentRoom)
    {
        ClearPlaceholders();

        if (!_enemyRoomPositions.TryGetValue(currentRoom, out List<PositionData> positions))
        {
            Debug.LogError($"No enemy position data found for room: {currentRoom}");
            return;
        }

        CreatePlaceholders(positions, false, "Enemy");
    }

    public void TogglePlaceHolders(bool active, string currentRoom)
    {
        DisplayPlaceHolders(currentRoom);
    }

    private void ClearPlaceholders()
    {
        foreach (GameObject placeHolder in _enemyEntities)
        {
            Object.Destroy(placeHolder);
        }
        _enemyEntities.Clear();
    }

    private void CreatePlaceholders(List<PositionData> positions, bool active, string prefix)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            PositionData position = positions[i];
            GameObject placeHolder = Object.Instantiate(_placeHolderPrefab, _mainCanvas.transform);

            RectTransform rectTransform = placeHolder.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position.Position;
            rectTransform.sizeDelta = position.Size;

            placeHolder.transform.localScale = position.Scale;
            placeHolder.transform.rotation = position.Rotation;
            placeHolder.name = $"{prefix}_Placeholder_{i}";
            placeHolder.SetActive(active);

            _enemyEntities.Add(placeHolder);
        }
    }
}