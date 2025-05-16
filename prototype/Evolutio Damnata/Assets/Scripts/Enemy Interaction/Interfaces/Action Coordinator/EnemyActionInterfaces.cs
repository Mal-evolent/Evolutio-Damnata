using System.Collections;

namespace EnemyInteraction.Interfaces
{
    public interface IEnemyCardPlayer
    {
        IEnumerator PlayCards();
    }
    
    public interface IEnemyAttacker
    {
        IEnumerator Attack();
    }
    
    public interface IEnemyLogger
    {
        void LogCardsInHand();
    }
    
    public interface IEnemyActions : IEnemyCardPlayer, IEnemyAttacker, IEnemyLogger 
    {
        bool IsInitialized { get; }
    }
}
