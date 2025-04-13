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

        private void Awake()
        {
            StartCoroutine(Initialize());
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
            float spritePositioningTimeout = 3f;
            float spritePositioningTimer = 0f;
            
            while ((_combatStage.SpritePositioning == null || _combatStage.SpellEffectApplier == null) 
                  && spritePositioningTimer < spritePositioningTimeout && _initializationTimer < _maxInitializationTime)
            {
                Debug.Log("[EnemyActions] Waiting for CombatStage to be fully initialized...");
                yield return new WaitForSeconds(_componentWaitTime);
                spritePositioningTimer += _componentWaitTime;
                _initializationTimer += _componentWaitTime;
            }
            
            if (_combatStage.SpritePositioning == null || _combatStage.SpellEffectApplier == null)
            {
                Debug.LogError("[EnemyActions] CombatStage dependencies not available within timeout period!");
                yield break;
            }
            Debug.Log("[EnemyActions] CombatStage is ready");

            // Get dependencies from CombatStage
            _spritePositioning = _combatStage.SpritePositioning as SpritePositioning;
            _cardLibrary = _combatStage.CardLibrary;
            _spellEffectApplier = _combatStage.SpellEffectApplier;
            _attackLimiter = _combatStage.GetAttackLimiter();
            Debug.Log("[EnemyActions] Got CombatStage dependencies");

            // Get EnemyDeck from CombatManager
            _enemyDeck = _combatManager.EnemyDeck;
            Debug.Log("[EnemyActions] Got EnemyDeck");
        }

        private bool ValidateCoreDependencies()
        {
            if (_combatManager == null)
            {
                Debug.LogError($"[{nameof(EnemyActions)}] Failed to find CombatManager in scene!");
                return false;
            }
            if (_spritePositioning == null)
            {
                Debug.LogError($"[{nameof(EnemyActions)}] Failed to get SpritePositioning from CombatStage!");
                return false;
            }
            if (_enemyDeck == null)
            {
                Debug.LogError($"[{nameof(EnemyActions)}] Failed to get EnemyDeck from CombatManager!");
                return false;
            }
            if (_cardLibrary == null)
            {
                Debug.LogError($"[{nameof(EnemyActions)}] Failed to get CardLibrary from CombatStage!");
                return false;
            }
            if (_combatStage == null)
            {
                Debug.LogError($"[{nameof(EnemyActions)}] Failed to find CombatStage in scene!");
                return false;
            }
            if (_spellEffectApplier == null)
            {
                Debug.LogError($"[{nameof(EnemyActions)}] Failed to get SpellEffectApplier from CombatStage!");
                return false;
            }
            return true;
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

        private bool ValidateCombatState()
        {
            return _combatManager != null &&
                   _combatStage != null &&
                   _combatManager.EnemyDeck != null;
        }

        private bool HasOngoingEffect(EntityManager target, SpellEffect effectType)
        {
            // Check if target has the specified ongoing effect
            if (_stackManager == null)
            {
                Debug.LogWarning("[EnemyActions] StackManager is null when checking effects, attempting to find it");
                _stackManager = StackManager.Instance;
                
                if (_stackManager == null)
                {
                    Debug.LogWarning("[EnemyActions] Could not find StackManager instance, effect check will return false");
                    return false;
                }
            }
            
            return _stackManager.HasEffect(target, effectType);
        }
    }
}
