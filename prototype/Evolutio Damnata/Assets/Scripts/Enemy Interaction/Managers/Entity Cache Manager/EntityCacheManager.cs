using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Utilities;

namespace EnemyInteraction.Managers
{
    public class EntityCacheManager : MonoBehaviour, IEntityCacheManager
    {
        private SpritePositioning _spritePositioning;
        private AttackLimiter _attackLimiter;

        private Dictionary<GameObject, EntityManager> _entityManagerCache;
        private List<EntityManager> _cachedPlayerEntities;
        private List<EntityManager> _cachedEnemyEntities;

        public Dictionary<GameObject, EntityManager> EntityManagerCache => _entityManagerCache;
        public List<EntityManager> CachedPlayerEntities => _cachedPlayerEntities;
        public List<EntityManager> CachedEnemyEntities => _cachedEnemyEntities;
        public static EntityCacheManager Instance { get; private set; }

        public void Initialize(SpritePositioning spritePositioning, AttackLimiter attackLimiter)
        {
            _spritePositioning = spritePositioning;
            _attackLimiter = attackLimiter;
            _entityManagerCache = new Dictionary<GameObject, EntityManager>();

            BuildEntityManagerCache();
            RefreshEntityCaches();
        }

        public void BuildEntityManagerCache()
        {
            _entityManagerCache.Clear();
            if (_spritePositioning == null) return;

            foreach (var entity in _spritePositioning.EnemyEntities.Concat(_spritePositioning.PlayerEntities))
            {
                if (entity != null && !_entityManagerCache.ContainsKey(entity))
                {
                    _entityManagerCache[entity] = entity.GetComponent<EntityManager>();
                }
            }
        }

        public void RefreshEntityCaches()
        {
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
            Debug.Log("[EntityCacheManager] Forced refresh after monster placement");
            BuildEntityManagerCache();
            RefreshEntityCaches();
        }

        public List<EntityManager> GetValidEntities(IEnumerable<GameObject> source, bool checkAttackLimiter)
        {
            if (source == null) return new List<EntityManager>();

            return source
                .Where(e => e != null && _entityManagerCache.ContainsKey(e))
                .Select(e => _entityManagerCache[e])
                .Where(em => em != null && em.placed && !em.dead && !em.IsFadingOut &&
                      (!checkAttackLimiter || (_attackLimiter?.CanAttack(em) ?? !em.HasAttacked)))
                .ToList();
        }

        public bool HasEntitiesOnField(bool isPlayerSide)
        {
            if (_spritePositioning == null)
                return false;

            var entities = isPlayerSide ? _spritePositioning.PlayerEntities : _spritePositioning.EnemyEntities;

            foreach (var entity in entities)
            {
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
                Debug.LogWarning("Found duplicate EntityCacheManager, destroying this instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
