using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpritePositioning : MonoBehaviour, ISpritePositioning
{
    [SerializeField] private GameObject _placeHolderPrefab;
    [SerializeField] private MapScript _mapScript;
    [SerializeField] private Canvas _mainCanvas;

    public bool RoomReady { get; private set; }
    public List<GameObject> PlayerEntities { get; private set; }
    public List<GameObject> EnemyEntities { get; private set; }

    private Dictionary<string, List<PositionData>> _roomPositions;
    private Dictionary<string, List<PositionData>> _enemyRoomPositions;
    private IPlaceholderManager _playerPlaceholderManager;
    private IPlaceholderManager _enemyPlaceholderManager;

    private void Start()
    {
        InitializePositionData();
        InitializePlaceholderManagers();
        StartCoroutine(WaitForRoomSelection());
        RoomReady = false;
    }

    private void InitializePositionData()
    {
        _roomPositions = new PlayerRoomPositionsInitializer().InitializePlayerRoomPositions();
        _enemyRoomPositions = new EnemyRoomPositionsInitializer().InitializeEnemyRoomPositions();
        PlayerEntities = new List<GameObject>();
        EnemyEntities = new List<GameObject>();
    }

    private void InitializePlaceholderManagers()
    {
        _playerPlaceholderManager = new PlaceHolderManager(
            _placeHolderPrefab,
            _mainCanvas,
            PlayerEntities,
            _roomPositions);

        _enemyPlaceholderManager = new EnemyPlaceHolderManager(
            _placeHolderPrefab,
            _mainCanvas,
            EnemyEntities,
            _enemyRoomPositions);
    }

    public IEnumerator WaitForRoomSelection()
    {
        while (_mapScript.currentSelectedRoom == "None")
        {
            yield return null;
        }

        _playerPlaceholderManager.TogglePlaceHolders(true, _mapScript.currentSelectedRoom);
        _enemyPlaceholderManager.DisplayPlaceHolders(_mapScript.currentSelectedRoom);
        RoomReady = true;
    }

    public List<PositionData> GetPlayerPositionsForCurrentRoom()
    {
        string currentRoom = _mapScript.currentSelectedRoom;
        return GetPositionsForRoom(currentRoom, _roomPositions);
    }

    public List<PositionData> GetEnemyPositionsForCurrentRoom()
    {
        string currentRoom = _mapScript.currentSelectedRoom;
        return GetPositionsForRoom(currentRoom, _enemyRoomPositions);
    }

    private List<PositionData> GetPositionsForRoom(string roomName, Dictionary<string, List<PositionData>> positionsDict)
    {
        if (positionsDict.ContainsKey(roomName))
        {
            return positionsDict[roomName];
        }

        Debug.LogError($"No position data found for room: {roomName}");
        return new List<PositionData>();
    }

    public Vector2 GetFirstPlaceholderSize()
    {
        return GetFirstPositionProperty(pos => pos.Size, Vector2.zero);
    }

    public Vector3 GetFirstPlaceholderScale()
    {
        return GetFirstPositionProperty(pos => pos.Scale, Vector3.one);
    }

    private T GetFirstPositionProperty<T>(System.Func<PositionData, T> propertySelector, T defaultValue)
    {
        var positions = GetPlayerPositionsForCurrentRoom();
        return positions.Count > 0 ? propertySelector(positions[0]) : defaultValue;
    }

    public IEnumerator SetPlaceholderActiveState(List<GameObject> placeholders, bool active)
    {
        while (placeholders.Count == 0)
        {
            yield return null;
        }

        foreach (var placeholder in placeholders)
        {
            if (placeholder != null)
            {
                placeholder.SetActive(active);
            }
        }
    }

    public IEnumerator SetAllPlaceHoldersInactive()
    {
        yield return StartCoroutine(SetPlaceholderActiveState(PlayerEntities, false));
        Debug.Log("All placeholders set to inactive!");
    }

    public IEnumerator SetAllPlaceHoldersActive()
    {
        yield return StartCoroutine(SetPlaceholderActiveState(PlayerEntities, true));
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
