using EnemyInteraction.Managers;

namespace EnemyInteraction.Services.Interfaces
{
    /// <summary>
    /// Interface for dependency registration
    /// </summary>
    public interface IAIServiceRegistry
    {
        void RegisterAttackManager(AttackManager attackManager);
        void RegisterBoardStateManager(BoardStateManager boardStateManager);
        void RegisterEntityCacheManager(EntityCacheManager entityCacheManager);
        void RegisterCardPlayManager(CardPlayManager cardPlayManager);
        void RegisterService<T>(T service) where T : class;
    }
}
