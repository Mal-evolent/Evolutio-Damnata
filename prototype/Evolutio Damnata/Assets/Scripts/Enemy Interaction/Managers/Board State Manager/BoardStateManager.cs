// Assets/Scripts/Enemy Interaction/Managers/Board State Manager/BoardStateManager.cs
using UnityEngine;
using System.Collections;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;
using System.Collections.Generic;
using System;

namespace EnemyInteraction.Managers
{
    public class BoardStateManager : MonoBehaviour, IBoardStateManager
    {
        // Singleton instance
        public static BoardStateManager Instance { get; private set; }

        [Header("Core Settings")]
        [SerializeField] private SpritePositioning _spritePositioning;
        [SerializeField] private BoardStateSettings _settings;
        [SerializeField, Tooltip("Timeout in seconds before cached board state is refreshed")]
        private float _cacheTimeout = 0.5f;

        [Header("Initialization Settings")]
        [SerializeField, Range(5, 60), Tooltip("Maximum timeout in seconds for component initialization")]
        private float _initializationTimeout = 10f;
        [SerializeField, Range(0.05f, 1f), Tooltip("Delay between initialization checks in seconds")]
        private float _initializationCheckDelay = 0.1f;

        private ICombatManager _combatManager;
        private IEntityCacheManager _entityCacheManager;
        private BoardStateEvaluator _boardStateEvaluator;
        private bool _isInitialized;

        // Caching mechanism
        private BoardState _cachedBoardState;
        private float _lastUpdateTime;

        // Event for board state updates
        public event Action<BoardState> OnBoardStateUpdated;

        // Properties to expose to other managers
        public bool IsInitialized => _isInitialized;
        public Dictionary<GameObject, EntityManager> EntityCache => _entityCacheManager?.EntityManagerCache;

        private void Awake()
        {
            InitializeSingleton();
        }

        private void InitializeSingleton()
        {
            // Implement singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[BoardStateManager] Another instance already exists, destroying this one");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Create default settings if none are assigned
            if (_settings == null)
            {
                Debug.LogWarning("[BoardStateManager] BoardStateSettings not assigned in inspector, creating default instance");
                _settings = ScriptableObject.CreateInstance<BoardStateSettings>();
            }

            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[BoardStateManager] Initializing...");

            yield return StartCoroutine(InitializeCombatManager());
            yield return StartCoroutine(InitializeCombatStageAndPositioning());
            yield return StartCoroutine(InitializeEntityCache());

            // Initialize our evaluator component
            _boardStateEvaluator = new BoardStateEvaluator(_combatManager, _settings);

            _isInitialized = true;
            Debug.Log("[BoardStateManager] Initialization complete");
        }

        private IEnumerator InitializeCombatManager()
        {
            _combatManager = FindObjectOfType<CombatManager>();

            // Wait for combat manager to be available
            float timeout = Time.time + _initializationTimeout;
            while (_combatManager == null)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateManager] Timeout while waiting for CombatManager");
                    yield break;
                }

                yield return new WaitForSeconds(_initializationCheckDelay);
                _combatManager = FindObjectOfType<CombatManager>();
            }

            // Subscribe to phase changes
            _combatManager.SubscribeToPhaseChanges(OnPhaseChanged);
        }

        private IEnumerator InitializeCombatStageAndPositioning()
        {
            var combatStage = FindObjectOfType<CombatStage>();

            // Wait for combat stage to be available
            float timeout = Time.time + _initializationTimeout;
            while (combatStage == null)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateManager] Timeout while waiting for CombatStage");
                    yield break;
                }

                yield return new WaitForSeconds(_initializationCheckDelay);
                combatStage = FindObjectOfType<CombatStage>();
            }

            // Wait for sprite positioning to be available
            timeout = Time.time + _initializationTimeout / 2;
            while (combatStage.SpritePositioning == null)
            {
                if (Time.time > timeout)
                {
                    Debug.LogError("[BoardStateManager] Timeout while waiting for SpritePositioning");
                    yield break;
                }

                yield return new WaitForSeconds(_initializationCheckDelay);
            }

            _spritePositioning ??= combatStage.SpritePositioning as SpritePositioning;
        }

        private IEnumerator InitializeEntityCache()
        {
            // Get or create the EntityCacheManager singleton instance
            _entityCacheManager = EntityCacheManager.Instance;

            if (_entityCacheManager == null)
            {
                Debug.LogWarning("[BoardStateManager] EntityCacheManager singleton not found, creating one...");
                var cacheManagerObj = new GameObject("EntityCacheManager");
                _entityCacheManager = cacheManagerObj.AddComponent<EntityCacheManager>();

                // Get the combat stage for attack limiter
                var combatStage = FindObjectOfType<CombatStage>();
                if (combatStage == null)
                {
                    Debug.LogError("[BoardStateManager] Could not find CombatStage for EntityCacheManager initialization");
                    yield break;
                }

                // Create a new AttackLimiter instance
                var attackLimiter = combatStage.GetAttackLimiter() ?? new AttackLimiter();

                // Initialize the entity cache manager
                (_entityCacheManager as EntityCacheManager).Initialize(_spritePositioning, attackLimiter);
            }
            else
            {
                Debug.Log("[BoardStateManager] Found existing EntityCacheManager instance");

                // Ensure the entity cache is properly initialized with current references
                if (_entityCacheManager.EntityManagerCache == null || _entityCacheManager.EntityManagerCache.Count == 0)
                {
                    var combatStage = FindObjectOfType<CombatStage>();
                    if (combatStage == null)
                    {
                        Debug.LogError("[BoardStateManager] Could not find CombatStage for EntityCacheManager initialization");
                        yield break;
                    }

                    var attackLimiter = combatStage.GetAttackLimiter() ?? new AttackLimiter();
                    (_entityCacheManager as EntityCacheManager).Initialize(_spritePositioning, attackLimiter);
                }
                else
                {
                    // Just refresh caches to ensure they're up-to-date
                    RefreshEntityCache();
                }
            }
        }

        private void OnEnable()
        {
            // Ensure we're subscribed to phase changes if already initialized
            if (_isInitialized && _combatManager != null)
            {
                _combatManager.SubscribeToPhaseChanges(OnPhaseChanged);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from phase changes
            if (_combatManager != null)
            {
                _combatManager.UnsubscribeFromPhaseChanges(OnPhaseChanged);
            }
        }

        private void OnPhaseChanged(CombatPhase newPhase)
        {
            // Refresh entity cache and board state when phase changes
            RefreshEntityCache();

            // Invalidate cached board state to force an update
            _cachedBoardState = null;
        }

        /// <summary>
        /// Gets the current board state, using cache if available and not expired
        /// </summary>
        /// <param name="forceRefresh">Force recalculation of the board state</param>
        /// <returns>Current BoardState</returns>
        public BoardState GetBoardState(bool forceRefresh = false)
        {
            float currentTime = Time.time;

            // If we need to refresh or don't have a cached state or cache is expired
            if (forceRefresh || _cachedBoardState == null || (currentTime - _lastUpdateTime > _cacheTimeout))
            {
                _cachedBoardState = EvaluateBoardState();
                _lastUpdateTime = currentTime;

                // Trigger event
                OnBoardStateUpdated?.Invoke(_cachedBoardState);
            }

            return _cachedBoardState;
        }

        /// <summary>
        /// Evaluates and creates a new board state based on current game conditions
        /// </summary>
        /// <returns>Newly created board state or null if dependencies unavailable</returns>
        public BoardState EvaluateBoardState()
        {
            if (!ValidateDependencies())
            {
                Debug.LogError("[BoardStateManager] Evaluation failed - dependencies missing");
                return null;
            }

            // Refresh entity cache before evaluating
            _entityCacheManager.RefreshEntityCaches();

            try
            {
                // Create a board state with deck references
                BoardState state = CreateBoardStateWithDeckReferences();

                // Update from combat manager (using BoardState's built-in method)
                state.UpdateFromCombatManager(_combatManager);

                // Update monster lists
                state.UpdateMonsters(
                    _entityCacheManager.CachedPlayerEntities,
                    _entityCacheManager.CachedEnemyEntities);

                // Calculate board control metrics
                state.UpdateBoardControlMetrics();

                // Apply evaluator factors that modify the base metrics
                ApplyEvaluatorFactors(state);

                return state;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BoardStateManager] Exception during board state evaluation: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Creates a BoardState with properly initialized deck references
        /// </summary>
        private BoardState CreateBoardStateWithDeckReferences()
        {
            if (_combatManager.PlayerDeck != null && _combatManager.EnemyDeck != null)
            {
                return new BoardState(_combatManager.PlayerDeck, _combatManager.EnemyDeck);
            }
            else
            {
                Debug.LogWarning("[BoardStateManager] One or both deck references are null, creating empty BoardState");
                return new BoardState();
            }
        }

        /// <summary>
        /// Applies various evaluation factors to the board state
        /// </summary>
        private void ApplyEvaluatorFactors(BoardState state)
        {
            _boardStateEvaluator.ApplyBoardPositioningFactors(state);
            _boardStateEvaluator.ApplyResourceAdvantages(state);
            _boardStateEvaluator.ApplyHealthBasedFactors(state);
            _boardStateEvaluator.ApplyTurnOrderInfluence(state);
        }

        /// <summary>
        /// Validates that all required dependencies are available
        /// </summary>
        private bool ValidateDependencies()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[BoardStateManager] Not yet initialized");
                return false;
            }

            if (_combatManager == null)
            {
                Debug.LogWarning("[BoardStateManager] CombatManager reference missing");
                return false;
            }

            if (_entityCacheManager == null)
            {
                Debug.LogWarning("[BoardStateManager] EntityCacheManager reference missing");
                return false;
            }

            if (_spritePositioning == null)
            {
                Debug.LogWarning("[BoardStateManager] SpritePositioning reference missing");
                return false;
            }

            if (_boardStateEvaluator == null)
            {
                Debug.LogWarning("[BoardStateManager] BoardStateEvaluator not initialized");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Refreshes the entity cache to ensure it's up to date
        /// </summary>
        public void RefreshEntityCache()
        {
            if (_entityCacheManager != null)
            {
                _entityCacheManager.BuildEntityManagerCache();
                _entityCacheManager.RefreshEntityCaches();
            }
            else
            {
                Debug.LogWarning("[BoardStateManager] Cannot refresh entity cache - EntityCacheManager is null");
            }
        }

        /// <summary>
        /// Gets an entity at the specified world position
        /// </summary>
        /// <param name="position">World position to check</param>
        /// <param name="maxDistance">Maximum distance for entity detection</param>
        /// <returns>The EntityManager at position or null if none found</returns>
        public EntityManager GetEntityAtPosition(Vector3 position, float maxDistance = 1.0f)
        {
            if (_entityCacheManager?.EntityManagerCache == null)
            {
                Debug.LogWarning("[BoardStateManager] EntityManagerCache is null in GetEntityAtPosition");
                return null;
            }

            // Find closest entity within max distance
            EntityManager closestEntity = null;
            float closestDistance = maxDistance;

            foreach (var kvp in _entityCacheManager.EntityManagerCache)
            {
                if (kvp.Key == null) continue;

                float distance = Vector3.Distance(kvp.Key.transform.position, position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEntity = kvp.Value;
                }
            }

            return closestEntity;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_combatManager != null)
            {
                _combatManager.UnsubscribeFromPhaseChanges(OnPhaseChanged);
            }

            // Only clear the static reference if this instance is being destroyed
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
