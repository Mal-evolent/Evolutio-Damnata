using UnityEngine;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using EnemyInteraction.Interfaces;
using System.Collections;

namespace EnemyInteraction.Services
{
    public class AIServices : MonoBehaviour
    {
        private static AIServices _instance;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;

        [SerializeField] private KeywordEvaluator _keywordEvaluator;
        [SerializeField] private EffectEvaluator _effectEvaluator;
        [SerializeField] private BoardStateManager _boardStateManager;
        [SerializeField] private CardPlayManager _cardPlayManager;
        [SerializeField] private AttackManager _attackManager;

        // Public properties with guaranteed initialization
        public IKeywordEvaluator KeywordEvaluator 
        {
            get
            {
                try
                {
                    return GetOrCreateService(ref _keywordEvaluator);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AIServices] Error getting KeywordEvaluator: {e.Message}");
                    return null;
                }
            }
        }
        
        public IEffectEvaluator EffectEvaluator 
        {
            get
            {
                try
                {
                    return GetOrCreateService(ref _effectEvaluator);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AIServices] Error getting EffectEvaluator: {e.Message}");
                    return null;
                }
            }
        }
        
        public IBoardStateManager BoardStateManager 
        {
            get
            {
                try
                {
                    return GetOrCreateService(ref _boardStateManager);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AIServices] Error getting BoardStateManager: {e.Message}");
                    return null;
                }
            }
        }
        
        public CardPlayManager CardPlayManager 
        {
            get
            {
                try
                {
                    return GetOrCreateService(ref _cardPlayManager);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AIServices] Error getting CardPlayManager: {e.Message}");
                    return null;
                }
            }
        }
        
        public AttackManager AttackManager 
        {
            get
            {
                try
                {
                    return GetOrCreateService(ref _attackManager);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AIServices] Error getting AttackManager: {e.Message}");
                    return null;
                }
            }
        }

        // Public property to check if services are ready
        public static bool IsInitialized => _isInitialized;

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

        private IEnumerator Initialize()
        {
            Debug.Log("[AIServices] Starting initialization...");
            
            if (this == null)
            {
                Debug.LogError("[AIServices] Initialize called but AIServices instance is null!");
                yield break;
            }

            // Create required components if they don't exist
            try
            {
                if (_keywordEvaluator == null) GetOrCreateService(ref _keywordEvaluator);
                if (_effectEvaluator == null) GetOrCreateService(ref _effectEvaluator);
                if (_boardStateManager == null) GetOrCreateService(ref _boardStateManager);
                if (_cardPlayManager == null) GetOrCreateService(ref _cardPlayManager);
                if (_attackManager == null) GetOrCreateService(ref _attackManager);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIServices] Error creating services: {e.Message}\n{e.StackTrace}");
                yield break;
            }

            // Wait for critical scene dependencies
            ICombatManager combatManager = null;
            CombatStage combatStage = null;
            SpritePositioning spritePositioning = null;
            int attempts = 0;
            int maxAttempts = 50;

            while ((combatManager == null || combatStage == null || spritePositioning == null) && attempts < maxAttempts)
            {
                combatManager = combatManager ?? FindObjectOfType<CombatManager>();
                combatStage = combatStage ?? FindObjectOfType<CombatStage>();
                if (combatStage != null)
                {
                    spritePositioning = combatStage.SpritePositioning as SpritePositioning;
                }

                if (combatManager == null || combatStage == null || spritePositioning == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }
            }

            // Inject dependencies into our services
            try
            {
                if (combatManager != null && combatStage != null && spritePositioning != null)
                {
                    Debug.Log("[AIServices] Found scene dependencies, injecting into services");
                    
                    // Inject into BoardStateManager
                    if (_boardStateManager != null)
                    {
                        SetPrivateField(_boardStateManager, "_combatManager", combatManager);
                        SetPrivateField(_boardStateManager, "_spritePositioning", spritePositioning);
                    }
                    
                    // Inject into CardPlayManager
                    if (_cardPlayManager != null)
                    {
                        SetPrivateField(_cardPlayManager, "_combatManager", combatManager);
                        SetPrivateField(_cardPlayManager, "_combatStage", combatStage);
                        SetPrivateField(_cardPlayManager, "_spritePositioning", spritePositioning);
                    }
                    
                    // Inject into AttackManager
                    if (_attackManager != null)
                    {
                        SetPrivateField(_attackManager, "_combatManager", combatManager);
                        SetPrivateField(_attackManager, "_combatStage", combatStage);
                        SetPrivateField(_attackManager, "_spritePositioning", spritePositioning);
                    }
                }
                else
                {
                    Debug.LogWarning("[AIServices] Could not find all required scene dependencies");
                }
                
                AIServices._isInitialized = true;
                Debug.Log("[AIServices] Initialization complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIServices] Error during dependency injection: {e.Message}\n{e.StackTrace}");
            }
        }

        // Helper method to set a private field using reflection
        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;

            var field = target.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (field != null)
                field.SetValue(target, value);
        }

        // Generic helper method to get or create a service component
        private T GetOrCreateService<T>(ref T serviceField) where T : Component
        {
            if (serviceField != null)
                return serviceField;
            
            try
            {    
                // Create a child GameObject for the service
                string serviceName = typeof(T).Name;
                GameObject serviceObj = new GameObject(serviceName);
                
                // Ensure we have a valid transform before setting parent
                if (this != null && this.transform != null)
                {
                    serviceObj.transform.SetParent(transform);
                }
                else
                {
                    Debug.LogWarning($"[AIServices] Cannot set parent for {serviceName} - AIServices transform is null");
                    // Don't destroy on load to keep it alive
                    DontDestroyOnLoad(serviceObj);
                }
                
                // Add the component
                serviceField = serviceObj.AddComponent<T>();
                
                if (serviceField != null)
                {
                    Debug.Log($"[AIServices] Created {serviceName}");
                }
                else
                {
                    Debug.LogError($"[AIServices] Failed to create {serviceName}");
                }
                
                return serviceField;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIServices] Error creating service: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        // When this is destroyed, reset the static instance
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _isInitialized = false;
            }
        }
    }
} 