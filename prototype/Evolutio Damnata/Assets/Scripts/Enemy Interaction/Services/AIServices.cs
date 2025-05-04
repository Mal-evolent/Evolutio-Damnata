using UnityEngine;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using EnemyInteraction.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System;

namespace EnemyInteraction.Services
{
    /// <summary>
    /// Central service provider that manages all AI-related components and dependencies.
    /// Implements the singleton pattern to ensure only one instance exists.
    /// </summary>
    public class AIServices : MonoBehaviour
    {
        #region Singleton Implementation

        private static AIServices _instance;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;

        /// <summary>
        /// Accesses the singleton instance, creating it if necessary.
        /// </summary>
        public static AIServices Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // First try to find an existing instance
                            _instance = FindObjectOfType<AIServices>();

                            // If still null, create a new GameObject with AIServices
                            if (_instance == null)
                            {
                                GameObject aiServicesObj = new GameObject("AIServices");
                                DontDestroyOnLoad(aiServicesObj);
                                _instance = aiServicesObj.AddComponent<AIServices>();
                                Debug.Log("[AIServices] Created new AIServices instance");
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Indicates whether the AIServices is fully initialized and ready to provide dependencies.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        #endregion

        #region Service References

        [SerializeField] private KeywordEvaluator _keywordEvaluator;
        [SerializeField] private EffectEvaluator _effectEvaluator;
        [SerializeField] private BoardStateManager _boardStateManager;
        [SerializeField] private CardPlayManager _cardPlayManager;
        [SerializeField] private AttackManager _attackManager;
        [SerializeField] private EntityCacheManager _entityCacheManager;

        // Scene dependencies
        private ICombatManager _combatManager;
        private CombatStage _combatStage;
        private SpritePositioning _spritePositioning;
        private AttackLimiter _attackLimiter;

        // Track registered managers for dependency injection
        private readonly HashSet<object> _registeredManagers = new HashSet<object>();
        private readonly Dictionary<Type, Component> _serviceCache = new Dictionary<Type, Component>();

        #endregion

        #region Public Service Access Properties

        /// <summary>
        /// Gets or creates the KeywordEvaluator instance.
        /// </summary>
        public IKeywordEvaluator KeywordEvaluator
        {
            get => GetService<KeywordEvaluator, IKeywordEvaluator>(ref _keywordEvaluator);
        }

        /// <summary>
        /// Gets or creates the EffectEvaluator instance.
        /// </summary>
        public IEffectEvaluator EffectEvaluator
        {
            get => GetService<EffectEvaluator, IEffectEvaluator>(ref _effectEvaluator);
        }

        /// <summary>
        /// Gets or creates the BoardStateManager instance, checking for an existing singleton first.
        /// </summary>
        public IBoardStateManager BoardStateManager
        {
            get => GetSingletonService<BoardStateManager, IBoardStateManager>(
                ref _boardStateManager,
                () => EnemyInteraction.Managers.BoardStateManager.Instance);
        }

        /// <summary>
        /// Gets or creates the EntityCacheManager instance.
        /// </summary>
        public IEntityCacheManager EntityCacheManager
        {
            get => GetSingletonService<EntityCacheManager, IEntityCacheManager>(
                ref _entityCacheManager,
                () => EnemyInteraction.Managers.EntityCacheManager.Instance);
        }

        /// <summary>
        /// Gets or creates the CardPlayManager instance.
        /// </summary>
        public CardPlayManager CardPlayManager
        {
            get => GetService<CardPlayManager, CardPlayManager>(ref _cardPlayManager);
        }

        /// <summary>
        /// Gets or creates the AttackManager instance.
        /// </summary>
        public AttackManager AttackManager
        {
            get => GetSingletonService<AttackManager, AttackManager>(
                ref _attackManager,
                () => EnemyInteraction.Managers.AttackManager.Instance);
        }

        #endregion

        #region Lifecycle Methods

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                StartCoroutine(Initialize());
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[AIServices] Multiple instances of AIServices detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _isInitialized = false;
                _serviceCache.Clear();
                _registeredManagers.Clear();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes all services and dependencies in the correct order.
        /// </summary>
        private IEnumerator Initialize()
        {
            Debug.Log("[AIServices] Starting initialization...");

            if (this == null)
            {
                Debug.LogError("[AIServices] Initialize called but AIServices instance is null!");
                yield break;
            }

            // First, gather scene dependencies
            yield return StartCoroutine(GatherSceneDependencies());

            // Then initialize all core services in the correct order
            InitializeServices();

            // Wait a frame to ensure all references are set up
            yield return null;

            // Now inject dependencies into all services
            InjectDependenciesIntoServices();

            _isInitialized = true;
            Debug.Log("[AIServices] Initialization complete");
        }

        /// <summary>
        /// Finds and caches essential scene dependencies.
        /// </summary>
        private IEnumerator GatherSceneDependencies()
        {
            Debug.Log("[AIServices] Gathering scene dependencies...");

            int attempts = 0;
            int maxAttempts = 50;

            while ((_combatManager == null || _combatStage == null || _spritePositioning == null) && attempts < maxAttempts)
            {
                _combatManager = _combatManager ?? FindObjectOfType<CombatManager>();
                _combatStage = _combatStage ?? FindObjectOfType<CombatStage>();

                if (_combatStage != null)
                {
                    _spritePositioning = _spritePositioning ?? _combatStage.SpritePositioning as SpritePositioning;
                    _attackLimiter = _attackLimiter ?? _combatStage.GetAttackLimiter();
                }

                if (_combatManager == null || _combatStage == null || _spritePositioning == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }
            }

            if (_combatManager == null || _combatStage == null || _spritePositioning == null)
            {
                Debug.LogWarning("[AIServices] Could not find all required scene dependencies after multiple attempts");
            }
            else
            {
                Debug.Log("[AIServices] Scene dependencies gathered successfully");
            }
        }

        /// <summary>
        /// Initializes all core services, creating them if they don't exist.
        /// </summary>
        private void InitializeServices()
        {
            Debug.Log("[AIServices] Initializing services...");

            try
            {
                // First initialize services that are needed by other services
                var entityCache = EntityCacheManager;
                var keywordEval = KeywordEvaluator;
                var effectEval = EffectEvaluator;
                var boardStateMgr = BoardStateManager;

                // Then initialize managers that depend on the above services
                var cardPlayMgr = CardPlayManager;
                var attackMgr = AttackManager;

                Debug.Log("[AIServices] All services initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AIServices] Error initializing services: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Injects scene dependencies and other required references into all services.
        /// </summary>
        private void InjectDependenciesIntoServices()
        {
            Debug.Log("[AIServices] Injecting dependencies into services...");

            try
            {
                if (_combatManager != null && _combatStage != null && _spritePositioning != null)
                {
                    // Inject into BoardStateManager
                    if (_boardStateManager != null)
                    {
                        SetPrivateField(_boardStateManager, "_combatManager", _combatManager);
                        SetPrivateField(_boardStateManager, "_spritePositioning", _spritePositioning);

                        // Also inject EntityCacheManager if available
                        if (_entityCacheManager != null)
                        {
                            SetPrivateField(_boardStateManager, "_entityCacheManager", _entityCacheManager);
                        }
                    }

                    // Inject into CardPlayManager
                    if (_cardPlayManager != null)
                    {
                        SetPrivateField(_cardPlayManager, "_combatManager", _combatManager);
                        SetPrivateField(_cardPlayManager, "_combatStage", _combatStage);
                        SetPrivateField(_cardPlayManager, "_spritePositioning", _spritePositioning);
                        SetPrivateField(_cardPlayManager, "_keywordEvaluator", _keywordEvaluator);
                        SetPrivateField(_cardPlayManager, "_effectEvaluator", _effectEvaluator);
                        SetPrivateField(_cardPlayManager, "_boardStateManager", _boardStateManager);
                    }

                    // Inject into AttackManager if it exists
                    if (_attackManager != null)
                    {
                        InjectAttackManagerDependencies(_attackManager);
                    }

                    // Inject into any other registered managers
                    InjectDependenciesToRegisteredManagers();
                }
                else
                {
                    Debug.LogWarning("[AIServices] Could not inject dependencies - scene dependencies missing");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AIServices] Error during dependency injection: {e.Message}\n{e.StackTrace}");
            }
        }

        #endregion

        #region Service Registration and Dependency Injection

        /// <summary>
        /// Registers an AttackManager instance to receive dependencies from AIServices.
        /// </summary>
        public void RegisterAttackManager(AttackManager attackManager)
        {
            if (attackManager == null) return;

            Debug.Log("[AIServices] AttackManager registered");

            // Store the reference and register for dependency injection
            _attackManager = attackManager;
            _registeredManagers.Add(attackManager);

            // Inject dependencies if we're already initialized
            if (_isInitialized)
            {
                InjectAttackManagerDependencies(attackManager);
            }
        }

        /// <summary>
        /// Injects dependencies into the provided AttackManager instance.
        /// </summary>
        public void InjectAttackManagerDependencies(AttackManager attackManager)
        {
            Debug.Log("[AIServices] Injecting dependencies into AttackManager");

            // Inject scene dependencies
            SetPrivateField(attackManager, "_combatManager", _combatManager);
            SetPrivateField(attackManager, "_combatStage", _combatStage);
            SetPrivateField(attackManager, "_spritePositioning", _spritePositioning);

            // Inject service dependencies
            attackManager.InjectDependencies(
                KeywordEvaluator,
                BoardStateManager,
                EntityCacheManager
            );
        }

        /// <summary>
        /// Registers a BoardStateManager instance to receive dependencies from AIServices.
        /// </summary>
        public void RegisterBoardStateManager(BoardStateManager boardStateManager)
        {
            if (boardStateManager == null) return;

            Debug.Log("[AIServices] BoardStateManager registered");

            // Store the reference and register for dependency injection
            _boardStateManager = boardStateManager;
            _registeredManagers.Add(boardStateManager);

            // Inject dependencies if we're already initialized
            if (_isInitialized && _combatManager != null && _spritePositioning != null)
            {
                SetPrivateField(boardStateManager, "_combatManager", _combatManager);
                SetPrivateField(boardStateManager, "_spritePositioning", _spritePositioning);

                if (_entityCacheManager != null)
                {
                    SetPrivateField(boardStateManager, "_entityCacheManager", _entityCacheManager);
                }
            }
        }

        /// <summary>
        /// Registers an EntityCacheManager instance to receive dependencies from AIServices.
        /// </summary>
        public void RegisterEntityCacheManager(EntityCacheManager entityCacheManager)
        {
            if (entityCacheManager == null) return;

            Debug.Log("[AIServices] EntityCacheManager registered");

            // Store the reference and register for dependency injection
            _entityCacheManager = entityCacheManager;
            _registeredManagers.Add(entityCacheManager);

            // Inject dependencies if we're already initialized
            if (_isInitialized && _spritePositioning != null && _attackLimiter != null)
            {
                entityCacheManager.Initialize(_spritePositioning, _attackLimiter);
            }
        }

        /// <summary>
        /// Registers a CardPlayManager instance to receive dependencies from AIServices.
        /// </summary>
        public void RegisterCardPlayManager(CardPlayManager cardPlayManager)
        {
            if (cardPlayManager == null) return;

            Debug.Log("[AIServices] CardPlayManager registered");

            // Store the reference and register for dependency injection
            _cardPlayManager = cardPlayManager;
            _registeredManagers.Add(cardPlayManager);

            // Inject dependencies if we're already initialized
            if (_isInitialized)
            {
                SetPrivateField(cardPlayManager, "_combatManager", _combatManager);
                SetPrivateField(cardPlayManager, "_combatStage", _combatStage);
                SetPrivateField(cardPlayManager, "_spritePositioning", _spritePositioning);
                SetPrivateField(cardPlayManager, "_keywordEvaluator", _keywordEvaluator);
                SetPrivateField(cardPlayManager, "_effectEvaluator", _effectEvaluator);
                SetPrivateField(cardPlayManager, "_boardStateManager", _boardStateManager);
            }
        }

        /// <summary>
        /// Injects dependencies into all registered managers.
        /// </summary>
        private void InjectDependenciesToRegisteredManagers()
        {
            foreach (var manager in _registeredManagers)
            {
                if (manager is AttackManager attackManager)
                {
                    InjectAttackManagerDependencies(attackManager);
                }
                else if (manager is BoardStateManager boardStateManager)
                {
                    SetPrivateField(boardStateManager, "_combatManager", _combatManager);
                    SetPrivateField(boardStateManager, "_spritePositioning", _spritePositioning);
                }
                else if (manager is EntityCacheManager entityCacheManager)
                {
                    entityCacheManager.Initialize(_spritePositioning, _attackLimiter);
                }
            }
        }

        #endregion

        #region Service Creation and Access Helpers

        /// <summary>
        /// Generic helper to get or create a service component.
        /// </summary>
        private TInterface GetService<TComponent, TInterface>(ref TComponent serviceField)
            where TComponent : Component, TInterface
            where TInterface : class
        {
            if (serviceField != null)
                return serviceField;

            // Check if we've already cached this component type
            if (_serviceCache.TryGetValue(typeof(TComponent), out var cachedComponent))
            {
                serviceField = cachedComponent as TComponent;
                return serviceField;
            }

            try
            {
                // Check if the service already exists in the scene
                var existingService = FindObjectOfType<TComponent>();
                if (existingService != null)
                {
                    Debug.Log($"[AIServices] Found existing {typeof(TComponent).Name} in scene");
                    serviceField = existingService;
                    _serviceCache[typeof(TComponent)] = existingService;
                    return serviceField;
                }

                // Create a child GameObject for the service
                string serviceName = typeof(TComponent).Name;
                GameObject serviceObj = new GameObject(serviceName);

                // Ensure we have a valid transform before setting parent
                if (this != null && this.transform != null)
                {
                    serviceObj.transform.SetParent(transform);
                }
                else
                {
                    Debug.LogWarning($"[AIServices] Cannot set parent for {serviceName} - AIServices transform is null");
                    DontDestroyOnLoad(serviceObj);
                }

                // Add the component
                serviceField = serviceObj.AddComponent<TComponent>();

                if (serviceField != null)
                {
                    Debug.Log($"[AIServices] Created {serviceName}");
                    _serviceCache[typeof(TComponent)] = serviceField;
                }
                else
                {
                    Debug.LogError($"[AIServices] Failed to create {serviceName}");
                }

                return serviceField;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AIServices] Error creating service {typeof(TComponent).Name}: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Gets or creates a service that implements the singleton pattern.
        /// </summary>
        private TInterface GetSingletonService<TComponent, TInterface>(
            ref TComponent serviceField,
            Func<TComponent> getSingletonInstance)
            where TComponent : Component, TInterface
            where TInterface : class
        {
            try
            {
                // First check if there's already a singleton instance
                var existingSingleton = getSingletonInstance();

                if (existingSingleton != null)
                {
                    // If we have a field reference that's different, destroy it
                    if (serviceField != null && serviceField != existingSingleton)
                    {
                        Debug.LogWarning($"[AIServices] Found a different {typeof(TComponent).Name} singleton instance, using it instead");
                        Destroy(serviceField.gameObject);
                        serviceField = existingSingleton;
                    }
                    else if (serviceField == null)
                    {
                        serviceField = existingSingleton;
                    }

                    return serviceField;
                }

                // If no singleton exists, create and register a new one
                return GetService<TComponent, TInterface>(ref serviceField);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AIServices] Error getting {typeof(TComponent).Name}: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Helper method to set a private field using reflection.
        /// </summary>
        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;

            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[AIServices] Field '{fieldName}' not found in {target.GetType().Name}");
            }
        }

        #endregion
    }
} 