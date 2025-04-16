using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;
using EnemyInteraction.Utilities;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Services;


namespace EnemyInteraction.Managers
{
    public class AttackManager : MonoBehaviour, IAttackManager
    {
        [SerializeField] private SpritePositioning _spritePositioning;
        
        // Core components
        private ICombatManager _combatManager;
        private CombatStage _combatStage;
        private IKeywordEvaluator _keywordEvaluator;
        private IBoardStateManager _boardStateManager;
        private AttackLimiter _attackLimiter;
        
        // Refactored components
        private IEntityCacheManager _entityCacheManager;
        private IAttackStrategyManager _attackStrategyManager;
        private ITargetEvaluator _targetEvaluator;
        private IAttackExecutor _attackExecutor;

        // Delay control parameters
        [SerializeField, Range(0.1f, 1.5f), Tooltip("Initial delay before starting attack sequence")]
        private float _initialAttackDelay = 0.8f;

        [SerializeField, Range(0.1f, 1f), Tooltip("Delay after attack evaluations")]
        private float _evaluationDelay = 0.4f;

        // Turn skipping settings
        [Header("Turn Skipping Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance to consider skipping a turn")]
        private float _skipTurnConsiderationChance = 0.25f;

        [SerializeField, Tooltip("Minimum enemy board advantage required to consider skipping")]
        private float _skipTurnBoardAdvantageThreshold = 1.5f;

        private void Awake()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[AttackManager] Starting initialization...");

            int maxAttempts = 30;
            int attempts = 0;

            // Initialize the primary components
            while (attempts < maxAttempts)
            {
                _combatManager = _combatManager ?? FindObjectOfType<CombatManager>();
                _combatStage = _combatStage ?? FindObjectOfType<CombatStage>();

                if (_combatManager != null && _combatStage != null)
                    break;

                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (_combatStage != null)
            {
                attempts = 0;
                while (_combatStage.SpritePositioning == null && attempts < maxAttempts)
                {
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }

                if (_spritePositioning == null && _combatStage.SpritePositioning != null)
                {
                    _spritePositioning = _combatStage.SpritePositioning as SpritePositioning;
                }

                if (_attackLimiter == null)
                {
                    _attackLimiter = _combatStage.GetAttackLimiter();
                }
            }

            if (AIServices.Instance != null)
            {
                var services = AIServices.Instance;
                _keywordEvaluator = _keywordEvaluator ?? services.KeywordEvaluator;
                _boardStateManager = _boardStateManager ?? services.BoardStateManager;
            }

            if (_keywordEvaluator == null)
            {
                var keywordEvaluatorObj = new GameObject("KeywordEvaluator_Local");
                keywordEvaluatorObj.transform.SetParent(transform);
                _keywordEvaluator = keywordEvaluatorObj.AddComponent<KeywordEvaluator>();
            }

            if (_boardStateManager == null)
            {
                var boardStateManagerObj = new GameObject("BoardStateManager_Local");
                boardStateManagerObj.transform.SetParent(transform);
                _boardStateManager = boardStateManagerObj.AddComponent<BoardStateManager>();
            }

            // Initialize refactored components
            InitializeRefactoredComponents();

            Debug.Log("[AttackManager] Initialization completed");
        }

        private void InitializeRefactoredComponents()
        {
            // Create and initialize EntityCacheManager
            if (_entityCacheManager == null)
            {
                var entityCacheManagerObj = gameObject.AddComponent<EntityCacheManager>();
                _entityCacheManager = entityCacheManagerObj;
                (entityCacheManagerObj as EntityCacheManager).Initialize(_spritePositioning, _attackLimiter);
            }
            
            // Create and initialize TargetEvaluator
            if (_targetEvaluator == null)
            {
                var targetEvaluatorObj = gameObject.AddComponent<TargetEvaluator>();
                _targetEvaluator = targetEvaluatorObj;
                (targetEvaluatorObj as TargetEvaluator).Initialize(_keywordEvaluator, _entityCacheManager);
            }
            
            // Create and initialize AttackStrategyManager
            if (_attackStrategyManager == null)
            {
                var attackStrategyManagerObj = gameObject.AddComponent<AttackStrategyManager>();
                _attackStrategyManager = attackStrategyManagerObj;
                (attackStrategyManagerObj as AttackStrategyManager).Initialize(_targetEvaluator, _entityCacheManager);
            }
            
            // Create and initialize AttackExecutor
            if (_attackExecutor == null)
            {
                var attackExecutorObj = gameObject.AddComponent<AttackExecutor>();
                _attackExecutor = attackExecutorObj;
                (attackExecutorObj as AttackExecutor).Initialize(_combatStage, _attackLimiter, _entityCacheManager);
            }
        }

        public IEnumerator Attack()
        {
            Debug.Log("[AttackManager] Starting Attack");

            // Initial delay before starting attack sequence - gives player time to prepare
            yield return new WaitForSeconds(_initialAttackDelay);

            if (_combatManager == null || _spritePositioning == null)
            {
                yield return SimulatePlaceholderAttack();
                yield break;
            }

            if (!ValidateCombatState())
            {
                yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.5f));
                yield break;
            }

            // Use cached entities
            _entityCacheManager.RefreshEntityCaches();
            List<EntityManager> enemyEntities = _entityCacheManager.CachedEnemyEntities;
            List<EntityManager> playerEntities = _entityCacheManager.CachedPlayerEntities;
            HealthIconManager playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();

            bool setupSuccess = false;
            string errorMessage = null;

            try
            {
                setupSuccess = ValidateAttackScenario(enemyEntities, playerEntities, playerHealthIcon);
            }
            catch (System.Exception e)
            {
                errorMessage = e.Message;
                Debug.LogError($"[AttackManager] Error in Attack: {errorMessage}");
            }

            if (!setupSuccess || errorMessage != null)
            {
                yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.5f));
                yield break;
            }

            // Delay to simulate "thinking" about attack strategy
            yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay));

            BoardState boardState = GetCurrentBoardState();
            var attackOrder = _attackStrategyManager.GetAttackOrder(enemyEntities, playerEntities, playerHealthIcon, boardState);

            StrategicMode mode = _attackStrategyManager.DetermineStrategicMode(boardState);
            Debug.Log($"[AttackManager] Current strategy: {mode}");

            // Brief pause after determining strategy before first attack
            yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.5f));

            // Process each attack with appropriate delays
            foreach (var attacker in attackOrder)
            {
                // Delay before selecting target - simulates AI "thinking"
                yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.7f));

                EntityManager targetEntity = _attackStrategyManager.SelectTarget(attacker, playerEntities, playerHealthIcon, boardState, mode);

                if (targetEntity != null)
                {
                    // Small delay between target selection and attack execution
                    yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.3f));

                    yield return _attackExecutor.ExecuteAttack(attacker, targetEntity);

                    if (targetEntity.dead)
                    {
                        // Add longer pause after killing a unit to emphasize the moment
                        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 1.2f));

                        _entityCacheManager.RefreshEntityCaches();
                        playerEntities = _entityCacheManager.CachedPlayerEntities;
                        if (playerEntities.Count == 0 && playerHealthIcon == null) break;
                    }
                    else
                    {
                        // Normal post-attack delay
                        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay));
                    }
                }
                else if (playerHealthIcon != null && 
                         _attackStrategyManager.ShouldAttackHealthIcon(attacker, playerEntities, playerHealthIcon, boardState))
                {
                    // Dramatic pause before attacking health icon directly
                    yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.5f));

                    _attackExecutor.AttackPlayerHealthIcon(attacker, playerHealthIcon);

                    // Longer pause after attacking health icon to emphasize importance
                    yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 1.5f));
                }
            }

            // Final delay after attack sequence completes
            yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay));
            Debug.Log("[AttackManager] Attack completed");
        }

        private bool ValidateCombatState()
        {
            return _combatManager != null && _combatManager.IsEnemyCombatPhase();
        }

        private bool ValidateAttackScenario(List<EntityManager> enemyEntities, List<EntityManager> playerEntities,
                                          HealthIconManager playerHealthIcon)
        {
            bool hasEnemyEntities = enemyEntities != null && enemyEntities.Count > 0;
            bool hasTargets = (playerEntities != null && playerEntities.Count > 0) || playerHealthIcon != null;

            if (!hasEnemyEntities)
                Debug.Log("[AttackManager] No enemy entities available to attack");

            if (!hasTargets)
                Debug.Log("[AttackManager] No valid targets available");

            return hasEnemyEntities && hasTargets;
        }

        private BoardState GetCurrentBoardState()
        {
            return _boardStateManager?.EvaluateBoardState() ?? new BoardState
            {
                EnemyHealth = _combatManager.EnemyHealth,
                PlayerHealth = _combatManager.PlayerHealth,
                TurnCount = _combatManager.TurnCount
            };
        }

        private IEnumerator SimulatePlaceholderAttack()
        {
            Debug.LogWarning("[AttackManager] Using placeholder attack implementation");
            yield return new WaitForSeconds(_attackExecutor?.GetRandomizedDelay(_evaluationDelay) ?? 0.5f);
            Debug.Log("[AttackManager] Simulating enemy attacks");
            yield return new WaitForSeconds(_attackExecutor?.GetRandomizedDelay(_evaluationDelay * 1.5f) ?? 0.8f);
        }
    }

    // Keep the enum in the same file
    public enum StrategicMode
    {
        Aggro,
        Defensive
    }
}
