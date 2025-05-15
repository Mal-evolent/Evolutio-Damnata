using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers.Evaluation;
using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Managers.Execution;
using EnemyInteraction.Services;
using EnemyInteraction.Models;
using EnemyInteraction.Interfaces;
using System.Linq;

namespace EnemyInteraction.Managers
{
    public interface ICardPlayInitializer
    {
        IEnumerator Initialize();
        IEnumerator ReacquireSceneReferences();
        Dictionary<GameObject, EntityManager> GetEntityCache();
    }

    public class CardPlayInitializer : ICardPlayInitializer
    {
        private readonly IDependencyProvider _dependencies;
        private readonly CardPlaySettings _settings;
        private Dictionary<GameObject, EntityManager> _entityCache;
        
        public CardPlayInitializer(IDependencyProvider dependencies, CardPlaySettings settings)
        {
            _dependencies = dependencies;
            _settings = settings;
            _entityCache = new Dictionary<GameObject, EntityManager>();
        }
        
        public Dictionary<GameObject, EntityManager> GetEntityCache()
        {
            return _entityCache;
        }
        
        public IEnumerator Initialize()
        {
            Debug.Log("[CardPlayInitializer] Initializing...");
            
            yield return InitializeCriticalComponents();
            yield return InitializeOptionalServices();
            
            InitializeCardPlayComponents();
            BuildEntityCache();
            
            Debug.Log("[CardPlayInitializer] Initialization complete");
        }
        
        public IEnumerator ReacquireSceneReferences()
        {
            yield return new WaitForSeconds(0.5f); // Wait for scene to stabilize

            Debug.Log("[CardPlayInitializer] Reacquiring scene references...");

            // Clear references that might be stale
            _dependencies.RegisterService<ICombatManager>(null);
            _dependencies.RegisterService<CombatStage>(null);
            _dependencies.RegisterService<SpritePositioning>(null);
            _dependencies.RegisterService<ISpellEffectApplier>(null);

            // Clear entity cache
            _entityCache.Clear();

            // Reacquire references
            yield return InitializeCriticalComponents();
            yield return InitializeOptionalServices();

            // Rebuild components with fresh references
            InitializeCardPlayComponents();
            BuildEntityCache();

            Debug.Log("[CardPlayInitializer] Scene references reacquired");
        }
        
        private IEnumerator InitializeCriticalComponents()
        {
            int attempts = 0;

            while (attempts < _settings.MaxInitializationAttempts)
            {
                var combatManager = _dependencies.GetService<ICombatManager>() ?? Object.FindObjectOfType<CombatManager>();
                var combatStage = _dependencies.GetService<CombatStage>() ?? Object.FindObjectOfType<CombatStage>();
                
                if (combatManager != null) _dependencies.RegisterService(combatManager);
                if (combatStage != null) _dependencies.RegisterService(combatStage);

                if (combatManager != null && combatStage != null) break;

                yield return new WaitForSeconds(_settings.InitializationRetryDelay);
                attempts++;
            }

            var registeredCombatManager = _dependencies.GetService<ICombatManager>();
            var registeredCombatStage = _dependencies.GetService<CombatStage>();

            // Log error if components weren't found after max attempts
            if (registeredCombatManager == null)
            {
                Debug.LogError("[CardPlayInitializer] Failed to initialize CombatManager after maximum attempts");
            }

            if (registeredCombatStage == null)
            {
                Debug.LogError("[CardPlayInitializer] Failed to initialize CombatStage after maximum attempts");
            }
            else
            {
                yield return InitializeCombatStageDependencies(registeredCombatStage);
            }
        }

        private IEnumerator InitializeCombatStageDependencies(CombatStage combatStage)
        {
            int attempts = 0;

            while ((combatStage.SpritePositioning == null || combatStage.SpellEffectApplier == null) &&
                   attempts < _settings.MaxInitializationAttempts)
            {
                yield return new WaitForSeconds(_settings.InitializationRetryDelay);
                attempts++;
            }

            var spritePositioning = _dependencies.GetService<SpritePositioning>() ?? combatStage.SpritePositioning as SpritePositioning;
            var spellEffectApplier = _dependencies.GetService<ISpellEffectApplier>() ?? combatStage.SpellEffectApplier;
            
            if (spritePositioning != null) _dependencies.RegisterService(spritePositioning);
            if (spellEffectApplier != null) _dependencies.RegisterService(spellEffectApplier);
        }

        private IEnumerator InitializeOptionalServices()
        {
            yield return InitializeAIServices();
            InitializeFallbackServices();
        }

        private IEnumerator InitializeAIServices()
        {
            int attempts = 0;

            while (AIServices.Instance == null && attempts < _settings.MaxInitializationAttempts)
            {
                yield return new WaitForSeconds(_settings.InitializationRetryDelay);
                attempts++;
            }

            if (AIServices.Instance != null)
            {
                var services = AIServices.Instance;
                
                var keywordEvaluator = _dependencies.GetService<IKeywordEvaluator>() ?? services.KeywordEvaluator;
                var effectEvaluator = _dependencies.GetService<IEffectEvaluator>() ?? services.EffectEvaluator;
                var boardStateManager = _dependencies.GetService<IBoardStateManager>() ?? services.BoardStateManager;
                
                if (keywordEvaluator != null) _dependencies.RegisterService(keywordEvaluator);
                if (effectEvaluator != null) _dependencies.RegisterService(effectEvaluator);
                if (boardStateManager != null) _dependencies.RegisterService(boardStateManager);
            }
        }

        private void InitializeFallbackServices()
        {
            // First try to find the BoardStateManager singleton
            BoardStateManager existingManager = null;
            try
            {
                existingManager = BoardStateManager.Instance;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CardPlayInitializer] Error accessing BoardStateManager.Instance: {ex.Message}");
            }

            // Use the singleton if found
            if (existingManager != null)
            {
                _dependencies.RegisterService<IBoardStateManager>(existingManager);
            }

            // Only create local services if absolutely necessary
            if (!_dependencies.HasService<IKeywordEvaluator>())
            {
                var keywordEvaluator = CreateLocalService<KeywordEvaluator>("KeywordEvaluator_Local");
                _dependencies.RegisterService<IKeywordEvaluator>(keywordEvaluator);
            }
            
            if (!_dependencies.HasService<IEffectEvaluator>())
            {
                var effectEvaluator = CreateLocalService<EffectEvaluator>("EffectEvaluator_Local");
                _dependencies.RegisterService<IEffectEvaluator>(effectEvaluator);
            }

            // Only create a local BoardStateManager if no singleton exists
            if (!_dependencies.HasService<IBoardStateManager>())
            {
                Debug.LogWarning("[CardPlayInitializer] No BoardStateManager singleton found, creating local instance");
                var boardStateManager = CreateLocalService<BoardStateManager>("BoardStateManager_Local");
                _dependencies.RegisterService<IBoardStateManager>(boardStateManager);
            }
        }

        private void InitializeCardPlayComponents()
        {
            var combatManager = _dependencies.GetService<ICombatManager>();
            var keywordEvaluator = _dependencies.GetService<IKeywordEvaluator>();
            var effectEvaluator = _dependencies.GetService<IEffectEvaluator>();
            var combatStage = _dependencies.GetService<CombatStage>();
            var spritePositioning = _dependencies.GetService<SpritePositioning>();
            var spellEffectApplier = _dependencies.GetService<ISpellEffectApplier>();
            
            // Create specialized components if they don't exist already
            if (!_dependencies.HasService<ICardEvaluator>())
            {
                var cardEvaluator = new CardEvaluator(
                    combatManager,
                    keywordEvaluator,
                    effectEvaluator,
                    _settings.SuboptimalPlayChance,
                    _settings.EvaluationVariance);
                _dependencies.RegisterService<ICardEvaluator>(cardEvaluator);
            }

            if (!_dependencies.HasService<MonsterPositionSelector>())
            {
                var monsterPositionSelector = new MonsterPositionSelector(
                    spritePositioning,
                    _entityCache);
                _dependencies.RegisterService<MonsterPositionSelector>(monsterPositionSelector);
            }

            if (!_dependencies.HasService<SpellTargetSelector>())
            {
                var spellTargetSelector = new SpellTargetSelector(
                    spritePositioning,
                    _entityCache);
                _dependencies.RegisterService<SpellTargetSelector>(spellTargetSelector);
            }

            if (!_dependencies.HasService<ICardPlayExecutor>())
            {
                var monsterPositionSelector = _dependencies.GetService<MonsterPositionSelector>();
                var spellTargetSelector = _dependencies.GetService<SpellTargetSelector>();
                
                var cardPlayExecutor = new CardPlayExecutor(
                    combatManager,
                    combatStage,
                    spellEffectApplier,
                    monsterPositionSelector,
                    spellTargetSelector,
                    _settings.ActionDelay);
                _dependencies.RegisterService<ICardPlayExecutor>(cardPlayExecutor);
            }
        }

        private T CreateLocalService<T>(string name) where T : Component
        {
            var context = Object.FindObjectOfType<CardPlayManager>();
            if (context == null)
            {
                Debug.LogError($"[CardPlayInitializer] Cannot create local service {name} - CardPlayManager not found");
                return null;
            }
            
            var obj = new GameObject(name);
            obj.transform.SetParent(context.transform);
            return obj.AddComponent<T>();
        }

        private void BuildEntityCache()
        {
            _entityCache.Clear();
            var spritePositioning = _dependencies.GetService<SpritePositioning>();
            if (spritePositioning == null) return;

            foreach (var entity in spritePositioning.EnemyEntities.Concat(spritePositioning.PlayerEntities))
            {
                if (entity != null && !_entityCache.ContainsKey(entity))
                {
                    _entityCache[entity] = entity.GetComponent<EntityManager>();
                }
            }
        }
    }
}
