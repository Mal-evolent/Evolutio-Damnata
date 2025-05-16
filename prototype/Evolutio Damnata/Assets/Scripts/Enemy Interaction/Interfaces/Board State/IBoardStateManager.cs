using EnemyInteraction.Models;

namespace EnemyInteraction.Interfaces
{
    public interface IBoardStateManager
    {
        bool IsInitialized { get; }
        BoardState EvaluateBoardState();
    }
} 