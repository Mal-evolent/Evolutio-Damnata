using UnityEngine;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using EnemyInteraction.Interfaces;

namespace EnemyInteraction.Services
{
    [CreateAssetMenu(fileName = "AIServices", menuName = "AI/Services")]
    public class AIServices : ScriptableObject
    {
        private static AIServices _instance;
        private static bool _isInitialized;
        private static bool _isInitializing;
        private static GameObject _aiServicesRoot;

        [SerializeField] private KeywordEvaluator _keywordEvaluator;
        [SerializeField] private EffectEvaluator _effectEvaluator;
        [SerializeField] private BoardStateManager _boardStateManager;
        [SerializeField] private CardPlayManager _cardPlayManager;
        [SerializeField] private AttackManager _attackManager;

        public static AIServices Instance
        {
            get
            {
                if (!_isInitialized && !_isInitializing)
                {
                    InitializeInstance();
                }
                return _instance;
            }
        }

        private static void InitializeInstance()
        {
            if (_instance != null)
            {
                _isInitialized = true;
                return;
            }

            _isInitializing = true;
            Debug.Log("[AIServices] Starting initialization...");

            // Load from Resources
            _instance = Resources.Load<AIServices>("AIServices");
            
            if (_instance == null)
            {
                Debug.LogError("[AIServices] Failed to load AIServices from Resources! Ensure AIServices.asset exists in a Resources folder.");
                // Create instance if loading failed
                _instance = ScriptableObject.CreateInstance<AIServices>();
                Debug.Log("[AIServices] Created new instance of AIServices");
            }

            // Create a parent GameObject to hold all managers if it doesn't exist
            if (_aiServicesRoot == null)
            {
                _aiServicesRoot = new GameObject("AIServicesRoot");
                GameObject.DontDestroyOnLoad(_aiServicesRoot);
                Debug.Log("[AIServices] Created AIServicesRoot GameObject");
            }

            // Create components safely
            CreateKeywordEvaluator();
            CreateEffectEvaluator();
            CreateBoardStateManager();
            CreateCardPlayManager();
            CreateAttackManager();

            // Validate services - but don't fail completely if some are missing
            ValidateServices();

            _isInitialized = true;
            _isInitializing = false;
            Debug.Log("[AIServices] Successfully initialized all services");
        }

        private static void CreateKeywordEvaluator()
        {
            try
            {
                if (_instance._keywordEvaluator == null)
                {
                    var keywordEvaluatorObj = new GameObject("KeywordEvaluator");
                    keywordEvaluatorObj.transform.SetParent(_aiServicesRoot.transform);
                    _instance._keywordEvaluator = keywordEvaluatorObj.AddComponent<KeywordEvaluator>();
                    Debug.Log("[AIServices] Created KeywordEvaluator");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIServices] Error creating KeywordEvaluator: {e.Message}");
            }
        }

        private static void CreateEffectEvaluator()
        {
            try
            {
                if (_instance._effectEvaluator == null)
                {
                    var effectEvaluatorObj = new GameObject("EffectEvaluator");
                    effectEvaluatorObj.transform.SetParent(_aiServicesRoot.transform);
                    _instance._effectEvaluator = effectEvaluatorObj.AddComponent<EffectEvaluator>();
                    Debug.Log("[AIServices] Created EffectEvaluator");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIServices] Error creating EffectEvaluator: {e.Message}");
            }
        }

        private static void CreateBoardStateManager()
        {
            try
            {
                if (_instance._boardStateManager == null)
                {
                    var boardStateManagerObj = new GameObject("BoardStateManager");
                    boardStateManagerObj.transform.SetParent(_aiServicesRoot.transform);
                    _instance._boardStateManager = boardStateManagerObj.AddComponent<BoardStateManager>();
                    Debug.Log("[AIServices] Created BoardStateManager");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIServices] Error creating BoardStateManager: {e.Message}");
            }
        }

        private static void CreateCardPlayManager()
        {
            try
            {
                if (_instance._cardPlayManager == null)
                {
                    var cardPlayManagerObj = new GameObject("CardPlayManager");
                    cardPlayManagerObj.transform.SetParent(_aiServicesRoot.transform);
                    _instance._cardPlayManager = cardPlayManagerObj.AddComponent<CardPlayManager>();
                    Debug.Log("[AIServices] Created CardPlayManager");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIServices] Error creating CardPlayManager: {e.Message}");
            }
        }

        private static void CreateAttackManager()
        {
            try
            {
                if (_instance._attackManager == null)
                {
                    var attackManagerObj = new GameObject("AttackManager");
                    attackManagerObj.transform.SetParent(_aiServicesRoot.transform);
                    _instance._attackManager = attackManagerObj.AddComponent<AttackManager>();
                    Debug.Log("[AIServices] Created AttackManager");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIServices] Error creating AttackManager: {e.Message}");
            }
        }

        private static bool ValidateServices()
        {
            bool allValid = true;

            if (_instance._keywordEvaluator == null)
            {
                Debug.LogWarning("[AIServices] KeywordEvaluator is null!");
                allValid = false;
            }

            if (_instance._effectEvaluator == null)
            {
                Debug.LogWarning("[AIServices] EffectEvaluator is null!");
                allValid = false;
            }

            if (_instance._boardStateManager == null)
            {
                Debug.LogWarning("[AIServices] BoardStateManager is null!");
                allValid = false;
            }

            if (_instance._cardPlayManager == null)
            {
                Debug.LogWarning("[AIServices] CardPlayManager is null!");
                allValid = false;
            }

            if (_instance._attackManager == null)
            {
                Debug.LogWarning("[AIServices] AttackManager is null!");
                allValid = false;
            }

            if (!allValid)
            {
                Debug.LogWarning("[AIServices] Some services are missing, but initialization will continue.");
            }

            return allValid;
        }

        public IKeywordEvaluator KeywordEvaluator => _keywordEvaluator;
        public IEffectEvaluator EffectEvaluator => _effectEvaluator;
        public IBoardStateManager BoardStateManager => _boardStateManager;
        public CardPlayManager CardPlayManager => _cardPlayManager;
        public AttackManager AttackManager => _attackManager;

        private void OnEnable()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        private void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
                _isInitialized = false;
            }
        }
    }
} 