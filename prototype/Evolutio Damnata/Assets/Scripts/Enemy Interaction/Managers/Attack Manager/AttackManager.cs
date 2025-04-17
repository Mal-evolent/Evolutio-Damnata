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
        // Add singleton pattern
        public static AttackManager Instance { get; private set; }

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
            // Implement singleton pattern - only register if we're the first instance
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AttackManager] Another instance already exists, destroying this one");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Instead of starting initialization here, 
            // register this instance with AIServices and let it handle initialization
            StartCoroutine(RegisterWithAIServices());
        }

        private IEnumerator RegisterWithAIServices()
        {
            Debug.Log("[AttackManager] Registering with AIServices...");

            // Wait for AIServices to be available
            int attempts = 0;
            int maxAttempts = 30;

            while (AIServices.Instance == null && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (AIServices.Instance == null)
            {
                Debug.LogError("[AttackManager] AIServices not available after waiting");
                yield break;
            }

            // Register this instance with AIServices
            AIServices.Instance.RegisterAttackManager(this);

            // Wait for scene dependencies before initializing components
            yield return StartCoroutine(WaitForSceneDependencies());
        }

        private IEnumerator WaitForSceneDependencies()
        {
            Debug.Log("[AttackManager] Waiting for scene dependencies...");

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

            // Let AIServices know we're ready for dependency injection
            AIServices.Instance.InjectAttackManagerDependencies(this);
        }

        // Method that AIServices can call to inject dependencies
        public void InjectDependencies(
            IKeywordEvaluator keywordEvaluator,
            IBoardStateManager boardStateManager,
            IEntityCacheManager entityCacheManager)
        {
            Debug.Log("[AttackManager] Receiving dependencies from AIServices");

            _keywordEvaluator = keywordEvaluator;
            _boardStateManager = boardStateManager;
            _entityCacheManager = entityCacheManager;

            // After receiving dependencies, initialize components that rely on them
            InitializeRefactoredComponents();

            Debug.Log("[AttackManager] Initialization completed");
        }

        private void InitializeRefactoredComponents()
        {
            // Only create components, don't try to find or create manager instances
            // as they should be injected by AIServices

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

            // Get board state before making any decisions
            BoardState boardState = GetCurrentBoardState();

            // Check if the AI should consider skipping this turn
            if (ShouldConsiderSkippingTurn(boardState))
            {
                // If the decision is to skip, log this and exit
                Debug.Log("[AttackManager] AI decided to skip this turn for strategic reasons");
                yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 1.5f));
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

        /// <summary>
        /// Determines if the AI should consider skipping its attack turn
        /// based on strategic considerations and board state.
        /// </summary>
        /// <param name="boardState">Current state of the game board</param>
        /// <returns>True if the AI should skip this turn</returns>
        private bool ShouldConsiderSkippingTurn(BoardState boardState)
        {
            // First, check if we should even consider skipping (random chance)
            if (Random.value > _skipTurnConsiderationChance)
                return false;

            Debug.Log("[AttackManager] Considering whether to skip turn...");

            // Check if board state is available
            if (boardState == null)
                return false;

            // Calculate the enemy's board advantage
            float enemyBoardAdvantage = boardState.EnemyBoardControl /
                (boardState.PlayerBoardControl > 0 ? boardState.PlayerBoardControl : 1);

            // Check if the enemy has sufficient board advantage to consider skipping
            bool hasSufficientAdvantage = enemyBoardAdvantage >= _skipTurnBoardAdvantageThreshold;

            // Check if player will go first next turn (if we can determine this)
            bool playerGoesNextTurn = boardState.IsNextTurnPlayerFirst;

            // Check if player is at low health (making a skip less advisable)
            bool playerAtLowHealth = boardState.PlayerHealth <= 10;

            // Late game considerations (don't skip in late game)
            bool isLateGame = boardState.TurnCount >= 5;

            // Strategic mode from the strategy manager
            StrategicMode currentStrategy = _attackStrategyManager.DetermineStrategicMode(boardState);

            // Don't skip if we're in aggressive mode
            if (currentStrategy == StrategicMode.Aggro)
            {
                Debug.Log("[AttackManager] Won't skip turn - current strategy is aggressive");
                return false;
            }

            // Don't skip if player is at low health
            if (playerAtLowHealth)
            {
                Debug.Log("[AttackManager] Won't skip turn - player health is low, should press advantage");
                return false;
            }

            // Don't skip in late game
            if (isLateGame)
            {
                Debug.Log("[AttackManager] Won't skip turn - game is in later stages");
                return false;
            }

            // If enemy has significant board advantage and is in defensive mode,
            // consider skipping to preserve board position
            if (hasSufficientAdvantage && currentStrategy == StrategicMode.Defensive)
            {
                // If player goes next turn, higher chance to skip (preserve board for their turn)
                if (playerGoesNextTurn)
                {
                    Debug.Log($"[AttackManager] Skipping turn - enemy has board advantage of {enemyBoardAdvantage:F2} and player goes next");
                    return true;
                }

                // Even if we go next, still consider skipping with a good advantage
                if (enemyBoardAdvantage >= _skipTurnBoardAdvantageThreshold * 1.5f)
                {
                    Debug.Log($"[AttackManager] Skipping turn - enemy has strong board advantage of {enemyBoardAdvantage:F2}");
                    return true;
                }
            }

            Debug.Log("[AttackManager] Decided not to skip turn");
            return false;
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

        // Handle destruction cleanup for the singleton
        private void OnDestroy()
        {
            // Only clear the static reference if this instance is being destroyed
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    // Keep the enum in the same file
    public enum StrategicMode
    {
        Aggro,
        Defensive
    }
}
