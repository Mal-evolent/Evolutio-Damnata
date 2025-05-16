using EnemyInteraction.Models;
using EnemyInteraction.Managers;

namespace EnemyInteraction.Interfaces
{
    public interface ITargetEvaluator
    {
        float EvaluateTarget(EntityManager attacker, EntityManager target, BoardState boardState, StrategicMode mode);
        float EvaluateKeywordInteractions(EntityManager attacker, EntityManager target, BoardState boardState);
        float EvaluateTurnOrderConsiderations(EntityManager attacker, EntityManager target, BoardState boardState);
    }
}