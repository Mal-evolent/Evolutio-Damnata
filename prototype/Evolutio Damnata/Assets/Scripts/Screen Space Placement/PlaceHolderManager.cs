using System.Collections.Generic;
using UnityEngine;

public class PlaceHolderManager : IPlaceholderManager
{
    private readonly GameObject _placeHolderPrefab;
    private readonly Canvas _mainCanvas;
    private readonly List<GameObject> _playerEntities;
    private readonly Dictionary<string, List<PositionData>> _roomPositions;

    public PlaceHolderManager(
        GameObject placeHolderPrefab,
        Canvas mainCanvas,
        List<GameObject> playerEntities,
        Dictionary<string, List<PositionData>> roomPositions)
    {
        _placeHolderPrefab = placeHolderPrefab;
        _mainCanvas = mainCanvas;
        _playerEntities = playerEntities;
        _roomPositions = roomPositions;
    }

    public void TogglePlaceHolders(bool show, string currentRoom)
    {
        ClearPlaceholders();

        if (!_roomPositions.TryGetValue(currentRoom, out List<PositionData> positions))
        {
            Debug.LogError($"No position data found for room: {currentRoom}");
            return;
        }

        CreatePlaceholders(positions, show, "Player");
    }

    public void DisplayPlaceHolders(string currentRoom)
    {
        TogglePlaceHolders(true, currentRoom);
    }

    private void ClearPlaceholders()
    {
        foreach (GameObject placeHolder in _playerEntities)
        {
            Object.Destroy(placeHolder);
        }
        _playerEntities.Clear();
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

            _playerEntities.Add(placeHolder);
        }
    }
}