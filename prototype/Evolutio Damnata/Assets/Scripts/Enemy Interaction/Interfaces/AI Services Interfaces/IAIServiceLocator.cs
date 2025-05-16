using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using EnemyInteraction.Interfaces;

namespace EnemyInteraction.Services.Interfaces
{
    /// <summary>
    /// Interface for the service locator, providing access to all AI-related services
    /// </summary>
    public interface IAIServiceLocator
    {
        IKeywordEvaluator KeywordEvaluator { get; }
        IEffectEvaluator EffectEvaluator { get; }
        IBoardStateManager BoardStateManager { get; }
        IEntityCacheManager EntityCacheManager { get; }
        CardPlayManager CardPlayManager { get; }
        AttackManager AttackManager { get; }
        bool IsInitialized { get; }
    }
}
