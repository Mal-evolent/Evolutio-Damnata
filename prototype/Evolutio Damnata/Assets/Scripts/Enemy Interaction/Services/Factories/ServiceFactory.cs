using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using UnityEngine;

namespace EnemyInteraction.Services
{
    /// <summary>
    /// Factory for creating AI services - Follows the factory pattern
    /// </summary>
    public class ServiceFactory
    {
        public EntityCacheManager CreateEntityCacheManager(GameObject parent)
        {
            // First check if there's an existing EntityCacheManager instance
            EntityCacheManager existingManager = Object.FindObjectOfType<EntityCacheManager>();
            if (existingManager != null)
            {
                Debug.Log($"[ServiceFactory] Found existing EntityCacheManager");
                return existingManager;
            }

            return CreateService<EntityCacheManager>("EntityCacheManager", parent);
        }

        public KeywordEvaluator CreateKeywordEvaluator(GameObject parent)
        {
            // First check if there's an existing KeywordEvaluator instance
            KeywordEvaluator existingEvaluator = Object.FindObjectOfType<KeywordEvaluator>();
            if (existingEvaluator != null)
            {
                Debug.Log($"[ServiceFactory] Found existing KeywordEvaluator");
                return existingEvaluator;
            }

            return CreateService<KeywordEvaluator>("KeywordEvaluator", parent);
        }

        public EffectEvaluator CreateEffectEvaluator(GameObject parent)
        {
            // First check if there's an existing EffectEvaluator instance
            EffectEvaluator existingEvaluator = Object.FindObjectOfType<EffectEvaluator>();
            if (existingEvaluator != null)
            {
                Debug.Log($"[ServiceFactory] Found existing EffectEvaluator");
                return existingEvaluator;
            }

            return CreateService<EffectEvaluator>("EffectEvaluator", parent);
        }

        public BoardStateManager CreateBoardStateManager(GameObject parent)
        {
            // Check for existing instance via the singleton pattern first
            BoardStateManager existingManager = BoardStateManager.Instance;
            if (existingManager != null)
            {
                Debug.Log($"[ServiceFactory] Found existing BoardStateManager singleton instance");
                return existingManager;
            }

            // Then check for any instance in the scene as a fallback
            existingManager = Object.FindObjectOfType<BoardStateManager>();
            if (existingManager != null)
            {
                Debug.Log($"[ServiceFactory] Found existing BoardStateManager in scene");
                return existingManager;
            }

            return CreateService<BoardStateManager>("BoardStateManager", parent);
        }

        public CardPlayManager CreateCardPlayManager(GameObject parent)
        {
            // First check if there's an existing CardPlayManager instance
            CardPlayManager existingManager = Object.FindObjectOfType<CardPlayManager>();
            if (existingManager != null)
            {
                Debug.Log($"[ServiceFactory] Found existing CardPlayManager");
                return existingManager;
            }

            return CreateService<CardPlayManager>("CardPlayManager", parent);
        }

        public AttackManager CreateAttackManager(GameObject parent)
        {
            // Check for existing instance via the singleton pattern first
            AttackManager existingManager = AttackManager.Instance;
            if (existingManager != null)
            {
                Debug.Log($"[ServiceFactory] Found existing AttackManager singleton instance");
                return existingManager;
            }

            // Then check for any instance in the scene as a fallback
            existingManager = Object.FindObjectOfType<AttackManager>();
            if (existingManager != null)
            {
                Debug.Log($"[ServiceFactory] Found existing AttackManager in scene");
                return existingManager;
            }

            return CreateService<AttackManager>("AttackManager", parent);
        }

        private T CreateService<T>(string name, GameObject parent) where T : Component
        {
            GameObject serviceObj = new GameObject(name);
            // Do NOT parent this object; it must be root for DontDestroyOnLoad to work
            // serviceObj.transform.SetParent(parent.transform);

            T service = serviceObj.AddComponent<T>();
            Debug.Log($"[ServiceFactory] Created {name}");
            return service;
        }
    }
}
