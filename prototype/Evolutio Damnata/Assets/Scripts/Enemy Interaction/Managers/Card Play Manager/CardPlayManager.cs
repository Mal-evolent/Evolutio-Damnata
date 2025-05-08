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
using UnityEngine.SceneManagement;

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

        [SerializeField, Range(0f, 1f)]
        private float _skipCardPlayChance = 0.15f;

        [SerializeField]
        private float _cardHoldBoardAdvantageThreshold = 1.3f;

        [SerializeField, Range(0f, 1f)]
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

        // Component references
        private ICardEvaluator _cardEvaluator;
        private MonsterPositionSelector _monsterPositionSelector;
        private SpellTargetSelector _spellTargetSelector;
        private ICardPlayExecutor _cardPlayExecutor;

        public static CardPlayManager Instance { get; private set; }

        private void Awake()
        {
            // Implement singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CardPlayManager] Another instance already exists, destroying this one");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Initialize entity cache
            _entityCache = new Dictionary<GameObject, EntityManager>();
            StartCoroutine(Initialize());
        }

        private void OnEnable()
        {
            // Register for scene load events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            // Unregister to prevent memory leaks
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[CardPlayManager] Scene loaded: {scene.name}");

            // Find references again after scene load
            StartCoroutine(ReacquireSceneReferences());
        }

        private IEnumerator ReacquireSceneReferences()
        {
            yield return new WaitForSeconds(0.5f); // Wait for scene to stabilize

            Debug.Log("[CardPlayManager] Reacquiring scene references...");

            // Clear references that might be stale
            _combatManager = null;
            _combatStage = null;
            _spritePositioning = null;
            _spellEffectApplier = null;

            // Clear entity cache
            _entityCache.Clear();

            // Reacquire references
            yield return InitializeCriticalComponents();
            yield return InitializeOptionalServices();

            // Rebuild components with fresh references
            InitializeCardPlayComponents();
            BuildEntityCache();

            Debug.Log("[CardPlayManager] Scene references reacquired");
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

            // Refresh entity cache before getting board state
            var entityCacheManager = AIServices.Instance?.EntityCacheManager as EntityCacheManager;
            entityCacheManager?.RefreshAfterAction();

            // Get current board state for decision making
            var boardState = GetCurrentBoardState();
            if (boardState == null)
            {
                yield break;
            }

            // Strategic decision making
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

            // Execute the card play strategy
            yield return _cardPlayExecutor.PlayCardsInOrder(cardsToPlay, enemyDeck, boardState);

            // Final delay after all cards are played
            yield return new WaitForSeconds(_actionDelay);
            Debug.Log("[CardPlayManager] Completed playing cards");
        }

        private bool ShouldSkipCardPlay(List<Card> playableCards, BoardState boardState)
        {
            // Base chance check
            if (Random.value > _skipCardPlayChance)
                return false;

            Debug.Log("[CardPlayManager] Considering whether to skip playing cards...");

            // Calculate board advantage
            float enemyBoardAdvantage = CalculateBoardAdvantage(boardState);
            bool hasBoardAdvantage = enemyBoardAdvantage >= _cardHoldBoardAdvantageThreshold;

            // Game state checks
            bool isLateGame = boardState.TurnCount >= 5;
            bool playerLowHealth = boardState.PlayerHealth <= _playerLowHealthThreshold;

            // Always press advantage when player is at low health
            if (playerLowHealth)
            {
                Debug.Log("[CardPlayManager] Won't skip - player health is low, pressing advantage");
                return false;
            }

            // Early game with board advantage - consider skipping
            if (!isLateGame && hasBoardAdvantage && Random.value < 0.7f)
            {
                Debug.Log($"[CardPlayManager] Skipping card play - early game with board advantage of {enemyBoardAdvantage:F2}");
                return true;
            }

            // Skip if cards have low value but we have board advantage
            float averageCardValue = playableCards.Average(c => _cardEvaluator.EvaluateCardPlay(c, boardState));
            if (averageCardValue < _lowValueCardThreshold && hasBoardAdvantage)
            {
                Debug.Log($"[CardPlayManager] Skipping card play - cards have low average value ({averageCardValue:F2})");
                return true;
            }

            // Late game strategy - almost always play cards
            if (isLateGame && enemyBoardAdvantage < 2.0f)
            {
                Debug.Log("[CardPlayManager] Won't skip - late game requires playing available cards");
                return false;
            }

            // Deck conservation strategy
            if (boardState.EnemyDeckSize < _lowDeckSizeThreshold)
            {
                Debug.Log("[CardPlayManager] Low on cards in deck, being more selective");
                return Random.value < _lowDeckSizeConservationChance;
            }

            return false;
        }

        private float CalculateBoardAdvantage(BoardState boardState)
        {
            if (boardState.PlayerBoardControl <= 0)
                return boardState.EnemyBoardControl > 0 ? 999f : 1f;

            return boardState.EnemyBoardControl / boardState.PlayerBoardControl;
        }

        private List<Card> GetOptimalCardsToPlay(List<Card> playableCards, BoardState boardState)
        {
            // Evaluate and rank cards
            var evaluatedCards = EvaluateCards(playableCards, boardState);
            var selectedCards = new List<Card>();
            int remainingMana = _combatManager.EnemyMana;

            // Process cards in order of their value
            foreach (var cardData in evaluatedCards)
            {
                // Skip if not enough mana
                if (cardData.Card.CardType.ManaCost > remainingMana)
                    continue;

                // Strategic card holding logic
                if (ShouldHoldCard(cardData, boardState))
                    continue;

                // Add card to play list
                selectedCards.Add(cardData.Card);
                remainingMana -= cardData.Card.CardType.ManaCost;

                // Stop if out of mana
                if (remainingMana <= 0)
                    break;

                // Consider strategic stopping after playing multiple cards
                if (ShouldStopPlaying(selectedCards, evaluatedCards, remainingMana, boardState))
                    break;
            }

            Debug.Log($"[CardPlayManager] Selected {selectedCards.Count} cards to play out of {playableCards.Count} playable cards");
            return selectedCards;
        }

        private IEnumerable<CardEvaluation> EvaluateCards(List<Card> playableCards, BoardState boardState)
        {
            return playableCards
                .Select(card => new CardEvaluation
                {
                    Card = card,
                    Score = _cardEvaluator.EvaluateCardPlay(card, boardState),
                    FutureValue = CalculateFutureValue(card, boardState)
                })
                .OrderByDescending(c => c.Score)
                .ToList();
        }

        private float CalculateFutureValue(Card card, BoardState boardState)
        {
            // Higher mana cost cards generally have higher strategic future value
            float baseMultiplier = boardState.TurnCount < 3 ? _earlyGameExpensiveCardMultiplier : 1.0f;
            return card.CardType.ManaCost * _futureValueMultiplier * baseMultiplier;
        }

        private bool ShouldHoldCard(CardEvaluation cardData, BoardState boardState)
        {
            // Check for expensive card in early game
            bool isExpensiveCard = cardData.Card.CardType.ManaCost >= 4;
            bool isEarlyGame = boardState.TurnCount <= 3;

            if (isExpensiveCard && isEarlyGame)
            {
                float boardAdvantage = CalculateBoardAdvantage(boardState);

                if (boardAdvantage > _earlyStopBoardAdvantageThreshold &&
                    cardData.Score < _highValueCardThreshold &&
                    Random.value < _holdExpensiveCardChance)
                {
                    Debug.Log($"[CardPlayManager] Holding expensive card '{cardData.Card.CardName}' for future turns");
                    return true;
                }
            }

            // Check for card with high future value
            if (cardData.FutureValue > cardData.Score * _futureToCurrentValueRatio &&
                boardState.EnemyBoardControl > boardState.PlayerBoardControl &&
                Random.value < _holdHighFutureValueChance)
            {
                Debug.Log($"[CardPlayManager] Holding card '{cardData.Card.CardName}' for higher future value");
                return true;
            }

            return false;
        }

        private bool ShouldStopPlaying(
            List<Card> selectedCards,
            IEnumerable<CardEvaluation> evaluatedCards,
            int remainingMana,
            BoardState boardState)
        {
            if (selectedCards.Count < 2)
                return false;

            float boardAdvantage = CalculateBoardAdvantage(boardState);
            bool hasGoodAdvantage = boardAdvantage > _earlyStopBoardAdvantageThreshold;

            bool remainingCardsLowValue = evaluatedCards
                .Where(c => c.Card.CardType.ManaCost <= remainingMana)
                .All(c => c.Score < _lowValueCardThreshold);

            if (hasGoodAdvantage && remainingCardsLowValue && Random.value < _strategicStopChance)
            {
                Debug.Log("[CardPlayManager] Stopping card play with strategic advantage and low-value remaining cards");
                return true;
            }

            return false;
        }

        private bool IsValidPlayState
        {
            get
            {
                // Check for null managers first
                if (_combatManager == null || _spritePositioning == null)
                {
                    Debug.LogWarning("[CardPlayManager] Cannot validate play state: managers are null");
                    return false;
                }

                // Check for valid phase
                try
                {
                    bool isValidPhase = _combatManager.IsEnemyPrepPhase() || _combatManager.IsEnemyCombatPhase();
                    if (!isValidPhase)
                    {
                        Debug.LogWarning($"[CardPlayManager] Not in a valid enemy phase. Current phase: {_combatManager.CurrentPhase}");
                    }
                    return isValidPhase;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[CardPlayManager] Error checking phase: {e.Message}");
                    return false;
                }
            }
        }

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
                boardState = CreateFallbackBoardState();
            }

            return boardState;
        }

        private BoardState CreateFallbackBoardState()
        {
            if (_combatManager == null) return null;

            var boardState = new BoardState
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

            if (_spritePositioning != null)
            {
                boardState.EnemyMonsters = GetValidEntities(_spritePositioning.EnemyEntities);
                boardState.PlayerMonsters = GetValidEntities(_spritePositioning.PlayerEntities);
            }

            return boardState;
        }

        private List<EntityManager> GetValidEntities(IEnumerable<GameObject> entities)
        {
            return entities
                .Where(e => e != null)
                .Select(e => e.GetComponent<EntityManager>())
                .Where(e => e != null && e.placed && !e.dead && !e.IsFadingOut)
                .ToList();
        }

        private class CardEvaluation
        {
            public Card Card { get; set; }
            public float Score { get; set; }
            public float FutureValue { get; set; }
        }
    }
}
