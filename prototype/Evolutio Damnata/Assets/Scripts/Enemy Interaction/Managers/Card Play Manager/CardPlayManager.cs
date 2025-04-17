using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers.Evaluation;
using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Managers.Execution;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;
using EnemyInteraction.Utilities;

namespace EnemyInteraction.Managers
{
    public class CardPlayManager : MonoBehaviour, ICardPlayManager
    {
        [SerializeField] private SpritePositioning _spritePositioning;
        private ICombatManager _combatManager;
        private CombatStage _combatStage;
        private IKeywordEvaluator _keywordEvaluator;
        private IEffectEvaluator _effectEvaluator;
        private IBoardStateManager _boardStateManager;
        private ISpellEffectApplier _spellEffectApplier;

        [Header("Card Evaluation Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance to make intentionally suboptimal plays")]
        private float _suboptimalPlayChance = 0.10f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Variance in card evaluation scores")]
        private float _evaluationVariance = 0.15f;

        [SerializeField, Range(0.2f, 2f), Tooltip("Delay between enemy actions in seconds")]
        private float _actionDelay = 0.5f;

        // Card skip settings
        [Header("Card Play Skip Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Base chance to consider skipping card plays")]
        private float _skipCardPlayChance = 0.15f;

        [SerializeField, Tooltip("Board advantage threshold where AI might hold cards")]
        private float _cardHoldBoardAdvantageThreshold = 1.3f;

        [SerializeField, Range(0f, 1f), Tooltip("Value multiplier for cards held for future turns")]
        private float _futureValueMultiplier = 0.7f;

        [Header("Strategic Gameplay Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance to stop playing cards when in advantageous position")]
        private float _strategicStopChance = 0.3f;

        [SerializeField, Range(1f, 3f), Tooltip("Minimum board advantage ratio to consider stopping early")]
        private float _earlyStopBoardAdvantageThreshold = 1.2f;

        [SerializeField, Range(0f, 100f), Tooltip("Score threshold below which cards are considered low value")]
        private float _lowValueCardThreshold = 60f;

        [SerializeField, Range(0f, 100f), Tooltip("Score threshold above which cards are considered high value")]
        private float _highValueCardThreshold = 70f;

        [SerializeField, Range(3, 15), Tooltip("Health threshold at which player is considered at low health")]
        private int _playerLowHealthThreshold = 10;

        [SerializeField, Range(0f, 2f), Tooltip("Future value multiplier for expensive cards in early game")]
        private float _earlyGameExpensiveCardMultiplier = 1.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Chance to hold expensive cards for future turns")]
        private float _holdExpensiveCardChance = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("Chance to hold cards with high future value")]
        private float _holdHighFutureValueChance = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Factor for comparing future value to current value")]
        private float _futureToCurrentValueRatio = 0.7f;

        [Header("Initialization Settings")]
        [SerializeField, Range(5, 60), Tooltip("Maximum attempts when initializing critical components")]
        private int _maxInitializationAttempts = 30;

        [SerializeField, Range(0.05f, 1f), Tooltip("Delay between initialization attempts in seconds")]
        private float _initializationRetryDelay = 0.1f;

        [SerializeField, Range(1, 20), Tooltip("Minimum deck size to consider card conservation strategies")]
        private int _lowDeckSizeThreshold = 10;

        [SerializeField, Range(0f, 1f), Tooltip("Chance to be selective with cards when deck size is low")]
        private float _lowDeckSizeConservationChance = 0.4f;

        private Dictionary<GameObject, EntityManager> _entityCache;

        // New components
        private ICardEvaluator _cardEvaluator;
        private MonsterPositionSelector _monsterPositionSelector;
        private SpellTargetSelector _spellTargetSelector;
        private ICardPlayExecutor _cardPlayExecutor;

        private void Awake()
        {
            _entityCache = new Dictionary<GameObject, EntityManager>();
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[CardPlayManager] Initializing...");

            yield return InitializeCriticalComponents();
            yield return InitializeOptionalServices();

            InitializeCardPlayComponents();
            BuildEntityCache();
            Debug.Log("[CardPlayManager] Initialization complete");
        }

        private void InitializeCardPlayComponents()
        {
            // Create our specialized components
            _cardEvaluator = new CardEvaluator(
                _combatManager,
                _keywordEvaluator,
                _effectEvaluator,
                _suboptimalPlayChance,
                _evaluationVariance);

            _monsterPositionSelector = new MonsterPositionSelector(
                _spritePositioning,
                _entityCache);

            _spellTargetSelector = new SpellTargetSelector(
                _spritePositioning,
                _entityCache);

            _cardPlayExecutor = new CardPlayExecutor(
                _combatManager,
                _combatStage,
                _spellEffectApplier,
                _monsterPositionSelector,
                _spellTargetSelector,
                _actionDelay);
        }

        private IEnumerator InitializeCriticalComponents()
        {
            int attempts = 0;

            while (attempts < _maxInitializationAttempts)
            {
                _combatManager ??= FindObjectOfType<CombatManager>();
                _combatStage ??= FindObjectOfType<CombatStage>();

                if (_combatManager != null && _combatStage != null) break;

                yield return new WaitForSeconds(_initializationRetryDelay);
                attempts++;
            }

            // Log error if components weren't found after max attempts
            if (_combatManager == null)
            {
                Debug.LogError("[CardPlayManager] Failed to initialize CombatManager after maximum attempts");
            }

            if (_combatStage == null)
            {
                Debug.LogError("[CardPlayManager] Failed to initialize CombatStage after maximum attempts");
            }
            else
            {
                yield return InitializeCombatStageDependencies();
            }
        }

        private IEnumerator InitializeCombatStageDependencies()
        {
            int attempts = 0;

            while ((_combatStage.SpritePositioning == null || _combatStage.SpellEffectApplier == null) &&
                   attempts < _maxInitializationAttempts)
            {
                yield return new WaitForSeconds(_initializationRetryDelay);
                attempts++;
            }

            _spritePositioning ??= _combatStage.SpritePositioning as SpritePositioning;
            _spellEffectApplier ??= _combatStage.SpellEffectApplier;
        }

        private IEnumerator InitializeOptionalServices()
        {
            yield return InitializeAIServices();
            InitializeFallbackServices();
        }

        private IEnumerator InitializeAIServices()
        {
            int attempts = 0;

            while (AIServices.Instance == null && attempts < _maxInitializationAttempts)
            {
                yield return new WaitForSeconds(_initializationRetryDelay);
                attempts++;
            }

            if (AIServices.Instance != null)
            {
                var services = AIServices.Instance;
                _keywordEvaluator ??= services.KeywordEvaluator;
                _effectEvaluator ??= services.EffectEvaluator;
                _boardStateManager ??= services.BoardStateManager;
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
                Debug.LogWarning($"[CardPlayManager] Error accessing BoardStateManager.Instance: {ex.Message}");
            }

            // Use the singleton if found
            if (existingManager != null)
            {
                _boardStateManager = existingManager;
            }

            // Only create local services if absolutely necessary
            _keywordEvaluator ??= CreateLocalService<KeywordEvaluator>("KeywordEvaluator_Local");
            _effectEvaluator ??= CreateLocalService<EffectEvaluator>("EffectEvaluator_Local");

            // Only create a local BoardStateManager if no singleton exists
            if (_boardStateManager == null)
            {
                Debug.LogWarning("[CardPlayManager] No BoardStateManager singleton found, creating local instance");
                _boardStateManager = CreateLocalService<BoardStateManager>("BoardStateManager_Local");
            }
        }

        private T CreateLocalService<T>(string name) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            return obj.AddComponent<T>();
        }

        private void BuildEntityCache()
        {
            _entityCache.Clear();
            if (_spritePositioning == null) return;

            foreach (var entity in _spritePositioning.EnemyEntities.Concat(_spritePositioning.PlayerEntities))
            {
                if (entity != null && !_entityCache.ContainsKey(entity))
                {
                    _entityCache[entity] = entity.GetComponent<EntityManager>();
                }
            }
        }

        public IEnumerator PlayCards()
        {
            // Initial delay before starting actions
            yield return new WaitForSeconds(_actionDelay);
            Debug.Log("[CardPlayManager] Starting card play sequence...");

            if (!IsValidPlayState)
            {
                yield return SimulatePlaceholderAction();
                yield break;
            }

            var enemyDeck = _combatManager.EnemyDeck;
            if (enemyDeck == null || enemyDeck.Hand == null || enemyDeck.Hand.Count == 0)
            {
                Debug.Log("[CardPlayManager] No cards in hand to play");
                yield break;
            }

            var playableCards = _cardEvaluator.GetPlayableCards(enemyDeck.Hand);
            if (playableCards.Count == 0)
            {
                Debug.Log("[CardPlayManager] No playable cards found");
                yield break;
            }

            // Add delay before evaluating board state
            yield return new WaitForSeconds(_actionDelay);

            // Get current board state for decision making
            var boardState = GetCurrentBoardState();

            // Check if we should skip playing cards this turn
            if (ShouldSkipCardPlay(playableCards, boardState))
            {
                Debug.Log("[CardPlayManager] AI decided to hold cards for strategic reasons");
                yield return new WaitForSeconds(_actionDelay * 1.5f);
                yield break;
            }

            // Determine the optimal cards to play and their order
            var cardsToPlay = GetOptimalCardsToPlay(playableCards, boardState);
            if (cardsToPlay.Count == 0)
            {
                Debug.Log("[CardPlayManager] AI decided to hold all cards after evaluation");
                yield return new WaitForSeconds(_actionDelay);
                yield break;
            }

            yield return _cardPlayExecutor.PlayCardsInOrder(cardsToPlay, enemyDeck, boardState);

            // Final delay after all cards are played
            yield return new WaitForSeconds(_actionDelay);
            Debug.Log("[CardPlayManager] Completed playing cards");
        }

        /// <summary>
        /// Determines if the AI should skip playing any cards this turn based on
        /// strategic considerations of the board state and future potential.
        /// </summary>
        private bool ShouldSkipCardPlay(List<Card> playableCards, BoardState boardState)
        {
            // First, consider basic random chance
            if (Random.value > _skipCardPlayChance)
                return false;

            Debug.Log("[CardPlayManager] Considering whether to skip playing cards...");

            // Skip if board state is unavailable
            if (boardState == null)
                return false;

            // Calculate our board advantage
            float enemyBoardAdvantage = boardState.EnemyBoardControl /
                (boardState.PlayerBoardControl > 0 ? boardState.PlayerBoardControl : 1);

            // Check if we have sufficient board advantage to consider skipping
            bool hasBoardAdvantage = enemyBoardAdvantage >= _cardHoldBoardAdvantageThreshold;

            // Check if we're in a defensive or late game position
            bool isLateGame = boardState.TurnCount >= 5;
            bool isDefensive = boardState.EnemyHealth < boardState.PlayerHealth;
            bool playerLowHealth = boardState.PlayerHealth <= _playerLowHealthThreshold;

            // Don't skip if player is at critically low health - press the advantage
            if (playerLowHealth)
            {
                Debug.Log("[CardPlayManager] Won't skip - player health is low, pressing advantage");
                return false;
            }

            // More likely to skip in early game if we have board advantage
            if (!isLateGame && hasBoardAdvantage)
            {
                // In early game with board advantage, high chance to skip
                float skipChance = 0.7f;
                bool shouldSkip = Random.value < skipChance;

                if (shouldSkip)
                {
                    Debug.Log($"[CardPlayManager] Skipping card play - early game with board advantage of {enemyBoardAdvantage:F2}");
                    return true;
                }
            }

            // Consider skipping if we see only low-value cards
            float averageCardValue = playableCards.Average(c => _cardEvaluator.EvaluateCardPlay(c, boardState));
            if (averageCardValue < _lowValueCardThreshold && hasBoardAdvantage)
            {
                Debug.Log($"[CardPlayManager] Skipping card play - cards have low average value ({averageCardValue:F2})");
                return true;
            }

            // Always play cards in late game unless we have overwhelming advantage
            if (isLateGame && enemyBoardAdvantage < 2.0f)
            {
                Debug.Log("[CardPlayManager] Won't skip - late game requires playing available cards");
                return false;
            }

            // Consider card conservation based on deck size and hand quality
            if (boardState.EnemyDeck != null && boardState.EnemyDeckSize < _lowDeckSizeThreshold)
            {
                // If we're running low on cards, be more strategic about using them
                Debug.Log("[CardPlayManager] Low on cards in deck, being more selective");
                return Random.value < _lowDeckSizeConservationChance;
            }

            Debug.Log("[CardPlayManager] Decided not to skip playing cards this turn");
            return false;
        }

        /// <summary>
        /// Gets the optimal subset of cards to play this turn, considering future value
        /// </summary>
        private List<Card> GetOptimalCardsToPlay(List<Card> playableCards, BoardState boardState)
        {
            // First, evaluate and rank all playable cards
            var evaluatedCards = playableCards
                .Select(card => new
                {
                    Card = card,
                    Score = _cardEvaluator.EvaluateCardPlay(card, boardState),
                    // Higher mana cost cards generally have higher strategic value
                    FutureValue = card.CardType.ManaCost * _futureValueMultiplier *
                                 (boardState.TurnCount < 3 ? _earlyGameExpensiveCardMultiplier : 1.0f)
                })
                .OrderByDescending(c => c.Score)
                .ToList();

            var selectedCards = new List<Card>();
            int remainingMana = _combatManager.EnemyMana;

            // Process cards in order of their value
            foreach (var cardData in evaluatedCards)
            {
                // If we don't have enough mana for this card, skip it
                if (cardData.Card.CardType.ManaCost > remainingMana)
                    continue;

                // For high-cost cards in early game, consider if they're worth playing now
                bool isExpensiveCard = cardData.Card.CardType.ManaCost >= 4;
                bool isEarlyGame = boardState.TurnCount <= 3;

                if (isExpensiveCard && isEarlyGame)
                {
                    // In early game, consider holding expensive cards for future turns if:
                    // 1. Their immediate score isn't very high
                    // 2. We have board advantage
                    // 3. Random chance factor for decision variance

                    float boardAdvantage = boardState.EnemyBoardControl /
                        (boardState.PlayerBoardControl > 0 ? boardState.PlayerBoardControl : 1);

                    if (boardAdvantage > _earlyStopBoardAdvantageThreshold &&
                        cardData.Score < _highValueCardThreshold &&
                        Random.value < _holdExpensiveCardChance)
                    {
                        Debug.Log($"[CardPlayManager] Holding expensive card '{cardData.Card.CardName}' for future turns");
                        continue;
                    }
                }

                // If a card has higher future value than current value and we're in a good position,
                // consider holding it for later
                if (cardData.FutureValue > cardData.Score * _futureToCurrentValueRatio &&
                    boardState.EnemyBoardControl > boardState.PlayerBoardControl)
                {
                    // Apply randomness to the decision
                    if (Random.value < _holdHighFutureValueChance)
                    {
                        Debug.Log($"[CardPlayManager] Holding card '{cardData.Card.CardName}' for higher future value");
                        continue;
                    }
                }

                // Add this card to our play selection and deduct its mana cost
                selectedCards.Add(cardData.Card);
                remainingMana -= cardData.Card.CardType.ManaCost;

                // If we're out of mana, stop playing cards
                if (remainingMana <= 0)
                {
                    break;
                }

                // Only consider stopping after 2+ cards if:
                // 1. The remaining cards have low value
                // 2. We have a reasonable board advantage
                if (selectedCards.Count >= 2)
                {
                    float boardAdvantage = boardState.EnemyBoardControl /
                        (boardState.PlayerBoardControl > 0 ? boardState.PlayerBoardControl : 1);

                    bool hasGoodAdvantage = boardAdvantage > _earlyStopBoardAdvantageThreshold;
                    bool remainingCardsLowValue = evaluatedCards
                        .Where(c => c.Card.CardType.ManaCost <= remainingMana)
                        .All(c => c.Score < _lowValueCardThreshold);

                    if (hasGoodAdvantage && remainingCardsLowValue && Random.value < _strategicStopChance)
                    {
                        Debug.Log("[CardPlayManager] Stopping card play with strategic advantage and low-value remaining cards");
                        break;
                    }
                }
            }

            Debug.Log($"[CardPlayManager] Selected {selectedCards.Count} cards to play out of {playableCards.Count} playable cards");
            return selectedCards;
        }

        private bool IsValidPlayState =>
            _combatManager != null &&
            _spritePositioning != null &&
            (_combatManager.IsEnemyPrepPhase() || _combatManager.IsEnemyCombatPhase());

        private IEnumerator SimulatePlaceholderAction()
        {
            Debug.LogWarning("[CardPlayManager] Using placeholder implementation");
            yield return new WaitForSeconds(_actionDelay);
            Debug.Log("[CardPlayManager] Simulating card play");
            yield return new WaitForSeconds(_actionDelay);
        }

        private BoardState GetCurrentBoardState()
        {
            // Try to get the board state from the board state manager first
            var boardState = _boardStateManager?.EvaluateBoardState();

            // If that fails, create a minimal but properly initialized board state
            if (boardState == null)
            {
                Debug.LogWarning("[CardPlayManager] Using fallback BoardState creation");
                boardState = new BoardState
                {
                    EnemyMana = _combatManager.EnemyMana,
                    TurnCount = _combatManager.TurnCount,
                    EnemyHealth = _combatManager.EnemyHealth,
                    PlayerHealth = _combatManager.PlayerHealth,
                    EnemyMaxHealth = _combatManager.MaxHealth,
                    PlayerMaxHealth = _combatManager.MaxHealth,
                    EnemyMonsters = new List<EntityManager>(),
                    PlayerMonsters = new List<EntityManager>(),
                    CardAdvantage = 0,
                    EnemyBoardControl = 0,
                    PlayerBoardControl = 0
                };

                // Try to get entity collections if _spritePositioning is available
                if (_spritePositioning != null)
                {
                    // Build minimal entity lists (without full caching logic)
                    boardState.EnemyMonsters = _spritePositioning.EnemyEntities
                        .Where(e => e != null)
                        .Select(e => e.GetComponent<EntityManager>())
                        .Where(e => e != null && e.placed && !e.dead && !e.IsFadingOut)
                        .ToList();

                    boardState.PlayerMonsters = _spritePositioning.PlayerEntities
                        .Where(e => e != null)
                        .Select(e => e.GetComponent<EntityManager>())
                        .Where(e => e != null && e.placed && !e.dead && !e.IsFadingOut)
                        .ToList();
                }
            }

            return boardState;
        }
    }
}
