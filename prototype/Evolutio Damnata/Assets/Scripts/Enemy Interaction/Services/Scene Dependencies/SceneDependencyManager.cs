using System.Collections;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Managers;
using EnemyInteraction.Services.Interfaces;
using UnityEngine;

namespace EnemyInteraction.Services
{
    /// <summary>
    /// Scene Dependency Manager - Responsible for finding and managing scene-level dependencies
    /// </summary>
    public class SceneDependencyManager : ISceneDependencyManager
    {
        private ICombatManager _combatManager;
        private CombatStage _combatStage;
        private SpritePositioning _spritePositioning;
        private AttackLimiter _attackLimiter;

        public ICombatManager CombatManager => _combatManager;
        public CombatStage CombatStage => _combatStage;
        public SpritePositioning SpritePositioning => _spritePositioning;
        public AttackLimiter AttackLimiter => _attackLimiter;

        public IEnumerator GatherSceneDependencies()
        {
            Debug.Log("[SceneDependencyManager] Gathering scene dependencies...");

            int attempts = 0;
            int maxAttempts = 50;

            while ((_combatManager == null || _combatStage == null || _spritePositioning == null) && attempts < maxAttempts)
            {
                _combatManager = _combatManager ?? Object.FindObjectOfType<CombatManager>();
                _combatStage = _combatStage ?? Object.FindObjectOfType<CombatStage>();

                if (_combatStage != null)
                {
                    _spritePositioning = _spritePositioning ?? _combatStage.SpritePositioning as SpritePositioning;
                    _attackLimiter = _attackLimiter ?? _combatStage.GetAttackLimiter();
                }

                if (_combatManager == null || _combatStage == null || _spritePositioning == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }
            }

            if (_combatManager == null || _combatStage == null || _spritePositioning == null)
            {
                Debug.LogWarning("[SceneDependencyManager] Could not find all required scene dependencies after multiple attempts");
            }
            else
            {
                Debug.Log("[SceneDependencyManager] Scene dependencies gathered successfully");
            }
        }
    }
}
