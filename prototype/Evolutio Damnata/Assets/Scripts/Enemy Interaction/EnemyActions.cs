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

        private Dictionary<Keywords.MonsterKeyword, EnemyInteraction.Models.KeywordEvaluation> _keywordEvaluations;
        private Dictionary<SpellEffect, EnemyInteraction.Models.SpellEffectEvaluation> _effectEvaluations;

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
            float initializationTimer = 0f;

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

            // Try to get AIServices, but don't wait indefinitely
            AIServices services = null;
            float aiServicesTimeout = 3f;
            float aiServicesTimer = 0f;
            
            while (AIServices.Instance == null && aiServicesTimer < aiServicesTimeout)
            {
                Debug.Log("[EnemyActions] Waiting for AIServices to be initialized...");
                yield return new WaitForSeconds(_componentWaitTime);
                aiServicesTimer += _componentWaitTime;
                initializationTimer += _componentWaitTime;
                
                if (initializationTimer >= _maxInitializationTime)
                {
                    Debug.LogWarning("[EnemyActions] Max initialization time reached while waiting for AIServices!");
                    break;
                }
            }
            
            // Get services from AIServices if available
            if (AIServices.Instance != null)
            {
                Debug.Log("[EnemyActions] AIServices is ready, getting dependencies");
                services = AIServices.Instance;
                
                // Get BoardStateManager but don't wait indefinitely
                _boardStateManager = services.BoardStateManager;
                float boardStateTimer = 0f;
                float boardStateTimeout = 2f;
                
                while (_boardStateManager != null && !_boardStateManager.IsInitialized && boardStateTimer < boardStateTimeout)
                {
                    Debug.Log("[EnemyActions] Waiting for BoardStateManager to be initialized...");
                    yield return new WaitForSeconds(_componentWaitTime);
                    boardStateTimer += _componentWaitTime;
                    initializationTimer += _componentWaitTime;
                    
                    if (initializationTimer >= _maxInitializationTime)
                    {
                        Debug.LogWarning("[EnemyActions] Max initialization time reached while waiting for BoardStateManager!");
                        break;
                    }
                }
                
                // Get other services without waiting
                _keywordEvaluator = services.KeywordEvaluator;
                _effectEvaluator = services.EffectEvaluator;
                _cardPlayManager = services.CardPlayManager;
                _attackManager = services.AttackManager;
                
                Debug.Log("[EnemyActions] Got available services from AIServices");
            }
            else
            {
                Debug.LogWarning("[EnemyActions] AIServices not available, will create local services when needed");
            }

            // Initialize evaluations regardless of AIServices availability
            InitializeEvaluations();
            
            // Mark as initialized - we'll create fallback services if needed during gameplay
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

        private void InitializeEvaluations()
        {
            _keywordEvaluations = new Dictionary<Keywords.MonsterKeyword, EnemyInteraction.Models.KeywordEvaluation>
            {
                {
                    Keywords.MonsterKeyword.Taunt,
                    new EnemyInteraction.Models.KeywordEvaluation
                    {
                        BaseScore = 40f,
                        IsPositive = true,
                        IsDefensive = true,
                        IsOffensive = false,
                        RequiresTarget = false
                    }
                },
                {
                    Keywords.MonsterKeyword.Ranged,
                    new EnemyInteraction.Models.KeywordEvaluation
                    {
                        BaseScore = 30f,
                        IsPositive = true,
                        IsDefensive = false,
                        IsOffensive = true,
                        RequiresTarget = false
                    }
                }
                // New keywords can be added here
            };

            _effectEvaluations = new Dictionary<SpellEffect, EnemyInteraction.Models.SpellEffectEvaluation>
            {
                {
                    SpellEffect.Damage,
                    new EnemyInteraction.Models.SpellEffectEvaluation
                    {
                        BaseScore = 35f,
                        IsPositive = false,
                        IsStackable = false,
                        RequiresTarget = true,
                        IsDamaging = true
                    }
                },
                {
                    SpellEffect.Burn,
                    new EnemyInteraction.Models.SpellEffectEvaluation
                    {
                        BaseScore = 30f,
                        IsPositive = false,
                        IsStackable = true,
                        RequiresTarget = true,
                        IsDamaging = true
                    }
                },
                {
                    SpellEffect.Heal,
                    new EnemyInteraction.Models.SpellEffectEvaluation
                    {
                        BaseScore = 25f,
                        IsPositive = true,
                        IsStackable = false,
                        RequiresTarget = true,
                        IsDamaging = false
                    }
                }
                // New effects can be added here
            };
        }

        private float EvaluateKeyword(Keywords.MonsterKeyword keyword, bool isOwnCard, BoardState boardState)
        {
            if (!_keywordEvaluations.ContainsKey(keyword))
            {
                Debug.LogWarning($"Unknown keyword: {keyword}. Add it to _keywordEvaluations for proper AI handling.");
                return 0f;
            }

            var evaluation = _keywordEvaluations[keyword];
            float score = evaluation.BaseScore;

            // Adjust score based on game state
            if (isOwnCard)
            {
                // When playing our own cards
                if (boardState.HealthAdvantage < 0)
                {
                    // We're behind in health
                    if (evaluation.IsDefensive) score *= 1.5f;
                    if (evaluation.IsOffensive) score *= 0.8f;
                }
                else
                {
                    // We're ahead in health
                    if (evaluation.IsOffensive) score *= 1.3f;
                }
            }
            else
            {
                // When evaluating enemy cards
                score *= evaluation.IsPositive ? -1 : 1; // Invert score for negative keywords
            }

            return score;
        }

        private float EvaluateEffect(SpellEffect effect, bool isOwnCard, EntityManager target, BoardState boardState)
        {
            if (!_effectEvaluations.ContainsKey(effect))
            {
                Debug.LogWarning($"Unknown effect: {effect}. Add it to _effectEvaluations for proper AI handling.");
                return 0f;
            }

            var evaluation = _effectEvaluations[effect];
            float score = evaluation.BaseScore;

            // Adjust score based on game state and target
            if (isOwnCard)
            {
                if (evaluation.IsStackable && HasOngoingEffect(target, effect))
                {
                    score *= 1.4f; // Bonus for stacking effects
                }

                if (evaluation.IsDamaging)
                {
                    float damageRatio = target.GetHealth() / target.GetMaxHealth();
                    if (damageRatio < 0.5f) score *= 1.3f; // Bonus for targeting low health
                }

                if (boardState.HealthAdvantage < 0)
                {
                    // We're behind in health
                    if (!evaluation.IsDamaging) score *= 1.2f; // Prefer defensive effects
                }
            }
            else
            {
                // When evaluating enemy effects
                score *= evaluation.IsPositive ? -1 : 1; // Invert score for negative effects
            }

            return score;
        }

        public IEnumerator PlayCards()
        {
            Debug.Log("[EnemyActions] Starting PlayCards");
            
            // Wait for initialization if needed but with timeout
            if (!_isInitialized)
            {
                float waitTime = 0f;
                float timeout = 3f;
                while (!_isInitialized && waitTime < timeout)
                {
                    Debug.Log("[EnemyActions] Waiting for initialization to complete...");
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }
            }
            
            // Create local CardPlayManager if the one from AIServices is null
            if (_cardPlayManager == null)
            {
                Debug.LogWarning("[EnemyActions] CardPlayManager is null, creating a local instance");
                var cardPlayManagerObj = new GameObject("CardPlayManager_Local");
                cardPlayManagerObj.transform.SetParent(transform);
                
                var localCardPlayManager = cardPlayManagerObj.AddComponent<CardPlayManager>();
                if (localCardPlayManager != null)
                {
                    // Set up CardPlayManager with necessary references
                    if (_spritePositioning != null)
                    {
                        var cardPlayManagerField = localCardPlayManager.GetType().GetField("_spritePositioning", 
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (cardPlayManagerField != null)
                            cardPlayManagerField.SetValue(localCardPlayManager, _spritePositioning);
                    }
                    
                    _cardPlayManager = localCardPlayManager;
                    
                    // Wait for initialization
                    int attempts = 0;
                    while (!localCardPlayManager.enabled && attempts < 50)
                    {
                        yield return new WaitForSeconds(0.1f);
                        attempts++;
                    }
                    
                    if (attempts >= 50)
                    {
                        Debug.LogError("[EnemyActions] Failed to initialize local CardPlayManager");
                        yield break;
                    }
                    
                    Debug.Log("[EnemyActions] Created and initialized local CardPlayManager");
                }
                else
                {
                    Debug.LogError("[EnemyActions] Failed to create local CardPlayManager!");
                    yield break;
                }
            }
            
            // Still null after attempted creation
            if (_cardPlayManager == null)
            {
                Debug.LogError("[EnemyActions] CardPlayManager is null in PlayCards!");
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
                float waitTime = 0f;
                float timeout = 3f;
                while (!_isInitialized && waitTime < timeout)
                {
                    Debug.Log("[EnemyActions] Waiting for initialization to complete...");
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }
            }
            
            // Create local AttackManager if the one from AIServices is null
            if (_attackManager == null)
            {
                Debug.LogWarning("[EnemyActions] AttackManager is null, creating a local instance");
                var attackManagerObj = new GameObject("AttackManager_Local");
                attackManagerObj.transform.SetParent(transform);
                
                var localAttackManager = attackManagerObj.AddComponent<AttackManager>();
                if (localAttackManager != null)
                {
                    // Set up AttackManager with necessary references
                    if (_spritePositioning != null)
                    {
                        var attackManagerField = localAttackManager.GetType().GetField("_spritePositioning", 
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (attackManagerField != null)
                            attackManagerField.SetValue(localAttackManager, _spritePositioning);
                    }
                    
                    _attackManager = localAttackManager;
                    
                    // Wait for initialization
                    int attempts = 0;
                    while (!localAttackManager.enabled && attempts < 50)
                    {
                        yield return new WaitForSeconds(0.1f);
                        attempts++;
                    }
                    
                    if (attempts >= 50)
                    {
                        Debug.LogError("[EnemyActions] Failed to initialize local AttackManager");
                        yield break;
                    }
                    
                    Debug.Log("[EnemyActions] Created and initialized local AttackManager");
                }
                else
                {
                    Debug.LogError("[EnemyActions] Failed to create local AttackManager!");
                    yield break;
                }
            }
            
            // Still null after attempted creation
            if (_attackManager == null)
            {
                Debug.LogError("[EnemyActions] AttackManager is null in Attack!");
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

        private bool IsPlayableCard(Card card)
        {
            if (card == null || !_combatManager.EnemyDeck.Hand.Contains(card))
            {
                Debug.Log("Card is null or not in hand");
                return false;
            }

            if (card.CardType == null)
            {
                Debug.Log($"Card {card.CardName} has no CardType");
                return false;
            }

            if (_combatManager.EnemyMana < card.CardType.ManaCost)
            {
                Debug.Log($"Not enough mana to play card {card.CardName}. Required: {card.CardType.ManaCost}, Available: {_combatManager.EnemyMana}");
                return false;
            }

            // For monster cards, check available positions
            if (card.CardType.IsMonsterCard)
            {
                bool hasAvailablePosition = _spritePositioning.EnemyEntities
                    .Any(entity => entity != null && 
                         entity.GetComponent<EntityManager>() != null && 
                         !entity.GetComponent<EntityManager>().placed);

                if (!hasAvailablePosition)
                {
                    Debug.Log("No available positions on the board");
                    return false;
                }
            }
            // For spell cards, check if there are valid targets
            else if (card.CardType.IsSpellCard)
            {
                if (!HasValidSpellTargets(card.CardType))
                {
                    Debug.Log($"No valid targets for spell {card.CardName}");
                    return false;
                }
            }

            return true;
        }

        private bool HasValidSpellTargets(CardData spellData)
        {
            // Get all potential targets (player monsters and health icon)
            var playerMonsters = _spritePositioning.PlayerEntities
                .Where(entity => entity != null)
                .Select(entity => entity.GetComponent<EntityManager>())
                .Where(entity => entity != null && entity.placed && !entity.dead)
                .ToList();

            var playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();

            // Check if spell can target any of these
            foreach (var effectType in spellData.EffectTypes)
            {
                switch (effectType)
                {
                    case SpellEffect.Damage:
                    case SpellEffect.Burn:
                        // Can target either monsters or health icon
                        if (playerMonsters.Count > 0 || playerHealthIcon != null)
                            return true;
                        break;

                    case SpellEffect.Heal:
                        // Can only target friendly monsters
                        var friendlyMonsters = _spritePositioning.EnemyEntities
                            .Where(entity => entity != null)
                            .Select(entity => entity.GetComponent<EntityManager>())
                            .Where(entity => entity != null && entity.placed && !entity.dead)
                            .ToList();
                        
                        if (friendlyMonsters.Count > 0)
                            return true;
                        break;
                }
            }

            return false;
        }

        private EntityManager GetBestSpellTarget(CardData spellData)
        {
            var playerMonsters = _spritePositioning.PlayerEntities
                .Where(entity => entity != null)
                .Select(entity => entity.GetComponent<EntityManager>())
                .Where(entity => entity != null && entity.placed && !entity.dead)
                .ToList();

            var playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
            var friendlyMonsters = _spritePositioning.EnemyEntities
                .Where(entity => entity != null)
                .Select(entity => entity.GetComponent<EntityManager>())
                .Where(entity => entity != null && entity.placed && !entity.dead)
                .ToList();

            EntityManager bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var effectType in spellData.EffectTypes)
            {
                switch (effectType)
                {
                    case SpellEffect.Damage:
                    case SpellEffect.Burn:
                        // Evaluate damage targets
                        foreach (var monster in playerMonsters)
                        {
                            float score = EvaluateDamageTarget(monster, spellData);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestTarget = monster;
                            }
                        }

                        // Also consider player health icon
                        if (playerHealthIcon != null)
                        {
                            float healthScore = EvaluateHealthIconTarget(playerHealthIcon, spellData);
                            if (healthScore > bestScore)
                            {
                                bestScore = healthScore;
                                bestTarget = playerHealthIcon;
                            }
                        }
                        break;

                    case SpellEffect.Heal:
                        // Evaluate healing targets
                        foreach (var monster in friendlyMonsters)
                        {
                            float score = EvaluateHealTarget(monster, spellData);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestTarget = monster;
                            }
                        }
                        break;
                }
            }

            return bestTarget;
        }

        private float EvaluateDamageTarget(EntityManager target, CardData spellData)
        {
            float score = 0f;

            // Base score is the ratio of damage to target's health
            float healthRatio = (float)spellData.EffectValue / target.GetHealth();
            score += healthRatio * 100f; // Scale up for better comparison

            // Consider keywords with consistent scoring
            if (target.HasKeyword(Keywords.MonsterKeyword.Taunt))
                score += 50f; // High priority for taunt units as they must be attacked
            else if (target.HasKeyword(Keywords.MonsterKeyword.Ranged))
                score += 30f; // Medium priority for ranged units as they're valuable
            // No need to handle None keyword as it's the default case

            // Consider spell effects with consistent scoring
            if (spellData.EffectTypes.Contains(SpellEffect.Burn))
            {
                if (HasOngoingEffect(target, SpellEffect.Burn))
                    score += 40f; // High bonus for stacking burn
            }
            else if (spellData.EffectTypes.Contains(SpellEffect.Damage))
            {
                // If target is already damaged, prefer to finish them off
                float currentHealthRatio = target.GetHealth() / target.GetMaxHealth();
                if (currentHealthRatio < 0.7f) // If below 70% health
                {
                    score += 35f;
                    // If we can kill with this damage, give a big bonus
                    if (spellData.EffectValue >= target.GetHealth())
                        score += 80f;
                }
            }
            // No need to handle None effect as it's the default case

            // Consider if target has high attack power (prioritize removing threats)
            float attackRatio = target.GetAttackPower() / target.GetHealth();
            if (attackRatio > 1.2f) // If attack power is higher than health
                score += 30f;

            // Consider timing - if it's early in the game, prefer to spread damage
            // If it's late in the game, prefer to focus fire
            float targetHealthRatio = target.GetHealth() / target.GetMaxHealth();
            
            if (_combatManager.TurnCount > 5) // Late game
            {
                if (targetHealthRatio < 0.6f) // Already damaged targets
                    score += 25f;
            }
            else // Early game
            {
                if (targetHealthRatio > 0.8f) // Fresh targets
                    score += 20f;
            }

            return score;
        }

        private float EvaluateHealthIconTarget(HealthIconManager target, CardData spellData)
        {
            float score = 0f;

            // Base score is the ratio of damage to target's health
            float healthRatio = (float)spellData.EffectValue / target.GetHealth();
            score += healthRatio * 100f;

            // Consider if there are no taunt units
            if (!CombatRulesEngine.HasTauntUnits(_spritePositioning.PlayerEntities))
                score += 40f; // More likely to target health if no taunts

            return score;
        }

        private float EvaluateHealTarget(EntityManager target, CardData spellData)
        {
            float score = 0f;

            // Base score is how much healing would be wasted (lower is better)
            float missingHealth = target.GetMaxHealth() - target.GetHealth();
            float healingWaste = Mathf.Max(0, spellData.EffectValue - missingHealth);
            score -= healingWaste * 2f;

            // Consider keywords
            if (target.HasKeyword(Keywords.MonsterKeyword.Taunt))
                score += 30f; // Prioritize healing taunt units

            if (target.HasKeyword(Keywords.MonsterKeyword.Ranged))
                score += 15f; // Slightly prefer healing ranged units

            return score;
        }

        private bool HasOngoingEffect(EntityManager target, SpellEffect effectType)
        {
            // Check if target has the specified ongoing effect
            return StackManager.Instance?.HasEffect(target, effectType) ?? false;
        }

        private BoardState EvaluateBoardState()
        {
            var state = new BoardState
            {
                EnemyMonsters = _spritePositioning.EnemyEntities
                    .Where(entity => entity != null)
                    .Select(entity => entity.GetComponent<EntityManager>())
                    .Where(entity => entity != null && entity.placed && !entity.dead)
                    .ToList(),
                PlayerMonsters = _spritePositioning.PlayerEntities
                    .Where(entity => entity != null)
                    .Select(entity => entity.GetComponent<EntityManager>())
                    .Where(entity => entity != null && entity.placed && !entity.dead)
                    .ToList(),
                EnemyHealth = _combatManager.EnemyHealth,
                PlayerHealth = _combatManager.PlayerHealth,
                TurnCount = _combatManager.TurnCount,
                EnemyMana = _combatManager.EnemyMana
            };

            // Calculate board control metrics
            state.EnemyBoardControl = CalculateBoardControl(state.EnemyMonsters);
            state.PlayerBoardControl = CalculateBoardControl(state.PlayerMonsters);
            
            // Add health icon considerations to board control
            state.EnemyBoardControl += state.EnemyHealth * 0.2f; // Health is worth 20% of its value in board control
            state.PlayerBoardControl += state.PlayerHealth * 0.2f;
            
            state.BoardControlDifference = state.EnemyBoardControl - state.PlayerBoardControl;
            
            // Calculate health advantage
            state.HealthAdvantage = state.EnemyHealth - state.PlayerHealth;
            state.HealthRatio = (float)state.EnemyHealth / state.PlayerHealth;

            return state;
        }

        private float CalculateBoardControl(List<EntityManager> monsters)
        {
            float control = 0f;
            foreach (var monster in monsters)
            {
                // Base control from attack and health
                control += monster.GetAttackPower() * 0.5f;
                control += monster.GetHealth() * 0.3f;

                // Bonus for keywords
                if (monster.HasKeyword(Keywords.MonsterKeyword.Taunt))
                    control += 2f;
                if (monster.HasKeyword(Keywords.MonsterKeyword.Ranged))
                    control += 1.5f;
            }
            return control;
        }

        private float EvaluateCardPlay(Card card, BoardState boardState)
        {
            float score = 0f;

            // Base score from mana efficiency
            float manaEfficiency = card.CardType.ManaCost / (float)_combatManager.EnemyMana;
            score += (1 - manaEfficiency) * 50f;

            if (card.CardType.IsMonsterCard)
            {
                score += EvaluateMonsterCard(card, boardState);
            }
            else
            {
                score += EvaluateSpellCard(card, boardState);
            }

            // Consider health advantage when playing cards
            if (boardState.HealthAdvantage < 0) // If we're behind in health
            {
                // More likely to play defensive cards when behind in health
                if (card.CardType.HasKeyword(Keywords.MonsterKeyword.Taunt))
                    score += 30f;
                if (card.CardType.EffectTypes.Contains(SpellEffect.Heal))
                    score += 40f;
            }
            else if (boardState.HealthAdvantage > 0) // If we're ahead in health
            {
                // More likely to play aggressive cards when ahead in health
                if (card.CardType.AttackPower > 0)
                    score += 20f;
                if (card.CardType.EffectTypes.Contains(SpellEffect.Damage))
                    score += 30f;
            }

            return score;
        }

        private float EvaluateMonsterCard(Card card, BoardState boardState)
        {
            float score = 0f;

            // Consider card's stats
            score += card.CardType.AttackPower * 0.8f;
            score += card.CardType.Health * 0.6f;

            // Consider all keywords using the evaluation system
            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (card.CardType.HasKeyword(keyword))
                {
                    score += EvaluateKeyword(keyword, true, boardState);
                }
            }

            // Consider board state
            if (boardState.BoardControlDifference < 0)
                score += 25f;

            // Consider mana curve
            if (boardState.TurnCount <= 3 && card.CardType.ManaCost <= 3)
                score += 15f;

            return score;
        }

        private float EvaluateSpellCard(Card card, BoardState boardState)
        {
            float score = 0f;

            // Consider all effects using the evaluation system
            foreach (var effect in card.CardType.EffectTypes)
            {
                // Get best target for this effect
                var target = GetBestTargetForEffect(effect, boardState);
                if (target != null)
                {
                    score += EvaluateEffect(effect, true, target, boardState);
                }
            }

            return score;
        }

        private EntityManager GetBestTargetForEffect(SpellEffect effect, BoardState boardState)
        {
            if (!_effectEvaluations.ContainsKey(effect))
                return null;

            var evaluation = _effectEvaluations[effect];
            
            if (evaluation.IsPositive)
            {
                // Target own units with positive effects
                return boardState.EnemyMonsters
                    .Where(m => m != null && !m.dead)
                    .OrderBy(m => m.GetHealth() / m.GetMaxHealth())
                    .FirstOrDefault();
            }
            else
            {
                // Target enemy units with negative effects
                return boardState.PlayerMonsters
                    .Where(m => m != null && !m.dead)
                    .OrderByDescending(m => EvaluateTargetThreat(m, boardState))
                    .FirstOrDefault();
            }
        }

        private float EvaluateTargetThreat(EntityManager target, BoardState boardState)
        {
            float threat = 0f;
            
            // Base threat from stats
            threat += target.GetAttackPower() * 1.2f;
            threat += target.GetHealth() * 0.8f;
            
            // Consider keywords
            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (target.HasKeyword(keyword))
                {
                    threat += EvaluateKeyword(keyword, false, boardState);
                }
            }
            
            return threat;
        }

        private int FindBestMonsterPosition(Card card, BoardState boardState)
        {
            // Find first available position
            for (int i = 0; i < _spritePositioning.EnemyEntities.Count; i++)
            {
                if (_spritePositioning.EnemyEntities[i] == null) continue;
                
                var entity = _spritePositioning.EnemyEntities[i].GetComponent<EntityManager>();
                if (entity != null && !entity.placed)
                {
                    return i;
                }
            }
            return -1;
        }

        private class BoardState
        {
            public List<EntityManager> EnemyMonsters { get; set; }
            public List<EntityManager> PlayerMonsters { get; set; }
            public int EnemyHealth { get; set; }
            public int PlayerHealth { get; set; }
            public int TurnCount { get; set; }
            public int EnemyMana { get; set; }
            public float EnemyBoardControl { get; set; }
            public float PlayerBoardControl { get; set; }
            public float BoardControlDifference { get; set; }
            public int HealthAdvantage { get; set; }
            public float HealthRatio { get; set; }
        }
    }
}
