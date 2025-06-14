using System;
using UnityEngine;

public class RoomState : IRoomState
{
    private bool isCurrentRoom;
    private bool isCleared;
    private static RoomState currentRoom;
    private readonly Cell cellComponent;

    public event Action OnStateChanged;

    public bool IsCurrentRoom => isCurrentRoom;
    public bool IsCleared => isCleared;
    public Cell CellComponent => cellComponent;

    public RoomState(Cell cellComponent)
    {
        this.cellComponent = cellComponent;
    }

    public void SetAsCurrentRoom()
    {
        if (currentRoom != null && currentRoom != this)
        {
            // Instead of calling SetAsCleared(), just update the current room state
            currentRoom.isCurrentRoom = false;
            currentRoom.OnStateChanged?.Invoke();
        }

        currentRoom = this;
        isCurrentRoom = true;
        OnStateChanged?.Invoke();
    }

    public void SetAsCleared()
    {
        isCleared = true;
        OnStateChanged?.Invoke();
    }

    public void ResetRoom()
    {
        isCurrentRoom = false;
        isCleared = false;
        if (currentRoom == this)
        {
            currentRoom = null;
        }
        OnStateChanged?.Invoke();
    }

    public static RoomState GetCurrentRoom() => currentRoom;
} 