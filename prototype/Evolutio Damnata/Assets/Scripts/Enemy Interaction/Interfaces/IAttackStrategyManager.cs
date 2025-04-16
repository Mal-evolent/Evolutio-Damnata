using System.Collections.Generic;
using EnemyInteraction.Models;
using EnemyInteraction.Managers;

namespace EnemyInteraction.Interfaces
{
    public interface IAttackStrategyManager
    {
        List<EntityManager> GetAttackOrder(List<EntityManager> enemies, List<EntityManager> players,
                                          HealthIconManager healthIcon, BoardState boardState);
        EntityManager SelectTarget(EntityManager attacker, List<EntityManager> playerEntities,
                                 HealthIconManager playerHealthIcon, BoardState boardState, StrategicMode mode);
        StrategicMode DetermineStrategicMode(BoardState boardState);
        bool ShouldAttackHealthIcon(EntityManager attacker, List<EntityManager> playerEntities,
                                   HealthIconManager playerHealthIcon, BoardState boardState);
    }
}