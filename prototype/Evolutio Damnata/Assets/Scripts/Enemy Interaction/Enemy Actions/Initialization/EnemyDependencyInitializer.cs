using System.Collections;
using UnityEngine;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using EnemyInteraction.Models;
using EnemyInteraction.Services;
using EnemyInteraction.Interfaces;

namespace EnemyInteraction
{
    public class EnemyDependencyInitializer : MonoBehaviour
    {
        [SerializeField] private float _maxInitializationTime = 10f;
        [SerializeField] private float _componentWaitTime = 0.1f;
        
        private float _initializationTimer = 0f;
        private bool _isInitialized = false;
        
        public bool IsInitialized => _isInitialized;
        
        // Dependencies gathered during initialization
        private ICombatManager _combatManager;
        private SpritePositioning _spritePositioning;
        private Deck _enemyDeck;
        private CardLibrary _cardLibrary;
        private CombatStage _combatStage;
        private ISpellEffectApplier _spellEffectApplier;
        private StackManager _stackManager;
        private AttackLimiter _attackLimiter;
        private IKeywordEvaluator _keywordEvaluator;
        private IEffectEvaluator _effectEvaluator;
        private IBoardStateManager _boardStateManager; 
        private ICardPlayManager _cardPlayManager;
        private IAttackManager _attackManager;
        
        // Public accessors
        public ICombatManager CombatManager => _combatManager;
        public SpritePositioning SpritePositioning => _spritePositioning;
        public Deck EnemyDeck => _enemyDeck;
        public CardLibrary CardLibrary => _cardLibrary;
        public CombatStage CombatStage => _combatStage;
        public ISpellEffectApplier SpellEffectApplier => _spellEffectApplier;
        public StackManager StackManager => _stackManager;
        public AttackLimiter AttackLimiter => _attackLimiter;
        public IKeywordEvaluator KeywordEvaluator => _keywordEvaluator;
        public IEffectEvaluator EffectEvaluator => _effectEvaluator;
        public IBoardStateManager BoardStateManager => _boardStateManager;
        public ICardPlayManager CardPlayManager => _cardPlayManager;
        public IAttackManager AttackManager => _attackManager;
        
        public IEnumerator Initialize()
        {
            Debug.Log("[EnemyDependencyInitializer] Starting initialization...");
            
            // First, find and initialize core scene dependencies
            yield return StartCoroutine(InitializeCoreDependencies());
            
            // Continue only if core dependencies are valid
            if (!ValidateCoreDependencies())
            {
                Debug.LogError("[EnemyDependencyInitializer] Core dependencies validation failed!");
                yield break;
            }
            
            Debug.Log("[EnemyDependencyInitializer] Core dependencies validated");
            
            // Get services from AIServices
            yield return StartCoroutine(InitializeAIServices());
            
            // Mark as initialized
            _isInitialized = true;
            Debug.Log("[EnemyDependencyInitializer] Initialization completed");
        }
        
        private IEnumerator InitializeAIServices()
        {
            // Get services from AIServices
            var services = AIServices.Instance;
            
            // Get all required services
            if (services != null)
            {
                _keywordEvaluator = services.KeywordEvaluator;
                _effectEvaluator = services.EffectEvaluator;
                _boardStateManager = services.BoardStateManager;
                _cardPlayManager = services.CardPlayManager;
                _attackManager = services.AttackManager;
                
                Debug.Log("[EnemyDependencyInitializer] Got available services from AIServices");
            }
            else
            {
                Debug.LogError("[EnemyDependencyInitializer] Failed to get AIServices.Instance");
                yield break;
            }
            
            // Ensure all services are available
            if (_keywordEvaluator == null || _effectEvaluator == null ||
                _boardStateManager == null || _cardPlayManager == null || _attackManager == null)
            {
                Debug.LogError("[EnemyDependencyInitializer] Critical AI services are missing, cannot continue");
                yield break;
            }
            
            // Find and cache StackManager instance
            _stackManager = StackManager.Instance;
            if (_stackManager == null)
            {
                Debug.LogWarning("[EnemyDependencyInitializer] StackManager instance is null, effect stacking evaluations will not work");
            }
            
            yield return null;
        }
        
        private IEnumerator InitializeCoreDependencies()
        {
            // Find CombatManager with timeout
            while (_combatManager == null && _initializationTimer < _maxInitializationTime)
            {
                _combatManager = FindObjectOfType<CombatManager>();
                if (_combatManager == null)
                {
                    Debug.Log("[EnemyDependencyInitializer] Waiting for CombatManager...");
                    yield return new WaitForSeconds(_componentWaitTime);
                    _initializationTimer += _componentWaitTime;
                }
            }
            
            if (_combatManager == null)
            {
                Debug.LogError("[EnemyDependencyInitializer] Failed to find CombatManager within timeout period!");
                yield break;
            }
            Debug.Log("[EnemyDependencyInitializer] Found CombatManager");
            
            // Find CombatStage with timeout
            while (_combatStage == null && _initializationTimer < _maxInitializationTime)
            {
                _combatStage = FindObjectOfType<CombatStage>();
                if (_combatStage == null)
                {
                    Debug.Log("[EnemyDependencyInitializer] Waiting for CombatStage...");
                    yield return new WaitForSeconds(_componentWaitTime);
                    _initializationTimer += _componentWaitTime;
                }
            }
            
            if (_combatStage == null)
            {
                Debug.LogError("[EnemyDependencyInitializer] Failed to find CombatStage within timeout period!");
                yield break;
            }
            Debug.Log("[EnemyDependencyInitializer] Found CombatStage");
            
            // Wait for CombatStage to be ready with timeout
            float spritePositioningTimeout = 5f;
            float spritePositioningTimer = 0f;
            
            // Try to get SpritePositioning first
            while (_combatStage.SpritePositioning == null && spritePositioningTimer < spritePositioningTimeout && _initializationTimer < _maxInitializationTime)
            {
                Debug.Log("[EnemyDependencyInitializer] Waiting for CombatStage.SpritePositioning...");
                yield return new WaitForSeconds(_componentWaitTime);
                spritePositioningTimer += _componentWaitTime;
                _initializationTimer += _componentWaitTime;
            }
            
            if (_combatStage.SpritePositioning == null)
            {
                Debug.LogError("[EnemyDependencyInitializer] CombatStage.SpritePositioning not available within timeout period!");
                yield break;
            }
            
            // Get SpritePositioning
            _spritePositioning = _combatStage.SpritePositioning as SpritePositioning;
            Debug.Log("[EnemyDependencyInitializer] Got SpritePositioning");
            
            // Reset timer for SpellEffectApplier
            spritePositioningTimer = 0f;
            
            // Try to get SpellEffectApplier separately with timeout
            while (_combatStage.SpellEffectApplier == null && spritePositioningTimer < spritePositioningTimeout && _initializationTimer < _maxInitializationTime)
            {
                Debug.Log("[EnemyDependencyInitializer] Waiting for CombatStage.SpellEffectApplier...");
                yield return new WaitForSeconds(_componentWaitTime);
                spritePositioningTimer += _componentWaitTime;
                _initializationTimer += _componentWaitTime;
            }
            
            // Continue even if SpellEffectApplier is null - we'll check later
            if (_combatStage.SpellEffectApplier == null)
            {
                Debug.LogWarning("[EnemyDependencyInitializer] CombatStage.SpellEffectApplier not available - will try to initialize without it");
            }
            else
            {
                _spellEffectApplier = _combatStage.SpellEffectApplier;
                Debug.Log("[EnemyDependencyInitializer] Got SpellEffectApplier");
            }
            
            // Get other dependencies from CombatStage
            _cardLibrary = _combatStage.CardLibrary;
            if (_cardLibrary == null)
            {
                Debug.LogWarning("[EnemyDependencyInitializer] CardLibrary not available from CombatStage");
            }
            
            _attackLimiter = _combatStage.GetAttackLimiter();
            if (_attackLimiter == null)
            {
                Debug.LogWarning("[EnemyDependencyInitializer] AttackLimiter not available from CombatStage");
            }
            
            // Get EnemyDeck from CombatManager
            _enemyDeck = _combatManager.EnemyDeck;
            if (_enemyDeck == null)
            {
                Debug.LogWarning("[EnemyDependencyInitializer] EnemyDeck not available from CombatManager");
            }
            else
            {
                Debug.Log("[EnemyDependencyInitializer] Got EnemyDeck");
            }
        }
        
        private bool ValidateCoreDependencies()
        {
            // Required core components
            bool hasCombatManager = _combatManager != null;
            bool hasSpritePositioning = _spritePositioning != null;
            
            // Non-critical components
            bool hasEnemyDeck = _enemyDeck != null;
            bool hasCardLibrary = _cardLibrary != null;
            bool hasCombatStage = _combatStage != null;
            bool hasSpellEffectApplier = _spellEffectApplier != null;
            
            // Log errors for critical missing components
            if (!hasCombatManager)
            {
                Debug.LogError($"[{nameof(EnemyDependencyInitializer)}] CRITICAL: Failed to find CombatManager in scene!");
            }
            
            if (!hasSpritePositioning)
            {
                Debug.LogError($"[{nameof(EnemyDependencyInitializer)}] CRITICAL: Failed to get SpritePositioning from CombatStage!");
            }
            
            // Log warnings for non-critical missing components
            if (!hasEnemyDeck)
            {
                Debug.LogWarning($"[{nameof(EnemyDependencyInitializer)}] Non-critical: EnemyDeck is not available yet from CombatManager.");
            }
            
            if (!hasCardLibrary)
            {
                Debug.LogWarning($"[{nameof(EnemyDependencyInitializer)}] Non-critical: CardLibrary is not available yet from CombatStage.");
            }
            
            if (!hasCombatStage)
            {
                Debug.LogWarning($"[{nameof(EnemyDependencyInitializer)}] Non-critical: CombatStage reference is null but might be recovered later.");
            }
            
            if (!hasSpellEffectApplier)
            {
                Debug.LogWarning($"[{nameof(EnemyDependencyInitializer)}] Non-critical: SpellEffectApplier is not available yet from CombatStage.");
            }
            
            // We MUST have Combat Manager and SpritePositioning to proceed
            bool hasCriticalComponents = hasCombatManager && hasSpritePositioning;
            
            if (!hasCriticalComponents)
            {
                Debug.LogError($"[{nameof(EnemyDependencyInitializer)}] One or more critical components are missing. AI initialization cannot continue.");
            }
            else
            {
                Debug.Log($"[{nameof(EnemyDependencyInitializer)}] Critical components are available. Continuing initialization.");
            }
            
            return hasCriticalComponents;
        }
    }
}
