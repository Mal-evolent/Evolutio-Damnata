using UnityEngine;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

namespace EnemyInteraction.Services
{
    /// <summary>
    /// Central service registry and locator for AI-related components
    /// Each class has a single responsibility following SOLID principles
    /// </summary>
    public class AIServices : MonoBehaviour, IAIServiceLocator, IAIServiceRegistry
    {
        #region Singleton Implementation

        private static AIServices _instance;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;
        private const string GAME_SCENE_NAME = "gameScene";

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
        public bool IsInitialized => _isInitialized;

        #endregion

        // Dependencies and components
        private readonly ServiceLocator _serviceLocator;
        private readonly ServiceFactory _serviceFactory;
        private readonly SceneDependencyManager _sceneDependencyManager;
        private readonly DependencyInjector _dependencyInjector;

        // Constructor ensures components are created
        public AIServices()
        {
            _serviceLocator = new ServiceLocator();
            _serviceFactory = new ServiceFactory();
            _sceneDependencyManager = new SceneDependencyManager();
            _dependencyInjector = new DependencyInjector(_serviceLocator, _sceneDependencyManager);
        }

        #region IAIServiceLocator Implementation

        public IKeywordEvaluator KeywordEvaluator => _serviceLocator.GetService<IKeywordEvaluator>();
        public IEffectEvaluator EffectEvaluator => _serviceLocator.GetService<IEffectEvaluator>();
        public IBoardStateManager BoardStateManager => _serviceLocator.GetService<IBoardStateManager>();
        public IEntityCacheManager EntityCacheManager => _serviceLocator.GetService<IEntityCacheManager>();
        public CardPlayManager CardPlayManager => _serviceLocator.GetService<CardPlayManager>();
        public AttackManager AttackManager => _serviceLocator.GetService<AttackManager>();

        #endregion

        #region IAIServiceRegistry Implementation

        public void RegisterAttackManager(AttackManager attackManager)
        {
            if (attackManager == null) return;

            Debug.Log("[AIServices] AttackManager registered");
            _serviceLocator.RegisterService<AttackManager>(attackManager);
            _serviceLocator.RegisterService<IAttackManager>(attackManager);

            // Inject dependencies if we're already initialized
            if (_isInitialized)
            {
                _dependencyInjector.InjectAttackManagerDependencies(attackManager);
            }
        }

        public void RegisterBoardStateManager(BoardStateManager boardStateManager)
        {
            if (boardStateManager == null) return;

            Debug.Log("[AIServices] BoardStateManager registered");
            _serviceLocator.RegisterService<BoardStateManager>(boardStateManager);
            _serviceLocator.RegisterService<IBoardStateManager>(boardStateManager);

            // Inject dependencies if we're already initialized
            if (_isInitialized)
            {
                _dependencyInjector.InjectBoardStateManagerDependencies(boardStateManager);
            }
        }

        public void RegisterEntityCacheManager(EntityCacheManager entityCacheManager)
        {
            if (entityCacheManager == null) return;

            Debug.Log("[AIServices] EntityCacheManager registered");
            _serviceLocator.RegisterService<EntityCacheManager>(entityCacheManager);
            _serviceLocator.RegisterService<IEntityCacheManager>(entityCacheManager);

            // Inject dependencies if we're already initialized
            if (_isInitialized)
            {
                _dependencyInjector.InjectEntityCacheManagerDependencies(entityCacheManager);
            }
        }

        public void RegisterCardPlayManager(CardPlayManager cardPlayManager)
        {
            if (cardPlayManager == null) return;

            Debug.Log("[AIServices] CardPlayManager registered");
            _serviceLocator.RegisterService<CardPlayManager>(cardPlayManager);
            _serviceLocator.RegisterService<ICardPlayManager>(cardPlayManager);

            // Inject dependencies if we're already initialized
            if (_isInitialized)
            {
                _dependencyInjector.InjectCardPlayManagerDependencies(cardPlayManager);
            }
        }

        public void RegisterService<T>(T service) where T : class
        {
            if (service == null) return;
            _serviceLocator.RegisterService(service);
        }

        // Exposes the ability to inject dependencies into an AttackManager for backward compatibility
        public void InjectAttackManagerDependencies(AttackManager attackManager)
        {
            _dependencyInjector.InjectAttackManagerDependencies(attackManager);
        }

        #endregion

        #region Unity Lifecycle Methods

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
                Debug.Log("[AIServices] Singleton destroyed and static instance cleared");
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[AIServices] Scene loaded: {scene.name}");
            if (scene.name == GAME_SCENE_NAME)
            {
                StartCoroutine(ReinitializeServices());
            }
            else
            {
                Debug.Log("[AIServices] Not in game scene, destroying singleton and cleaning up");
                Destroy(gameObject); // This will trigger OnDestroy and clear _instance
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
            yield return StartCoroutine(_sceneDependencyManager.GatherSceneDependencies());

            // Register scene dependencies with the service locator
            RegisterSceneDependencies();

            // Initialize core services
            InitializeCoreServices();

            // Wait a frame to ensure all references are set up
            yield return null;

            // Now inject dependencies into all services
            _dependencyInjector.InjectDependenciesIntoServices();

            _isInitialized = true;
            Debug.Log("[AIServices] Initialization complete");
        }

        private void RegisterSceneDependencies()
        {
            // Register scene dependencies with the service locator
            _serviceLocator.RegisterService(_sceneDependencyManager.CombatManager);
            _serviceLocator.RegisterService(_sceneDependencyManager.CombatStage);
            _serviceLocator.RegisterService(_sceneDependencyManager.SpritePositioning);
            _serviceLocator.RegisterService(_sceneDependencyManager.AttackLimiter);
        }

        private void InitializeCoreServices()
        {
            try
            {
                // Create services if they don't exist yet
                if (_serviceLocator.GetService<IEntityCacheManager>() == null)
                {
                    EntityCacheManager entityCache = _serviceFactory.CreateEntityCacheManager(this.gameObject);
                    _serviceLocator.RegisterService<IEntityCacheManager>(entityCache);
                    _serviceLocator.RegisterService<EntityCacheManager>(entityCache);
                }

                if (_serviceLocator.GetService<IKeywordEvaluator>() == null)
                {
                    KeywordEvaluator keywordEval = _serviceFactory.CreateKeywordEvaluator(this.gameObject);
                    _serviceLocator.RegisterService<IKeywordEvaluator>(keywordEval);
                    _serviceLocator.RegisterService<KeywordEvaluator>(keywordEval);
                }

                if (_serviceLocator.GetService<IEffectEvaluator>() == null)
                {
                    EffectEvaluator effectEval = _serviceFactory.CreateEffectEvaluator(this.gameObject);
                    _serviceLocator.RegisterService<IEffectEvaluator>(effectEval);
                    _serviceLocator.RegisterService<EffectEvaluator>(effectEval);
                }

                if (_serviceLocator.GetService<IBoardStateManager>() == null)
                {
                    // Fully qualify the class name with its namespace to avoid ambiguity
                    BoardStateManager boardStateMgr = EnemyInteraction.Managers.BoardStateManager.Instance;
                    if (boardStateMgr == null)
                    {
                        boardStateMgr = _serviceFactory.CreateBoardStateManager(this.gameObject);
                    }
                    _serviceLocator.RegisterService<IBoardStateManager>(boardStateMgr);
                    _serviceLocator.RegisterService<BoardStateManager>(boardStateMgr);
                }

                if (_serviceLocator.GetService<CardPlayManager>() == null)
                {
                    CardPlayManager cardPlayMgr = _serviceFactory.CreateCardPlayManager(this.gameObject);
                    _serviceLocator.RegisterService<CardPlayManager>(cardPlayMgr);
                    _serviceLocator.RegisterService<ICardPlayManager>(cardPlayMgr);
                }

                if (_serviceLocator.GetService<AttackManager>() == null)
                {
                    // Try to find existing AttackManager singleton
                    AttackManager attackMgr = AttackManager.Instance;
                    if (attackMgr == null)
                    {
                        attackMgr = _serviceFactory.CreateAttackManager(this.gameObject);
                    }
                    _serviceLocator.RegisterService<AttackManager>(attackMgr);
                    _serviceLocator.RegisterService<IAttackManager>(attackMgr);
                }

                Debug.Log("[AIServices] All core services created");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AIServices] Error initializing services: {e.Message}\n{e.StackTrace}");
            }
        }

        private IEnumerator ReinitializeServices()
        {
            Debug.Log("[AIServices] Reinitializing services for game scene...");
            
            // Wait for scene to stabilize
            yield return new WaitForSeconds(0.5f);

            // First, gather scene dependencies
            yield return StartCoroutine(_sceneDependencyManager.GatherSceneDependencies());

            // Register scene dependencies with the service locator
            RegisterSceneDependencies();

            // Initialize core services
            InitializeCoreServices();

            // Wait a frame to ensure all references are set up
            yield return null;

            // Now inject dependencies into all services
            _dependencyInjector.InjectDependenciesIntoServices();

            _isInitialized = true;
            Debug.Log("[AIServices] Reinitialization complete");
        }

        #endregion
    }
} 