using UnityEngine;
using System.Collections;
using EnemyInteraction.Interfaces;

namespace EnemyInteraction.Managers
{
    public class AttackExecutor : MonoBehaviour, IAttackExecutor
    {
        private CombatStage _combatStage;
        private AttackLimiter _attackLimiter;
        private IEntityCacheManager _entityCacheManager;
        
        [SerializeField, Range(0.2f, 2f), Tooltip("Base delay between attack actions in seconds")]
        private float _baseAttackDelay = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("Random variance in delay timing (percentage)")]
        private float _delayVariance = 0.3f;
        
        public void Initialize(CombatStage combatStage, AttackLimiter attackLimiter, IEntityCacheManager entityCacheManager)
        {
            _combatStage = combatStage;
            _attackLimiter = attackLimiter;
            _entityCacheManager = entityCacheManager;
        }
        
        public IEnumerator ExecuteAttack(EntityManager attacker, EntityManager target)
        {
            _combatStage.HandleMonsterAttack(attacker, target);

            if (_attackLimiter != null)
            {
                _attackLimiter.RegisterAttack(attacker);
            }
            else
            {
                attacker.HasAttacked = true;
            }

            // Brief pause after attack execution to let animation play
            yield return new WaitForSeconds(0.2f);
        }
        
        public bool AttackPlayerHealthIcon(EntityManager attacker, HealthIconManager healthIcon)
        {
            if (attacker == null || healthIcon == null || _combatStage == null)
                return false;

            if ((_entityCacheManager as EntityCacheManager)?.HasEntitiesOnField(true) ?? false)
            {
                Debug.LogWarning("[AttackExecutor] Cannot attack health icon - player entities present");
                return false;
            }

            try
            {
                _combatStage.HandleMonsterAttack(attacker, healthIcon);

                if (_attackLimiter != null)
                    _attackLimiter.RegisterAttack(attacker);
                else
                    attacker.HasAttacked = true;

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AttackExecutor] Error attacking health icon: {e.Message}");
                return false;
            }
        }
        
        public float GetRandomizedDelay(float baseDelay)
        {
            if (_delayVariance <= 0) return baseDelay;

            float variance = baseDelay * _delayVariance;
            return baseDelay + Random.Range(-variance, variance);
        }
    }
}
