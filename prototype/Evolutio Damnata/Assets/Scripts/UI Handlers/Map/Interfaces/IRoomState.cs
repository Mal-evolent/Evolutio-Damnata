public interface IRoomState
{
    bool IsCurrentRoom { get; }
    bool IsCleared { get; }
    void SetAsCurrentRoom();
    void SetAsCleared();
    void ResetRoom();
} 