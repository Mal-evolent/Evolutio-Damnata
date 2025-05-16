using System.Collections;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Managers;
using UnityEngine;

namespace EnemyInteraction.Services.Interfaces
{
    /// <summary>
    /// Responsible for managing scene dependencies
    /// </summary>
    public interface ISceneDependencyManager
    {
        ICombatManager CombatManager { get; }
        CombatStage CombatStage { get; }
        SpritePositioning SpritePositioning { get; }
        AttackLimiter AttackLimiter { get; }
        IEnumerator GatherSceneDependencies();
    }
}
