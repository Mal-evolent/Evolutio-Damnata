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
            float timeout = Time.time + _timeout;
            while (EntityCacheManager.Instance == null)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateInitializer] Timeout while waiting for EntityCacheManager singleton instance");
                    yield break;
                }
                yield return new WaitForSeconds(_checkDelay);
            }
            _entityCacheManager = EntityCacheManager.Instance;

            // Wait for SpritePositioning to be available
            timeout = Time.time + _timeout / 2;
            while (_spritePositioning == null)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateInitializer] Timeout while waiting for SpritePositioning before initializing EntityCacheManager");
                    yield break;
                }
                yield return new WaitForSeconds(_checkDelay);
            }

            // Wait for room to be ready
            timeout = Time.time + _timeout;
            while (!_spritePositioning.RoomReady)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateInitializer] Timeout while waiting for room to be ready");
                    yield break;
                }
                yield return new WaitForSeconds(_checkDelay);
            }

            var combatStage = Object.FindObjectOfType<CombatStage>();
            if (combatStage == null)
            {
                Debug.LogError("[BoardStateInitializer] Could not find CombatStage for EntityCacheManager initialization");
                yield break;
            }
            var attackLimiter = combatStage.GetAttackLimiter() ?? new AttackLimiter();
            (_entityCacheManager as EntityCacheManager).Initialize(_spritePositioning, attackLimiter);

            // Wait for EntityManagerCache to be populated
            timeout = Time.time + _timeout / 2;
            while (_entityCacheManager.EntityManagerCache == null || _entityCacheManager.EntityManagerCache.Count == 0)
            {
                if (Time.time > timeout)
                {
                    Debug.LogWarning("[BoardStateInitializer] EntityManagerCache is still empty after waiting, proceeding anyway");
                    break;
                }
                yield return new WaitForSeconds(_checkDelay);
            }
            _manager.RefreshEntityCache();
        }
    }
}
