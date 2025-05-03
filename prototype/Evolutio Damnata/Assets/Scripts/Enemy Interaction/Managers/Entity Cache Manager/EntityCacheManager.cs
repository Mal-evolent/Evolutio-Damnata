using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Utilities;
using UnityEngine.SceneManagement;

namespace EnemyInteraction.Managers
{
    public class EntityCacheManager : MonoBehaviour, IEntityCacheManager
    {
        private SpritePositioning _spritePositioning;
        private AttackLimiter _attackLimiter;

        private Dictionary<GameObject, EntityManager> _entityManagerCache;
        private List<EntityManager> _cachedPlayerEntities;
        private List<EntityManager> _cachedEnemyEntities;

        // Track the current scene to detect scene changes
        private int _currentSceneIndex = -1;

        public Dictionary<GameObject, EntityManager> EntityManagerCache => _entityManagerCache ?? (_entityManagerCache = new Dictionary<GameObject, EntityManager>());
        public List<EntityManager> CachedPlayerEntities => _cachedPlayerEntities ?? (_cachedPlayerEntities = new List<EntityManager>());
        public List<EntityManager> CachedEnemyEntities => _cachedEnemyEntities ?? (_cachedEnemyEntities = new List<EntityManager>());
        public static EntityCacheManager Instance { get; private set; }

        /// <summary>
        /// Initialize with new references after scene changes or resets
        /// </summary>
        public void ReinitializeWithNewReferences(SpritePositioning spritePositioning, AttackLimiter attackLimiter)
        {
            Debug.Log("[EntityCacheManager] Reinitializing with new references");
            ClearStaleReferences();
            Initialize(spritePositioning, attackLimiter);
        }

        public void Initialize(SpritePositioning spritePositioning, AttackLimiter attackLimiter)
        {
            if (spritePositioning == null)
            {
                Debug.LogError("[EntityCacheManager] Cannot initialize with null SpritePositioning!");
                return;
            }

            _spritePositioning = spritePositioning;
            _attackLimiter = attackLimiter;
            _entityManagerCache = new Dictionary<GameObject, EntityManager>();

            // Track the current scene
            _currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Now that we have validated our dependencies, we can build the cache
            BuildEntityManagerCache();
            RefreshEntityCaches();
        }

        /// <summary>
        /// Clear stale references after scene reload
        /// </summary>
        public void ClearStaleReferences()
        {
            Debug.Log("[EntityCacheManager] Clearing stale references");
            _spritePositioning = null;
            _attackLimiter = null;

            if (_entityManagerCache != null)
                _entityManagerCache.Clear();

            if (_cachedPlayerEntities != null)
                _cachedPlayerEntities.Clear();

            if (_cachedEnemyEntities != null)
                _cachedEnemyEntities.Clear();
        }

        public void BuildEntityManagerCache()
        {
            if (_entityManagerCache == null)
            {
                _entityManagerCache = new Dictionary<GameObject, EntityManager>();
            }
            else
            {
                _entityManagerCache.Clear();
            }

            // Early exit if _spritePositioning is null or not properly initialized
            if (_spritePositioning == null)
            {
                Debug.LogWarning("[EntityCacheManager] Cannot build entity manager cache - SpritePositioning is null.");
                return;
            }

            // Check if entity lists are initialized to prevent NullReferenceException
            var enemyEntities = _spritePositioning.EnemyEntities;
            var playerEntities = _spritePositioning.PlayerEntities;

            if (enemyEntities == null || playerEntities == null)
            {
                Debug.LogWarning("[EntityCacheManager] Cannot build entity manager cache - Entity lists are null in SpritePositioning.");
                return;
            }

            // Now safely concatenate the lists and process entities
            try
            {
                foreach (var entity in enemyEntities.Concat(playerEntities))
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
                Debug.Log($"[EntityCacheManager] Successfully built entity cache with {_entityManagerCache.Count} entities");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EntityCacheManager] Error building entity cache: {e.Message}");
            }
        }

        public void RefreshEntityCaches()
        {
            // Initialize lists if they don't exist
            if (_cachedEnemyEntities == null)
            {
                _cachedEnemyEntities = new List<EntityManager>();
            }
            else
            {
                _cachedEnemyEntities.Clear();
            }

            if (_cachedPlayerEntities == null)
            {
                _cachedPlayerEntities = new List<EntityManager>();
            }
            else
            {
                _cachedPlayerEntities.Clear();
            }

            // Take a local snapshot of the entity lists to prevent race conditions
            var enemyEntitiesList = _spritePositioning?.EnemyEntities;
            var playerEntitiesList = _spritePositioning?.PlayerEntities;

            if (enemyEntitiesList != null && playerEntitiesList != null)
            {
                _cachedEnemyEntities = GetValidEntities(enemyEntitiesList, true);
                _cachedPlayerEntities = GetValidEntities(playerEntitiesList, false);

                // Log entities found
                Debug.Log($"[EntityCacheManager] Refreshed entity caches - Found {_cachedEnemyEntities.Count} enemy entities and {_cachedPlayerEntities.Count} player entities");
            }
            else
            {
                Debug.LogWarning("[EntityCacheManager] Could not refresh entity caches - sprite positioning references are null");
            }
        }

        public void RefreshAfterAction()
        {
            Debug.Log("[EntityCacheManager] Forced refresh after action");
            BuildEntityManagerCache();
            RefreshEntityCaches();
        }

        public List<EntityManager> GetValidEntities(IEnumerable<GameObject> source, bool checkAttackLimiter)
        {
            if (source == null) return new List<EntityManager>();
            if (_entityManagerCache == null) return new List<EntityManager>();

            try
            {
                return source
                    .Where(e => e != null && _entityManagerCache.ContainsKey(e))
                    .Select(e => _entityManagerCache[e])
                    .Where(em => em != null && em.placed && !em.dead && !em.IsFadingOut &&
                          (!checkAttackLimiter || _attackLimiter == null || _attackLimiter.CanAttack(em)))
                    .ToList();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EntityCacheManager] Error in GetValidEntities: {e.Message}");
                return new List<EntityManager>();
            }
        }

        public bool HasEntitiesOnField(bool isPlayerSide)
        {
            if (_spritePositioning == null || _entityManagerCache == null)
                return false;

            var entities = isPlayerSide ? _spritePositioning.PlayerEntities : _spritePositioning.EnemyEntities;
            if (entities == null)
                return false;

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                if (!_entityManagerCache.TryGetValue(entity, out var entityManager)) continue;

                if (entityManager != null && entityManager.placed && !entityManager.dead && !entityManager.IsFadingOut)
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[EntityCacheManager] Found duplicate EntityCacheManager, destroying this instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _entityManagerCache = new Dictionary<GameObject, EntityManager>();
            _cachedPlayerEntities = new List<EntityManager>();
            _cachedEnemyEntities = new List<EntityManager>();

            // Register for scene change events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // When a new scene is loaded, clear stale references
            if (scene.buildIndex != _currentSceneIndex)
            {
                Debug.Log($"[EntityCacheManager] Scene changed from {_currentSceneIndex} to {scene.buildIndex} - clearing references");
                ClearStaleReferences();
                _currentSceneIndex = scene.buildIndex;

                // Start a coroutine to find and inject new references
                StartCoroutine(FindAndInjectNewReferences());
            }
        }

        private IEnumerator FindAndInjectNewReferences()
        {
            // Wait a short time for other components to initialize
            yield return new WaitForSeconds(0.2f);

            var combatStage = FindObjectOfType<CombatStage>();
            if (combatStage != null)
            {
                // Wait for SpritePositioning to be available
                int attempts = 0;
                const int maxAttempts = 30;
                const float waitTime = 0.1f;

                while (combatStage.SpritePositioning == null && attempts < maxAttempts)
                {
                    yield return new WaitForSeconds(waitTime);
                    attempts++;
                }

                if (combatStage.SpritePositioning != null)
                {
                    var attackLimiter = combatStage.GetAttackLimiter();
                    var spritePositioning = combatStage.SpritePositioning as SpritePositioning;

                    if (spritePositioning != null)
                    {
                        Debug.Log("[EntityCacheManager] Found new SpritePositioning reference, reinitializing");
                        ReinitializeWithNewReferences(spritePositioning, attackLimiter);
                    }
                    else
                    {
                        Debug.LogWarning("[EntityCacheManager] SpritePositioning was found but could not be cast to SpritePositioning type");
                    }
                }
                else
                {
                    Debug.LogWarning("[EntityCacheManager] Failed to find SpritePositioning after maximum attempts");
                }
            }
            else
            {
                Debug.LogWarning("[EntityCacheManager] Could not find CombatStage in scene");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Clear the singleton reference
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
