using UnityEngine;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;
using System.Collections;
using System.Collections.Generic;

namespace EnemyInteraction.Managers
{
    public class BoardStateManager : MonoBehaviour, IBoardStateManager
    {
        [SerializeField] private SpritePositioning _spritePositioning;
        [SerializeField, Range(5, 20)] private int _lateGameTurnThreshold = 10;
        [SerializeField, Range(1f, 1.5f)] private float _lateGameBonusMultiplier = 1.1f;

        private ICombatManager _combatManager;
        private Dictionary<GameObject, EntityManager> _entityManagerCache; // Renamed from _entityCache
        private bool _isInitialized;
        private bool _entityCacheBuilt = false; // Added flag to track cache status

        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            _entityManagerCache = new Dictionary<GameObject, EntityManager>(); // Updated variable name
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

            BuildEntityCache();
            _isInitialized = true;
            Debug.Log("[BoardStateManager] Initialization complete");
        }

        private void BuildEntityCache()
        {
            _entityManagerCache.Clear();
            
            // Check if _spritePositioning or its properties are null
            if (_spritePositioning == null || _spritePositioning.PlayerEntities == null || _spritePositioning.EnemyEntities == null)
            {
                Debug.LogWarning("SpritePositioning or its entities are null during BuildEntityCache - will retry later");
                StartCoroutine(RetryBuildEntityCache());
                return;
            }

            // Cache all player and enemy entities
            foreach (var entity in _spritePositioning.EnemyEntities.Concat(_spritePositioning.PlayerEntities))
            {
                if (entity != null && !_entityManagerCache.ContainsKey(entity))
                {
                    var entityManager = entity.GetComponent<EntityManager>();
                    if (entityManager != null)
                    {
                        _entityManagerCache[entity] = entityManager;
                    }
                }
            }
            
            _entityCacheBuilt = true;
            Debug.Log("Entity cache successfully built with " + _entityManagerCache.Count + " entities");
        }

        private IEnumerator RetryBuildEntityCache()
        {
            int attempts = 0;
            const int maxAttempts = 10;
            const float retryDelay = 0.2f;

            while (attempts < maxAttempts && 
                  (_spritePositioning == null || 
                   _spritePositioning.PlayerEntities == null || 
                   _spritePositioning.EnemyEntities == null))
            {
                yield return new WaitForSeconds(retryDelay);
                attempts++;
                Debug.Log($"Retry {attempts}/{maxAttempts} building entity cache...");
            }

            if (_spritePositioning != null && 
                _spritePositioning.PlayerEntities != null && 
                _spritePositioning.EnemyEntities != null)
            {
                BuildEntityCache();
            }
            else
            {
                Debug.LogError("Failed to build entity cache after multiple attempts - entities may be missing");
            }
        }

        public BoardState EvaluateBoardState()
        {
            if (!_isInitialized || _spritePositioning == null || _combatManager == null)
            {
                Debug.LogError("[BoardStateManager] Evaluation failed - dependencies missing");
                return null;
            }

            // Ensure cache is built before evaluating
            if (!_entityCacheBuilt)
            {
                BuildEntityCache();
                if (!_entityCacheBuilt) {
                    Debug.LogWarning("[BoardStateManager] Entity cache not built during evaluation");
                }
            }

            var state = new BoardState
            {
                EnemyMonsters = GetValidEntities(_spritePositioning.EnemyEntities),
                PlayerMonsters = GetValidEntities(_spritePositioning.PlayerEntities),
                EnemyHealth = _combatManager.EnemyHealth,
                PlayerHealth = _combatManager.PlayerHealth,
                TurnCount = _combatManager.TurnCount,
                EnemyMana = _combatManager.EnemyMana
            };

            // Calculate board control
            state.EnemyBoardControl = CalculateBoardControl(state.EnemyMonsters);
            state.PlayerBoardControl = CalculateBoardControl(state.PlayerMonsters);

            // Late-game scaling
            if (_combatManager.TurnCount > _lateGameTurnThreshold)
                state.EnemyBoardControl *= _lateGameBonusMultiplier;

            // Health influence
            state.EnemyBoardControl += state.EnemyHealth * 0.2f;
            state.PlayerBoardControl += state.PlayerHealth * 0.2f;

            // Derived metrics
            state.BoardControlDifference = state.EnemyBoardControl - state.PlayerBoardControl;
            state.HealthAdvantage = state.EnemyHealth - state.PlayerHealth;
            state.HealthRatio = state.EnemyHealth / (float)_combatManager.MaxHealth;

            return state;
        }

        private List<EntityManager> GetValidEntities(IEnumerable<GameObject> entities)
        {
            return entities?
                .Where(e => e != null && _entityManagerCache.TryGetValue(e, out var em) &&
                       em != null && !em.dead && em.placed)
                .Select(e => _entityManagerCache[e])
                .ToList() ?? new List<EntityManager>();
        }

        private float CalculateBoardControl(List<EntityManager> entities)
        {
            if (entities == null) return 0f;

            return entities.Sum(e =>
            {
                if (e == null) return 0f;

                float value = e.GetAttack() + e.GetHealth();

                // Bonus for keywords
                if (e.HasKeyword(Keywords.MonsterKeyword.Taunt)) value *= 1.3f;
                if (e.HasKeyword(Keywords.MonsterKeyword.Ranged)) value *= 1.2f;

                // Account for ongoing effects
                var stackManager = StackManager.Instance;
                if (stackManager != null)
                {
                    // Reduce value for entities with burn (ongoing damage)
                    if (stackManager.HasEffect(e, SpellEffect.Burn))
                    {
                        var burnEffects = stackManager.GetEffectsOfType(e, SpellEffect.Burn);
                        foreach (var effect in burnEffects)
                        {
                            // Approximate effect on board control based on remaining burn damage
                            int remainingRounds = stackManager.GetRemainingDuration(effect);
                            int damagePerRound = effect.EffectValue;
                            value -= remainingRounds * damagePerRound * 0.8f;
                        }
                    }

                    // Add value for other positive effects
                    // This is just a placeholder for the general approach
                }

                return value;
            });
        }
    }
}