using UnityEngine;
using UnityEngine.UI;

public class Room : MonoBehaviour
{
    [Header("Room State")]
    [SerializeField] private RoomType roomType;
    [SerializeField] private bool isCurrentRoom = false;
    [SerializeField] private bool isCleared = false;
    
    [Header("Visuals")]
    [SerializeField] private Image roomImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color currentRoomColor = Color.red;
    [SerializeField] private Color clearedColor = Color.green;
    
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
        // If there was a previous current room, mark it as cleared
        if (currentRoom != null && currentRoom != this)
        {
            currentRoom.SetAsCleared();
        }
        
        currentRoom = this;
        isCurrentRoom = true;
        // Don't override isCleared if it's already set
        UpdateVisuals();
    }

    public void ResetRoom()
    {
        isCurrentRoom = false;
        isCleared = false;
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
        else if (isCleared)
        {
            roomImage.color = clearedColor;
        }
        else
        {
            roomImage.color = normalColor;
        }
    }

    public bool CanEnterRoom(Room fromRoom)
    {
        // Can't enter current room
        if (isCurrentRoom)
        {
            Debug.LogWarning("Cannot enter current room");
            return false;
        }
        
        // Can enter if this room is already cleared
        if (isCleared) return true;
        
        // Can enter if coming from a cleared room and this room is adjacent
        if (fromRoom != null && fromRoom.isCleared)
        {
            if (cellComponent.IsAdjacentTo(fromRoom.cellComponent))
            {
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot enter room: Must be adjacent to a cleared room");
                return false;
            }
        }
        
        Debug.LogWarning("Cannot enter room: Must be adjacent to a cleared room or the room must be cleared");
        return false;
    }

    public void SetAsCleared()
    {
        isCleared = true;
        isCurrentRoom = false;
        UpdateVisuals();
    }

    public void OnRoomEnter()
    {
        if (!CanEnterRoom(currentRoom)) return;

        switch (roomType)
        {
            case RoomType.Normal:
                // Random chance to trigger combat
                if (Random.value < 0.7f) // 70% chance
                {
                    TriggerCombat();
                }
                else
                {
                    // If no combat, room is automatically cleared
                    SetAsCleared();
                }
                break;
            case RoomType.Boss:
                TriggerCombat();
                break;
            case RoomType.Shop:
                // TODO: Implement shop logic
                SetAsCleared(); // Shop is cleared when you leave it
                break;
            case RoomType.Item:
                // TODO: Implement item room logic
                SetAsCleared(); // Item room is cleared when you get the item
                break;
            case RoomType.Secret:
                // TODO: Implement secret room logic
                SetAsCleared(); // Secret room is cleared when you discover its secret
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