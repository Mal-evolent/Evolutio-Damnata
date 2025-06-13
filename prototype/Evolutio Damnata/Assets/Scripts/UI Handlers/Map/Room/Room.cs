using UnityEngine;
using UnityEngine.UI;

public class Room : MonoBehaviour
{
    [Header("Room State")]
    [SerializeField] private RoomType roomType;

    [Header("Visuals")]
    [SerializeField] private Image roomImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color currentRoomColor = Color.red;
    [SerializeField] private Color clearedColor = Color.green;
    [SerializeField] private Color currentClearedColor = Color.yellow;

    [Header("Debug State")]
    [SerializeField, Tooltip("Is this room cleared? (Read-only)")]
    private bool debugIsCleared;
    [SerializeField, Tooltip("Is this the current room? (Read-only)")]
    private bool debugIsCurrentRoom;

    private Cell cellComponent;
    private IRoomState roomState;
    private IRoomVisuals roomVisuals;
    private IRoomBehavior roomBehavior;
    private ICombatTrigger combatTrigger;

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

        InitializeComponents();

        // Subscribe to state changes for automatic visual updates
        if (roomState is RoomState concreteState)
        {
            concreteState.OnStateChanged += UpdateVisuals;
        }
    }

    private void InitializeComponents()
    {
        roomState = new RoomState(cellComponent);
        roomVisuals = new RoomVisuals(normalColor, currentRoomColor, clearedColor, currentClearedColor);
        roomVisuals.SetRoomImage(roomImage);
        combatTrigger = new CombatTrigger();
        roomBehavior = CreateRoomBehavior();
    }

    private IRoomBehavior CreateRoomBehavior()
    {
        return roomType switch
        {
            RoomType.Normal => new NormalRoomBehavior(roomState, cellComponent, combatTrigger),
            RoomType.Boss => new BossRoomBehavior(roomState, cellComponent, combatTrigger),
            RoomType.Shop => new ShopRoomBehavior(roomState, cellComponent, combatTrigger),
            RoomType.Item => new ItemRoomBehavior(roomState, cellComponent, combatTrigger),
            RoomType.Secret => new SecretRoomBehavior(roomState, cellComponent, combatTrigger),
            _ => new NormalRoomBehavior(roomState, cellComponent, combatTrigger)
        };
    }

    public void OnRoomEnter()
    {
        var currentRoom = RoomState.GetCurrentRoom();
        roomBehavior.OnRoomEnter(currentRoom);
        roomVisuals.UpdateVisuals(roomState.IsCurrentRoom, roomState.IsCleared);
    }

    public void ResetRoom()
    {
        roomState.ResetRoom();
        roomVisuals.UpdateVisuals(roomState.IsCurrentRoom, roomState.IsCleared);
    }

    private void UpdateVisuals()
    {
        roomVisuals.UpdateVisuals(roomState.IsCurrentRoom, roomState.IsCleared);
        debugIsCleared = roomState.IsCleared;
        debugIsCurrentRoom = roomState.IsCurrentRoom;
    }

    public void ForceClearAndSetCurrent()
    {
        if (roomState is RoomState concreteState)
        {
            concreteState.SetAsCleared();
            concreteState.SetAsCurrentRoom();
        }
    }
}
