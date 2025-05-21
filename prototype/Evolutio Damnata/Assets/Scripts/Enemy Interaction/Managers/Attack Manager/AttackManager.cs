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

        // Helper classes for refactored functionality
        private AttackStrategyExecutor _attackStrategyExecutor;
        private TurnSkipEvaluator _turnSkipEvaluator;

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

        // Human error simulation parameters
        [Header("Human Error Simulation")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance to make a suboptimal target selection")]
        private float _suboptimalTargetChance = 0.15f;

        [SerializeField, Range(0f, 1f), Tooltip("Maximum randomization of delay times (0 = fixed delays, 1 = fully random)")]
        private float _delayRandomizationFactor = 0.3f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Chance to randomly change strategic mode during a turn")]
        private float _strategyChangeChance = 0.1f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Chance to reorder attack sequence suboptimally")]
        private float _attackOrderRandomizationChance = 0.2f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Chance to 'forget' to attack with an entity")]
        private float _missedAttackChance = 0.05f;

        [SerializeField, Range(0f, 0.3f), Tooltip("Chance to reconsider target after selecting")]
        private float _targetReconsiderationChance = 0.15f;

        [SerializeField, Range(0.5f, 3f), Tooltip("Additional thinking time when 'reconsidering' decisions")]
        private float _reconsiderationDelay = 1.2f;

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
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Instead of starting initialization here, 
            // register this instance with AIServices and let it handle initialization
            StartCoroutine(RegisterWithAIServices());
        }

        private void OnEnable()
        {
            // Register for scene load events
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            // Unregister to prevent memory leaks
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Debug.Log($"[AttackManager] Scene loaded: {scene.name}");
            // Only reacquire references if we're in the game scene
            if (scene.name == "gameScene")
            {
                StartCoroutine(ReacquireSceneReferences());
            }
            else
            {
                Debug.Log("[AttackManager] Not in game scene, destroying singleton and cleaning up");
                Destroy(gameObject); // This will trigger OnDestroy and clear Instance
            }
        }

        private IEnumerator ReacquireSceneReferences()
        {
            yield return new WaitForSeconds(0.5f); // Wait for scene to stabilize

            Debug.Log("[AttackManager] Reacquiring scene references...");

            // Clear references that might be stale
            _combatManager = null;
            _combatStage = null;
            _spritePositioning = null;

            // Reacquire references
            yield return StartCoroutine(WaitForSceneDependencies());

            Debug.Log("[AttackManager] Scene references reacquired");
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

                // Pass human error simulation parameters to AttackStrategyManager if it supports them
                if (attackStrategyManagerObj is IHumanErrorSimulator)
                {
                    (attackStrategyManagerObj as IHumanErrorSimulator).SetErrorSimulationParameters(
                        _suboptimalTargetChance,
                        _attackOrderRandomizationChance,
                        _strategyChangeChance
                    );
                }
            }

            // Create and initialize AttackExecutor
            if (_attackExecutor == null)
            {
                var attackExecutorObj = gameObject.AddComponent<AttackExecutor>();
                _attackExecutor = attackExecutorObj;
                (attackExecutorObj as AttackExecutor).Initialize(_combatStage, _attackLimiter, _entityCacheManager);

                // Pass delay randomization to AttackExecutor if it has this method
                if (attackExecutorObj is AttackExecutor)
                {
                    (attackExecutorObj as AttackExecutor).SetDelayRandomizationFactor(_delayRandomizationFactor);
                }
            }

            // Initialize refactored helper classes
            _turnSkipEvaluator = new TurnSkipEvaluator(_skipTurnConsiderationChance, _skipTurnBoardAdvantageThreshold);

            _attackStrategyExecutor = new AttackStrategyExecutor(
                _attackExecutor,
                _entityCacheManager,
                _attackStrategyManager,
                _evaluationDelay,
                _missedAttackChance,
                _strategyChangeChance,
                _targetReconsiderationChance,
                _reconsiderationDelay,
                _attackOrderRandomizationChance);
        }

        public IEnumerator Attack()
        {
            Debug.Log("[AttackManager] Starting Attack");

            // Initial delay before starting attack sequence - gives player time to prepare
            yield return new WaitForSeconds(_initialAttackDelay);

            // Validate prerequisites
            if (!AttackValidator.ValidateAttackPrerequisites(_combatManager, _spritePositioning))
            {
                yield return SimulatePlaceholderAttack();
                yield break;
            }

            // Get board state for decision making
            BoardState boardState = GetCurrentBoardState();

            // Check if we should skip turn
            if (_turnSkipEvaluator.ShouldSkipTurn(boardState, _entityCacheManager, _attackStrategyManager))
            {
                yield return PerformTurnSkip();
                yield break;
            }

            // Prepare attack context
            AttackContext context = PrepareAttackContext(boardState);

            // Validate the context
            if (!context.IsValid)
            {
                yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.5f));
                yield break;
            }

            // Execute the attack sequence
            yield return _attackStrategyExecutor.ExecuteAttackSequence(context);

            Debug.Log("[AttackManager] Attack completed");
        }

        private AttackContext PrepareAttackContext(BoardState boardState)
        {
            var context = new AttackContext();
            context.BoardState = boardState;

            // Refresh and get entity caches
            _entityCacheManager.RefreshEntityCaches();
            context.EnemyEntities = _entityCacheManager.CachedEnemyEntities;
            context.PlayerEntities = _entityCacheManager.CachedPlayerEntities;
            context.PlayerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();

            try
            {
                context.IsValid = AttackValidator.ValidateAttackScenario(
                    context.EnemyEntities,
                    context.PlayerEntities,
                    context.PlayerHealthIcon);
            }
            catch (System.Exception e)
            {
                context.ErrorMessage = e.Message;
                context.IsValid = false;
                Debug.LogError($"[AttackManager] Error preparing attack context: {e.Message}");
            }

            return context;
        }

        private IEnumerator PerformTurnSkip()
        {
            Debug.Log("[AttackManager] AI decided to skip this turn for strategic reasons");
            yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 1.5f));
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
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("[AttackManager] Singleton destroyed and static instance cleared");
            }
        }
    }

    // New interface for components that support human error simulation parameters
    public interface IHumanErrorSimulator
    {
        void SetErrorSimulationParameters(float suboptimalTargetChance, float attackOrderRandomizationChance, float strategyChangeChance);
    }

    // Keep the enum in the same file
    public enum StrategicMode
    {
        Aggro,
        Defensive
    }
}
