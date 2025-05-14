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

        // Components
        private BoardStateInitializer _initializer;
        private BoardStateCache _cache;
        private BoardStateDependencyValidator _dependencyValidator;

        // References shared across components
        private ICombatManager _combatManager;
        private IEntityCacheManager _entityCacheManager;
        private BoardStateEvaluator _boardStateEvaluator;

        // Event for board state updates
        public event Action<BoardState> OnBoardStateUpdated;

        /// <summary>
        /// Gets the board state settings
        /// </summary>
        public BoardStateSettings Settings => _settings;

        // Properties to expose to other managers
        public bool IsInitialized => _initializer?.IsInitialized ?? false;
        public Dictionary<GameObject, EntityManager> EntityCache => _entityCacheManager?.EntityManagerCache;

        private void Awake()
        {
            InitializeSingleton();
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void InitializeSingleton()
        {
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

            // Initialize components
            _initializer = new BoardStateInitializer(this, _initializationTimeout, _initializationCheckDelay);
            _cache = new BoardStateCache(_cacheTimeout);
            _dependencyValidator = new BoardStateDependencyValidator();

            // Start initialization process
            _initializer.Initialize();
        }

        // Called by the initializer when initialization is complete
        public void OnInitializationComplete(ICombatManager combatManager,
                                            IEntityCacheManager entityCacheManager,
                                            SpritePositioning spritePositioning,
                                            BoardStateEvaluator evaluator)
        {
            _combatManager = combatManager;
            _entityCacheManager = entityCacheManager;
            _spritePositioning = spritePositioning;
            _boardStateEvaluator = evaluator;

            Debug.Log("[BoardStateManager] Initialization complete");
        }

        private void OnEnable()
        {
            if (IsInitialized && _combatManager != null)
            {
                _combatManager.SubscribeToPhaseChanges(OnPhaseChanged);
            }
        }

        private void OnDisable()
        {
            if (_combatManager != null)
            {
                _combatManager.UnsubscribeFromPhaseChanges(OnPhaseChanged);
            }
        }

        private void OnPhaseChanged(CombatPhase newPhase)
        {
            RefreshEntityCache();
            _cache.Invalidate();
        }

        /// <summary>
        /// Gets the current board state, using cache if available and not expired
        /// </summary>
        public BoardState GetBoardState(bool forceRefresh = false)
        {
            if (forceRefresh || !_cache.IsValid(Time.time))
            {
                BoardState newState = EvaluateBoardState();
                if (newState != null)
                {
                    _cache.UpdateCache(newState, Time.time);
                    OnBoardStateUpdated?.Invoke(newState);
                }
            }

            return _cache.GetCachedState();
        }

        /// <summary>
        /// Evaluates and creates a new board state based on current game conditions
        /// </summary>
        public BoardState EvaluateBoardState()
        {
            if (!_dependencyValidator.ValidateDependencies(_initializer.IsInitialized,
                                                          _combatManager,
                                                          _entityCacheManager,
                                                          _spritePositioning,
                                                          _boardStateEvaluator))
            {
                return null;
            }

            _entityCacheManager.RefreshEntityCaches();

            try
            {
                BoardState state = CreateBoardStateWithDeckReferences();
                state.UpdateFromCombatManager(_combatManager);
                state.UpdateMonsters(
                    _entityCacheManager.CachedPlayerEntities,
                    _entityCacheManager.CachedEnemyEntities);
                state.UpdateBoardControlMetrics();

                _boardStateEvaluator.ApplyAllFactors(state);

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

        private void OnDestroy()
        {
            if (_combatManager != null)
            {
                _combatManager.UnsubscribeFromPhaseChanges(OnPhaseChanged);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
        // Add this method to BoardStateManager.cs
        /// <summary>
        /// Handles subscribing to combat phase changes
        /// </summary>
        public void SubscribeToCombatPhaseChanges(ICombatManager combatManager)
        {
            if (combatManager != null)
            {
                combatManager.SubscribeToPhaseChanges(OnPhaseChanged);
            }
        }
    }
}
