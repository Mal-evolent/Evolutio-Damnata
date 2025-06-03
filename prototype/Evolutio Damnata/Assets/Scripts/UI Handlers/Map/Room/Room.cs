using UnityEngine;
using UnityEngine.UI;

public class Room : MonoBehaviour
{
    [Header("Room State")]
    [SerializeField] private RoomType roomType;
    [SerializeField] private bool isVisited = false;
    [SerializeField] private bool isCurrentRoom = false;
    
    [Header("Visuals")]
    [SerializeField] private Image roomImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color currentRoomColor = Color.red;
    [SerializeField] private Color visitedColor = Color.green;
    
    [Header("Room Criteria")]
    [SerializeField] private int minLevel = 1;
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private bool requiresKey = false;
    
    private Cell cellComponent;
    private static Room currentRoom;

    private void Awake()
    {
        cellComponent = GetComponent<Cell>();
        if (cellComponent == null)
        {
            Debug.LogError("Room must be attached to a GameObject with a Cell component");
            enabled = false;
            return;
        }

        if (roomImage == null)
        {
            roomImage = GetComponent<Image>();
            if (roomImage == null)
            {
                Debug.LogError("Room must have an Image component");
                enabled = false;
                return;
            }
        }
    }

    public void SetAsCurrentRoom()
    {
        // If there was a previous current room, mark it as visited
        if (currentRoom != null && currentRoom != this)
        {
            currentRoom.SetAsVisited();
        }
        
        currentRoom = this;
        isCurrentRoom = true;
        isVisited = true;
        UpdateVisuals();
    }

    public void SetAsVisited()
    {
        isCurrentRoom = false;
        isVisited = true;
        UpdateVisuals();
    }

    public void ResetRoom()
    {
        isVisited = false;
        isCurrentRoom = false;
        if (currentRoom == this)
        {
            currentRoom = null;
        }
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (isCurrentRoom)
        {
            roomImage.color = currentRoomColor;
        }
        else if (isVisited)
        {
            roomImage.color = visitedColor;
        }
        else
        {
            roomImage.color = normalColor;
        }
    }

    public bool CanEnterRoom(int playerLevel)
    {
        // Allow entering any room that isn't the current room
        if (isCurrentRoom) return false;
        
        // Check other criteria
        if (playerLevel < minLevel || playerLevel > maxLevel) return false;
        if (requiresKey && !HasKey()) return false;
        return true;
    }

    private bool HasKey()
    {
        // TODO: Implement key checking logic
        return true;
    }

    public void OnRoomEnter()
    {
        if (!CanEnterRoom(1)) return; // TODO: Pass actual player level

        switch (roomType)
        {
            case RoomType.Normal:
                // Random chance to trigger combat
                if (Random.value < 0.7f) // 70% chance
                {
                    TriggerCombat();
                }
                break;
            case RoomType.Boss:
                TriggerCombat();
                break;
            case RoomType.Shop:
                // TODO: Implement shop logic
                break;
            case RoomType.Item:
                // TODO: Implement item room logic
                break;
            case RoomType.Secret:
                // TODO: Implement secret room logic
                break;
        }

        SetAsCurrentRoom();
    }

    private void TriggerCombat()
    {
        // TODO: Implement combat initialization
        Debug.Log($"Triggering combat in room {cellComponent.Index}");
    }
} 