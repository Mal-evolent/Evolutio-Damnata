// Assets/Scripts/Enemy Interaction/Managers/Board State Manager/BoardStateManager.cs
using UnityEngine;
using System.Collections;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;
using System.Collections.Generic;

namespace EnemyInteraction.Managers
{
    public class BoardStateManager : MonoBehaviour, IBoardStateManager
    {
        // Singleton instance
        public static BoardStateManager Instance { get; private set; }

        [Header("Core Settings")]
        [SerializeField] private SpritePositioning _spritePositioning;
        [SerializeField] private BoardStateSettings _settings;

        private ICombatManager _combatManager;
        private IEntityCacheManager _entityCacheManager;
        private BoardStateEvaluator _boardStateEvaluator;
        private bool _isInitialized;

        // Properties to expose to other managers
        public bool IsInitialized => _isInitialized;
        public Dictionary<GameObject, EntityManager> EntityCache => _entityCacheManager?.EntityManagerCache;

        private void Awake()
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

            // Get dependencies
            _combatManager = FindObjectOfType<CombatManager>();
            while (_combatManager == null)
            {
                yield return null;
                _combatManager = FindObjectOfType<CombatManager>();
            }

            var combatStage = FindObjectOfType<CombatStage>();
            while (combatStage == null)
            {
                yield return null;
                combatStage = FindObjectOfType<CombatStage>();
            }

            while (combatStage.SpritePositioning == null)
                yield return null;

            _spritePositioning ??= combatStage.SpritePositioning as SpritePositioning;

            // Get or create the EntityCacheManager singleton instance
            _entityCacheManager = EntityCacheManager.Instance;
            if (_entityCacheManager == null)
            {
                Debug.LogWarning("[BoardStateManager] EntityCacheManager singleton not found, creating one...");
                var cacheManagerObj = new GameObject("EntityCacheManager");
                _entityCacheManager = cacheManagerObj.AddComponent<EntityCacheManager>();

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
                    var attackLimiter = combatStage.GetAttackLimiter() ?? new AttackLimiter();
                    (_entityCacheManager as EntityCacheManager).Initialize(_spritePositioning, attackLimiter);
                }
                else
                {
                    // Just refresh caches to ensure they're up-to-date
                    _entityCacheManager.BuildEntityManagerCache();
                    _entityCacheManager.RefreshEntityCaches();
                }
            }

            // Initialize our evaluator component
            _boardStateEvaluator = new BoardStateEvaluator(_combatManager, _settings);

            _isInitialized = true;
            Debug.Log("[BoardStateManager] Initialization complete");
        }

        public BoardState EvaluateBoardState()
        {
            if (!_isInitialized || _spritePositioning == null || _combatManager == null || _entityCacheManager == null)
            {
                Debug.LogError("[BoardStateManager] Evaluation failed - dependencies missing");
                return null;
            }

            // Refresh entity cache before evaluating
            _entityCacheManager.RefreshEntityCaches();

            // Initialize the state variable here
            var state = new BoardState
            {
                EnemyMonsters = _entityCacheManager.CachedEnemyEntities,
                PlayerMonsters = _entityCacheManager.CachedPlayerEntities,
                EnemyHealth = _combatManager.EnemyHealth,
                PlayerHealth = _combatManager.PlayerHealth,
                EnemyMaxHealth = _combatManager.MaxHealth,
                PlayerMaxHealth = _combatManager.MaxHealth,
                TurnCount = _combatManager.TurnCount,
                EnemyMana = _combatManager.EnemyMana,
                CurrentPhase = _combatManager.CurrentPhase,
                IsPlayerTurn = _combatManager.PlayerTurn,
                IsNextTurnPlayerFirst = !_combatManager.PlayerGoesFirst
            };

            // Try to get hand sizes if available on the combat manager
            try
            {
                state.enemyHandSize = _combatManager.EnemyHandSize;
                state.playerHandSize = _combatManager.PlayerHandSize;
                state.CardAdvantage = state.enemyHandSize - state.playerHandSize;
                Debug.Log($"[BoardStateManager] Card advantage: {state.CardAdvantage} (Enemy: {state.enemyHandSize}, Player: {state.playerHandSize})");
            }
            catch
            {
                Debug.LogWarning("[BoardStateManager] Could not access hand sizes - consider adding this property to ICombatManager");
            }

            // Calculate board control with improved logic
            state.EnemyBoardControl = _boardStateEvaluator.CalculateBoardControl(state.EnemyMonsters, true);
            state.PlayerBoardControl = _boardStateEvaluator.CalculateBoardControl(state.PlayerMonsters, false);

            // Apply various factors
            _boardStateEvaluator.ApplyBoardPositioningFactors(state);
            _boardStateEvaluator.ApplyResourceAdvantages(state);
            _boardStateEvaluator.ApplyHealthBasedFactors(state);
            _boardStateEvaluator.ApplyTurnOrderInfluence(state);

            // Calculate derived metrics
            state.BoardControlDifference = state.EnemyBoardControl - state.PlayerBoardControl;
            state.HealthAdvantage = state.EnemyHealth - state.PlayerHealth;
            state.HealthRatio = state.EnemyHealth / (float)state.EnemyMaxHealth;

            return state;
        }

        // Method to refresh the cache when new entities are added
        public void RefreshEntityCache()
        {
            if (_entityCacheManager != null)
            {
                _entityCacheManager.BuildEntityManagerCache();
                _entityCacheManager.RefreshEntityCaches();
            }
        }

        // Public method to get entity at position
        public EntityManager GetEntityAtPosition(Vector3 position, float maxDistance = 1.0f)
        {
            if (_entityCacheManager?.EntityManagerCache == null)
                return null;

            foreach (var kvp in _entityCacheManager.EntityManagerCache)
            {
                if (kvp.Key != null && Vector3.Distance(kvp.Key.transform.position, position) < maxDistance)
                {
                    return kvp.Value;
                }
            }

            return null;
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
}
