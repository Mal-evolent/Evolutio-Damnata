using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using EnemyInteraction.Models;
using EnemyInteraction.Services;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;

namespace EnemyInteraction
{
    public class EnemyActions : MonoBehaviour, IEnemyActions
    {
        private ICombatManager _combatManager;
        private SpritePositioning _spritePositioning;
        private Deck _enemyDeck;
        private CardLibrary _cardLibrary;
        private CombatStage _combatStage;
        private ISpellEffectApplier _spellEffectApplier;
        private StackManager _stackManager;
        private AttackLimiter _attackLimiter;

        // Public properties for controlled access
        public ICombatManager CombatManager { get => _combatManager; set => _combatManager = value; }
        public SpritePositioning SpritePositioning { get => _spritePositioning; set => _spritePositioning = value; }
        public Deck EnemyDeck { get => _enemyDeck; set => _enemyDeck = value; }
        public CardLibrary CardLibrary { get => _cardLibrary; set => _cardLibrary = value; }
        public CombatStage CombatStage { get => _combatStage; set => _combatStage = value; }

        private IKeywordEvaluator _keywordEvaluator;
        private IEffectEvaluator _effectEvaluator;
        private IBoardStateManager _boardStateManager;
        private ICardPlayManager _cardPlayManager;
        private IAttackManager _attackManager;

        [SerializeField] private float _maxInitializationTime = 10f;
        [SerializeField] private float _componentWaitTime = 0.1f;
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;

        private float _initializationTimer = 0f;

        private void Start()
        {
            _combatManager = FindObjectOfType<CombatManager>();

            // Subscribe to phase changes
            if (_combatManager is CombatManager combatManagerImpl)
            {
                combatManagerImpl.SubscribeToPhaseChanges(OnPhaseChanged);
            }
        }

        private void Awake()
        {
            StartCoroutine(Initialize());
        }

        private void OnPhaseChanged(CombatPhase newPhase)
        {
            Debug.Log($"[EnemyActions] Phase changed to {newPhase}");
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (_combatManager is CombatManager combatManagerImpl)
            {
                combatManagerImpl.UnsubscribeFromPhaseChanges(OnPhaseChanged);
            }
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[EnemyActions] Starting initialization...");

            // First, find and initialize core scene dependencies
            yield return StartCoroutine(InitializeCoreDependencies());

            // Continue only if core dependencies are valid
            if (!ValidateCoreDependencies())
            {
                Debug.LogError("[EnemyActions] Core dependencies validation failed!");
                enabled = false;
                yield break;
            }
            Debug.Log("[EnemyActions] Core dependencies validated");

            // Get services from AIServices
            // This will create AIServices if it doesn't exist
            var services = AIServices.Instance;

            // Get all required services
            if (services != null)
            {
                _keywordEvaluator = services.KeywordEvaluator;
                _effectEvaluator = services.EffectEvaluator;
                _boardStateManager = services.BoardStateManager;
                _cardPlayManager = services.CardPlayManager;
                _attackManager = services.AttackManager;

                Debug.Log("[EnemyActions] Got available services from AIServices");
            }
            else
            {
                Debug.LogError("[EnemyActions] Failed to get AIServices.Instance");
                enabled = false;
                yield break;
            }

            // Ensure all services are available
            if (_keywordEvaluator == null || _effectEvaluator == null ||
                _boardStateManager == null || _cardPlayManager == null || _attackManager == null)
            {
                Debug.LogError("[EnemyActions] Critical AI services are missing, cannot continue");
                enabled = false;
                yield break;
            }

            // Find and cache StackManager instance
            _stackManager = StackManager.Instance;
            if (_stackManager == null)
            {
                Debug.LogWarning("[EnemyActions] StackManager instance is null, effect stacking evaluations will not work");
            }

            // Mark as initialized
            _isInitialized = true;
            Debug.Log("[EnemyActions] Initialization completed");
        }

        private IEnumerator InitializeCoreDependencies()
        {
            // Find CombatManager with timeout
            while (_combatManager == null && _initializationTimer < _maxInitializationTime)
            {
                _combatManager = FindObjectOfType<CombatManager>();
                if (_combatManager == null)
                {
                    Debug.Log("[EnemyActions] Waiting for CombatManager...");
                    yield return new WaitForSeconds(_componentWaitTime);
                    _initializationTimer += _componentWaitTime;
                }
            }

            if (_combatManager == null)
            {
                Debug.LogError("[EnemyActions] Failed to find CombatManager within timeout period!");
                yield break;
            }
            Debug.Log("[EnemyActions] Found CombatManager");

            // Find CombatStage with timeout
            while (_combatStage == null && _initializationTimer < _maxInitializationTime)
            {
                _combatStage = FindObjectOfType<CombatStage>();
                if (_combatStage == null)
                {
                    Debug.Log("[EnemyActions] Waiting for CombatStage...");
                    yield return new WaitForSeconds(_componentWaitTime);
                    _initializationTimer += _componentWaitTime;
                }
            }

            if (_combatStage == null)
            {
                Debug.LogError("[EnemyActions] Failed to find CombatStage within timeout period!");
                yield break;
            }
            Debug.Log("[EnemyActions] Found CombatStage");

            // Wait for CombatStage to be ready with timeout
            float spritePositioningTimeout = 5f; // Increased from 3f to give more time
            float spritePositioningTimer = 0f;

            // Try to get SpritePositioning first
            while (_combatStage.SpritePositioning == null && spritePositioningTimer < spritePositioningTimeout && _initializationTimer < _maxInitializationTime)
            {
                Debug.Log("[EnemyActions] Waiting for CombatStage.SpritePositioning...");
                yield return new WaitForSeconds(_componentWaitTime);
                spritePositioningTimer += _componentWaitTime;
                _initializationTimer += _componentWaitTime;
            }

            if (_combatStage.SpritePositioning == null)
            {
                Debug.LogError("[EnemyActions] CombatStage.SpritePositioning not available within timeout period!");
                yield break;
            }

            // Get SpritePositioning
            _spritePositioning = _combatStage.SpritePositioning as SpritePositioning;
            Debug.Log("[EnemyActions] Got SpritePositioning");

            // Reset timer for SpellEffectApplier
            spritePositioningTimer = 0f;

            // Try to get SpellEffectApplier separately with timeout
            while (_combatStage.SpellEffectApplier == null && spritePositioningTimer < spritePositioningTimeout && _initializationTimer < _maxInitializationTime)
            {
                Debug.Log("[EnemyActions] Waiting for CombatStage.SpellEffectApplier...");
                yield return new WaitForSeconds(_componentWaitTime);
                spritePositioningTimer += _componentWaitTime;
                _initializationTimer += _componentWaitTime;
            }

            // Continue even if SpellEffectApplier is null - we'll check later
            if (_combatStage.SpellEffectApplier == null)
            {
                Debug.LogWarning("[EnemyActions] CombatStage.SpellEffectApplier not available - will try to initialize without it");
            }
            else
            {
                _spellEffectApplier = _combatStage.SpellEffectApplier;
                Debug.Log("[EnemyActions] Got SpellEffectApplier");
            }

            // Get other dependencies from CombatStage
            _cardLibrary = _combatStage.CardLibrary;
            if (_cardLibrary == null)
            {
                Debug.LogWarning("[EnemyActions] CardLibrary not available from CombatStage");
            }

            _attackLimiter = _combatStage.GetAttackLimiter();
            if (_attackLimiter == null)
            {
                Debug.LogWarning("[EnemyActions] AttackLimiter not available from CombatStage");
            }

            // Get EnemyDeck from CombatManager
            _enemyDeck = _combatManager.EnemyDeck;
            if (_enemyDeck == null)
            {
                Debug.LogWarning("[EnemyActions] EnemyDeck not available from CombatManager");
            }
            else
            {
                Debug.Log("[EnemyActions] Got EnemyDeck");
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
                Debug.LogError($"[{nameof(EnemyActions)}] CRITICAL: Failed to find CombatManager in scene!");
            }

            if (!hasSpritePositioning)
            {
                Debug.LogError($"[{nameof(EnemyActions)}] CRITICAL: Failed to get SpritePositioning from CombatStage!");
            }

            // Log warnings for non-critical missing components
            if (!hasEnemyDeck)
            {
                Debug.LogWarning($"[{nameof(EnemyActions)}] Non-critical: EnemyDeck is not available yet from CombatManager.");
            }

            if (!hasCardLibrary)
            {
                Debug.LogWarning($"[{nameof(EnemyActions)}] Non-critical: CardLibrary is not available yet from CombatStage.");
            }

            if (!hasCombatStage)
            {
                Debug.LogWarning($"[{nameof(EnemyActions)}] Non-critical: CombatStage reference is null but might be recovered later.");
            }

            if (!hasSpellEffectApplier)
            {
                Debug.LogWarning($"[{nameof(EnemyActions)}] Non-critical: SpellEffectApplier is not available yet from CombatStage.");
            }

            // We MUST have Combat Manager and SpritePositioning to proceed
            bool hasCriticalComponents = hasCombatManager && hasSpritePositioning;

            if (!hasCriticalComponents)
            {
                Debug.LogError($"[{nameof(EnemyActions)}] One or more critical components are missing. AI initialization cannot continue.");
            }
            else
            {
                Debug.Log($"[{nameof(EnemyActions)}] Critical components are available. Continuing initialization.");
            }

            return hasCriticalComponents;
        }

        public IEnumerator PlayCards()
        {
            Debug.Log("[EnemyActions] Starting PlayCards");

            // Wait for initialization if needed but with timeout
            if (!_isInitialized)
            {
                Debug.Log("[EnemyActions] Not yet initialized, waiting for initialization to complete...");
                float waitTime = 0f;
                float timeout = 3f;
                while (!_isInitialized && waitTime < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }

                // If still not initialized, abort
                if (!_isInitialized)
                {
                    Debug.LogError("[EnemyActions] Failed to initialize, cannot play cards");
                    yield break;
                }
            }

            // Make sure AIServices is ready before proceeding
            if (!AIServices.IsInitialized)
            {
                float waitTime = 0f;
                float timeout = 3f;
                while (!AIServices.IsInitialized && waitTime < timeout)
                {
                    Debug.Log("[EnemyActions] Waiting for AIServices to initialize...");
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }

                if (!AIServices.IsInitialized)
                {
                    Debug.LogError("[EnemyActions] AIServices initialization timed out");
                    yield break;
                }
            }

            // Ensure CardPlayManager is available
            if (_cardPlayManager == null)
            {
                Debug.LogError("[EnemyActions] CardPlayManager is null, cannot play cards");
                yield break;
            }

            // Refresh entity cache before playing cards
            var entityCacheManager = AIServices.Instance?.EntityCacheManager as EntityCacheManager;
            entityCacheManager?.RefreshAfterAction();

            // Get the coroutine outside the try block
            IEnumerator playCardsCoroutine = null;
            bool errorOccurred = false;

            try
            {
                playCardsCoroutine = _cardPlayManager.PlayCards();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemyActions] Error in CardPlayManager.PlayCards: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }

            // Execute the coroutine outside the try-catch if we got one successfully
            if (playCardsCoroutine != null && !errorOccurred)
            {
                yield return playCardsCoroutine;
            }
            else if (errorOccurred)
            {
                // Add a placeholder delay if there was an error
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[EnemyActions] PlayCards completed");
        }

        public IEnumerator Attack()
        {
            Debug.Log("[EnemyActions] Starting Attack");

            // Wait for initialization if needed but with timeout
            if (!_isInitialized)
            {
                Debug.Log("[EnemyActions] Not yet initialized, waiting for initialization to complete...");
                float waitTime = 0f;
                float timeout = 3f;
                while (!_isInitialized && waitTime < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }

                // If still not initialized, abort
                if (!_isInitialized)
                {
                    Debug.LogError("[EnemyActions] Failed to initialize, cannot perform attack");
                    yield break;
                }
            }

            // Make sure AIServices is ready before proceeding
            if (!AIServices.IsInitialized)
            {
                float waitTime = 0f;
                float timeout = 3f;
                while (!AIServices.IsInitialized && waitTime < timeout)
                {
                    Debug.Log("[EnemyActions] Waiting for AIServices to initialize...");
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }

                if (!AIServices.IsInitialized)
                {
                    Debug.LogError("[EnemyActions] AIServices initialization timed out");
                    yield break;
                }
            }

            // Ensure AttackManager is available
            if (_attackManager == null)
            {
                Debug.LogError("[EnemyActions] AttackManager is null, cannot perform attack");
                yield break;
            }

            // Get the coroutine outside the try block
            IEnumerator attackCoroutine = null;
            bool errorOccurred = false;

            try
            {
                attackCoroutine = _attackManager.Attack();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemyActions] Error in AttackManager.Attack: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }

            // Execute the coroutine outside the try-catch if we got one successfully
            if (attackCoroutine != null && !errorOccurred)
            {
                yield return attackCoroutine;
            }
            else if (errorOccurred)
            {
                // Add a placeholder delay if there was an error
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[EnemyActions] Attack completed");
        }

        public void LogCardsInHand()
        {
            if (_combatManager.EnemyDeck?.Hand == null)
            {
                Debug.LogWarning("Enemy deck or hand is null");
                return;
            }

            Debug.Log("Enemy Cards in Hand:");
            foreach (var card in _combatManager.EnemyDeck.Hand)
            {
                if (card != null)
                {
                    Debug.Log($"- {card.CardName} (Mana Cost: {card.CardType.ManaCost})");
                }
            }
        }
    }
}

