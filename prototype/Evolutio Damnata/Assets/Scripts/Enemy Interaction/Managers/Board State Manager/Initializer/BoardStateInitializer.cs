using UnityEngine;
using System.Collections;
using EnemyInteraction.Interfaces;

namespace EnemyInteraction.Managers
{
    public class BoardStateInitializer
    {
        private readonly BoardStateManager _manager;
        private readonly float _timeout;
        private readonly float _checkDelay;
        private bool _isInitialized;

        private ICombatManager _combatManager;
        private IEntityCacheManager _entityCacheManager;
        private SpritePositioning _spritePositioning;
        private BoardStateEvaluator _boardStateEvaluator;

        public bool IsInitialized => _isInitialized;

        public BoardStateInitializer(BoardStateManager manager, float timeout, float checkDelay)
        {
            _manager = manager;
            _timeout = timeout;
            _checkDelay = checkDelay;
        }

        public void Initialize()
        {
            _manager.StartCoroutine(InitializeComponents());
        }

        private IEnumerator InitializeComponents()
        {
            Debug.Log("[BoardStateInitializer] Initializing components...");

            yield return _manager.StartCoroutine(InitializeCombatManager());
            yield return _manager.StartCoroutine(InitializeCombatStageAndPositioning());
            yield return _manager.StartCoroutine(InitializeEntityCache());

            // Initialize evaluator component
            BoardStateSettings settings = _manager.Settings;
            _boardStateEvaluator = new BoardStateEvaluator(_combatManager, settings);

            _isInitialized = true;

            // Notify manager that initialization is complete
            _manager.OnInitializationComplete(_combatManager, _entityCacheManager, _spritePositioning, _boardStateEvaluator);
        }

        private IEnumerator InitializeCombatManager()
        {
            _combatManager = Object.FindObjectOfType<CombatManager>();

            float timeout = Time.time + _timeout;
            while (_combatManager == null)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateInitializer] Timeout while waiting for CombatManager");
                    yield break;
                }

                yield return new WaitForSeconds(_checkDelay);
                _combatManager = Object.FindObjectOfType<CombatManager>();
            }

            _manager.SubscribeToCombatPhaseChanges(_combatManager);
        }

        private IEnumerator InitializeCombatStageAndPositioning()
        {
            var combatStage = Object.FindObjectOfType<CombatStage>();

            float timeout = Time.time + _timeout;
            while (combatStage == null)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateInitializer] Timeout while waiting for CombatStage");
                    yield break;
                }

                yield return new WaitForSeconds(_checkDelay);
                combatStage = Object.FindObjectOfType<CombatStage>();
            }

            timeout = Time.time + _timeout / 2;
            while (combatStage.SpritePositioning == null)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateInitializer] Timeout while waiting for SpritePositioning");
                    yield break;
                }

                yield return new WaitForSeconds(_checkDelay);
            }

            _spritePositioning = combatStage.SpritePositioning as SpritePositioning;
        }

        private IEnumerator InitializeEntityCache()
        {
            _entityCacheManager = EntityCacheManager.Instance;

            if (_entityCacheManager == null)
            {
                Debug.LogWarning("[BoardStateInitializer] EntityCacheManager singleton not found, creating one...");
                var cacheManagerObj = new GameObject("EntityCacheManager");
                _entityCacheManager = cacheManagerObj.AddComponent<EntityCacheManager>();

                var combatStage = Object.FindObjectOfType<CombatStage>();
                if (combatStage == null)
                {
                    Debug.LogError("[BoardStateInitializer] Could not find CombatStage for EntityCacheManager initialization");
                    yield break;
                }

                var attackLimiter = combatStage.GetAttackLimiter() ?? new AttackLimiter();
                (_entityCacheManager as EntityCacheManager).Initialize(_spritePositioning, attackLimiter);
            }
            else
            {
                Debug.Log("[BoardStateInitializer] Found existing EntityCacheManager instance");

                if (_entityCacheManager.EntityManagerCache == null || _entityCacheManager.EntityManagerCache.Count == 0)
                {
                    var combatStage = Object.FindObjectOfType<CombatStage>();
                    if (combatStage == null)
                    {
                        Debug.LogError("[BoardStateInitializer] Could not find CombatStage for EntityCacheManager initialization");
                        yield break;
                    }

                    var attackLimiter = combatStage.GetAttackLimiter() ?? new AttackLimiter();
                    (_entityCacheManager as EntityCacheManager).Initialize(_spritePositioning, attackLimiter);
                }
                else
                {
                    _manager.RefreshEntityCache();
                }
            }
        }
    }
}
