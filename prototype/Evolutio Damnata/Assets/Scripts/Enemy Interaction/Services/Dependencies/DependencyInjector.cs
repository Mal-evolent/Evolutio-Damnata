using System;
using System.Reflection;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Managers;
using EnemyInteraction.Services.Interfaces;
using EnemyInteraction.Evaluation;
using UnityEngine;

namespace EnemyInteraction.Services
{
    /// <summary>
    /// Dependency Injector - Handles injecting dependencies into services
    /// </summary>
    public class DependencyInjector
    {
        private readonly ServiceLocator _serviceLocator;
        private readonly ISceneDependencyManager _sceneDependencyManager;

        public DependencyInjector(ServiceLocator serviceLocator, ISceneDependencyManager sceneDependencyManager)
        {
            _serviceLocator = serviceLocator;
            _sceneDependencyManager = sceneDependencyManager;
        }

        public void InjectDependenciesIntoServices()
        {
            Debug.Log("[DependencyInjector] Injecting dependencies into all services...");

            try
            {
                // Get each registered service and inject its dependencies based on type
                foreach (var service in _serviceLocator.GetAllRegisteredServices())
                {
                    if (service is AttackManager attackManager)
                    {
                        InjectAttackManagerDependencies(attackManager);
                    }
                    else if (service is BoardStateManager boardStateManager)
                    {
                        InjectBoardStateManagerDependencies(boardStateManager);
                    }
                    else if (service is EntityCacheManager entityCacheManager)
                    {
                        InjectEntityCacheManagerDependencies(entityCacheManager);
                    }
                    else if (service is CardPlayManager cardPlayManager)
                    {
                        InjectCardPlayManagerDependencies(cardPlayManager);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DependencyInjector] Error during dependency injection: {e.Message}\n{e.StackTrace}");
            }
        }

        public void InjectAttackManagerDependencies(AttackManager attackManager)
        {
            Debug.Log("[DependencyInjector] Injecting dependencies into AttackManager");

            // Get necessary services
            var keywordEvaluator = _serviceLocator.GetService<IKeywordEvaluator>();
            var boardStateManager = _serviceLocator.GetService<IBoardStateManager>();
            var entityCacheManager = _serviceLocator.GetService<IEntityCacheManager>();

            // Inject scene dependencies first
            SetPrivateField(attackManager, "_combatManager", _sceneDependencyManager.CombatManager);
            SetPrivateField(attackManager, "_combatStage", _sceneDependencyManager.CombatStage);
            SetPrivateField(attackManager, "_spritePositioning", _sceneDependencyManager.SpritePositioning);

            // Then inject service dependencies
            attackManager.InjectDependencies(
                keywordEvaluator,
                boardStateManager,
                entityCacheManager
            );
        }

        public void InjectBoardStateManagerDependencies(BoardStateManager boardStateManager)
        {
            Debug.Log("[DependencyInjector] Injecting dependencies into BoardStateManager");

            // Get necessary services
            var entityCacheManager = _serviceLocator.GetService<EntityCacheManager>();

            // Inject scene dependencies
            SetPrivateField(boardStateManager, "_combatManager", _sceneDependencyManager.CombatManager);
            SetPrivateField(boardStateManager, "_spritePositioning", _sceneDependencyManager.SpritePositioning);

            // Inject service dependencies
            if (entityCacheManager != null)
            {
                SetPrivateField(boardStateManager, "_entityCacheManager", entityCacheManager);
            }
        }

        public void InjectEntityCacheManagerDependencies(EntityCacheManager entityCacheManager)
        {
            Debug.Log("[DependencyInjector] Injecting dependencies into EntityCacheManager");

            // Initialize with scene dependencies
            if (_sceneDependencyManager.SpritePositioning != null && _sceneDependencyManager.AttackLimiter != null)
            {
                entityCacheManager.Initialize(_sceneDependencyManager.SpritePositioning, _sceneDependencyManager.AttackLimiter);
            }
        }

        public void InjectCardPlayManagerDependencies(CardPlayManager cardPlayManager)
        {
            Debug.Log("[DependencyInjector] Injecting dependencies into CardPlayManager");

            if (cardPlayManager == null)
            {
                Debug.LogWarning("[DependencyInjector] CardPlayManager is null, cannot inject dependencies");
                return;
            }

            try
            {
                // Get the DependencyProvider from CardPlayManager
                var dependencyProviderField = cardPlayManager.GetType().GetField("_dependencyProvider",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (dependencyProviderField != null)
                {
                    var dependencyProvider = dependencyProviderField.GetValue(cardPlayManager) as IDependencyProvider;

                    if (dependencyProvider != null)
                    {
                        // Register scene dependencies
                        dependencyProvider.RegisterService(_sceneDependencyManager.CombatManager);
                        dependencyProvider.RegisterService(_sceneDependencyManager.CombatStage);

                        if (_sceneDependencyManager.SpritePositioning != null)
                        {
                            dependencyProvider.RegisterService(_sceneDependencyManager.SpritePositioning);
                        }

                        // Register services
                        var keywordEvaluator = _serviceLocator.GetService<IKeywordEvaluator>();
                        var effectEvaluator = _serviceLocator.GetService<IEffectEvaluator>();
                        var boardStateManager = _serviceLocator.GetService<IBoardStateManager>();

                        if (keywordEvaluator != null)
                            dependencyProvider.RegisterService(keywordEvaluator);

                        if (effectEvaluator != null)
                            dependencyProvider.RegisterService(effectEvaluator);

                        if (boardStateManager != null)
                            dependencyProvider.RegisterService(boardStateManager);

                        Debug.Log("[DependencyInjector] Successfully registered services with CardPlayManager's DependencyProvider");
                    }
                    else
                    {
                        Debug.LogWarning("[DependencyInjector] Couldn't get DependencyProvider from CardPlayManager");
                    }
                }
                else
                {
                    Debug.LogWarning("[DependencyInjector] Couldn't find _dependencyProvider field in CardPlayManager");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DependencyInjector] Error injecting dependencies into CardPlayManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to set a private field using reflection.
        /// </summary>
        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;

            var field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[DependencyInjector] Field '{fieldName}' not found in {target.GetType().Name}");
            }
        }
    }
}
