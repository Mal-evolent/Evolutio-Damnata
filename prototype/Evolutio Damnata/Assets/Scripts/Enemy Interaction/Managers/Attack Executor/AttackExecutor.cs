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
            // Skip attack execution if attacker has 0 attack
            if (attacker != null && attacker.GetAttack() <= 0)
            {
                Debug.Log($"[AttackExecutor] Skipping attack with {attacker.name} as it has 0 attack (defensive unit)");
                yield break;
            }

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

            // Skip attack if attacker has 0 attack
            if (attacker.GetAttack() <= 0)
            {
                Debug.Log($"[AttackExecutor] Skipping health icon attack with {attacker.name} as it has 0 attack (defensive unit)");
                return false;
            }

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
        public void SetDelayRandomizationFactor(float factor)
        {
            _delayVariance = Mathf.Clamp01(factor);
        }
    }
}
