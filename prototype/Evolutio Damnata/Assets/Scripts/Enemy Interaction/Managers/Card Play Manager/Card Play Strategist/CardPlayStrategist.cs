using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EnemyInteraction.Models;
using EnemyInteraction.Services;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Managers.Evaluation;
using EnemyInteraction.Managers.Execution;


namespace EnemyInteraction.Managers
{
    public interface ICardPlayStrategist
    {
        IEnumerator ExecuteCardPlayStrategy();
    }

    public class CardPlayStrategist : ICardPlayStrategist
    {
        private readonly IDependencyProvider _dependencies;
        private readonly CardPlaySettings _settings;
        
        public CardPlayStrategist(IDependencyProvider dependencies, CardPlaySettings settings)
        {
            _dependencies = dependencies;
            _settings = settings;
        }
        
        public IEnumerator ExecuteCardPlayStrategy()
        {
            // Initial delay before starting actions
            yield return new WaitForSeconds(_settings.ActionDelay);
            Debug.Log("[CardPlayStrategist] Starting card play sequence...");

            if (!IsValidPlayState())
            {
                yield return SimulatePlaceholderAction();
                yield break;
            }

            var combatManager = _dependencies.GetService<ICombatManager>();
            var cardEvaluator = _dependencies.GetService<ICardEvaluator>();
            var cardPlayExecutor = _dependencies.GetService<ICardPlayExecutor>();
            
            var enemyDeck = combatManager.EnemyDeck;
            if (enemyDeck == null || enemyDeck.Hand == null || enemyDeck.Hand.Count == 0)
            {
                Debug.Log("[CardPlayStrategist] No cards in hand to play");
                yield break;
            }

            var playableCards = cardEvaluator.GetPlayableCards(enemyDeck.Hand);
            if (playableCards.Count == 0)
            {
                Debug.Log("[CardPlayStrategist] No playable cards found");
                yield break;
            }

            // Add delay before evaluating board state
            yield return new WaitForSeconds(_settings.ActionDelay);

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
                Debug.Log("[CardPlayStrategist] AI decided to hold cards for strategic reasons");
                yield return new WaitForSeconds(_settings.ActionDelay * 1.5f);
                yield break;
            }

            // Determine the optimal cards to play and their order
            var cardsToPlay = GetOptimalCardsToPlay(playableCards, boardState);
            if (cardsToPlay.Count == 0)
            {
                Debug.Log("[CardPlayStrategist] AI decided to hold all cards after evaluation");
                yield return new WaitForSeconds(_settings.ActionDelay);
                yield break;
            }

            // Execute the card play strategy
            yield return cardPlayExecutor.PlayCardsInOrder(cardsToPlay, enemyDeck, boardState);

            // Final delay after all cards are played
            yield return new WaitForSeconds(_settings.ActionDelay);
            Debug.Log("[CardPlayStrategist] Completed playing cards");
        }
        
        private bool IsValidPlayState()
        {
            var combatManager = _dependencies.GetService<ICombatManager>();
            var spritePositioning = _dependencies.GetService<SpritePositioning>();
            
            // Check for null managers first
            if (combatManager == null || spritePositioning == null)
            {
                Debug.LogWarning("[CardPlayStrategist] Cannot validate play state: managers are null");
                return false;
            }

            // Check for valid phase
            try
            {
                bool isValidPhase = combatManager.IsEnemyPrepPhase() || combatManager.IsEnemyCombatPhase();
                if (!isValidPhase)
                {
                    Debug.LogWarning($"[CardPlayStrategist] Not in a valid enemy phase. Current phase: {combatManager.CurrentPhase}");
                }
                return isValidPhase;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CardPlayStrategist] Error checking phase: {e.Message}");
                return false;
            }
        }

        private IEnumerator SimulatePlaceholderAction()
        {
            Debug.LogWarning("[CardPlayStrategist] Using placeholder implementation");
            yield return new WaitForSeconds(_settings.ActionDelay);
            Debug.Log("[CardPlayStrategist] Simulating card play");
            yield return new WaitForSeconds(_settings.ActionDelay);
        }

        private BoardState GetCurrentBoardState()
        {
            var boardStateManager = _dependencies.GetService<IBoardStateManager>();
            
            // Try to get the board state from the board state manager first
            var boardState = boardStateManager?.EvaluateBoardState();

            // If that fails, create a minimal but properly initialized board state
            if (boardState == null)
            {
                Debug.LogWarning("[CardPlayStrategist] Using fallback BoardState creation");
                boardState = CreateFallbackBoardState();
            }

            return boardState;
        }

        private BoardState CreateFallbackBoardState()
        {
            var combatManager = _dependencies.GetService<ICombatManager>();
            var spritePositioning = _dependencies.GetService<SpritePositioning>();
            
            if (combatManager == null) return null;

            var boardState = new BoardState
            {
                EnemyMana = combatManager.EnemyMana,
                TurnCount = combatManager.TurnCount,
                EnemyHealth = combatManager.EnemyHealth,
                PlayerHealth = combatManager.PlayerHealth,
                EnemyMaxHealth = combatManager.MaxHealth,
                PlayerMaxHealth = combatManager.MaxHealth,
                EnemyMonsters = new List<EntityManager>(),
                PlayerMonsters = new List<EntityManager>(),
                CardAdvantage = 0,
                EnemyBoardControl = 0,
                PlayerBoardControl = 0
            };

            if (spritePositioning != null)
            {
                boardState.EnemyMonsters = GetValidEntities(spritePositioning.EnemyEntities);
                boardState.PlayerMonsters = GetValidEntities(spritePositioning.PlayerEntities);
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

        private bool ShouldSkipCardPlay(List<Card> playableCards, BoardState boardState)
        {
            var cardEvaluator = _dependencies.GetService<ICardEvaluator>();
            
            // Base chance check
            if (Random.value > _settings.SkipCardPlayChance)
                return false;

            Debug.Log("[CardPlayStrategist] Considering whether to skip playing cards...");

            // Calculate board advantage
            float enemyBoardAdvantage = CalculateBoardAdvantage(boardState);
            bool hasBoardAdvantage = enemyBoardAdvantage >= _settings.CardHoldBoardAdvantageThreshold;

            // Game state checks
            bool isLateGame = boardState.TurnCount >= 5;
            bool playerLowHealth = boardState.PlayerHealth <= _settings.PlayerLowHealthThreshold;

            // Always press advantage when player is at low health
            if (playerLowHealth)
            {
                Debug.Log("[CardPlayStrategist] Won't skip - player health is low, pressing advantage");
                return false;
            }

            // Early game with board advantage - consider skipping
            if (!isLateGame && hasBoardAdvantage && Random.value < 0.7f)
            {
                Debug.Log($"[CardPlayStrategist] Skipping card play - early game with board advantage of {enemyBoardAdvantage:F2}");
                return true;
            }

            // Skip if cards have low value but we have board advantage
            float averageCardValue = playableCards.Average(c => cardEvaluator.EvaluateCardPlay(c, boardState));
            if (averageCardValue < _settings.LowValueCardThreshold && hasBoardAdvantage)
            {
                Debug.Log($"[CardPlayStrategist] Skipping card play - cards have low average value ({averageCardValue:F2})");
                return true;
            }

            // Late game strategy - almost always play cards
            if (isLateGame && enemyBoardAdvantage < 2.0f)
            {
                Debug.Log("[CardPlayStrategist] Won't skip - late game requires playing available cards");
                return false;
            }

            // Deck conservation strategy
            if (boardState.EnemyDeckSize < _settings.LowDeckSizeThreshold)
            {
                Debug.Log("[CardPlayStrategist] Low on cards in deck, being more selective");
                return Random.value < _settings.LowDeckSizeConservationChance;
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
            var combatManager = _dependencies.GetService<ICombatManager>();
            var cardEvaluator = _dependencies.GetService<ICardEvaluator>();
            
            // Evaluate and rank cards
            var evaluatedCards = EvaluateCards(playableCards, boardState);
            var selectedCards = new List<Card>();
            int remainingMana = combatManager.EnemyMana;

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

            Debug.Log($"[CardPlayStrategist] Selected {selectedCards.Count} cards to play out of {playableCards.Count} playable cards");
            return selectedCards;
        }

        private IEnumerable<CardEvaluation> EvaluateCards(List<Card> playableCards, BoardState boardState)
        {
            var cardEvaluator = _dependencies.GetService<ICardEvaluator>();
            
            return playableCards
                .Select(card => new CardEvaluation
                {
                    Card = card,
                    Score = cardEvaluator.EvaluateCardPlay(card, boardState),
                    FutureValue = CalculateFutureValue(card, boardState)
                })
                .OrderByDescending(c => c.Score)
                .ToList();
        }

        private float CalculateFutureValue(Card card, BoardState boardState)
        {
            // Higher mana cost cards generally have higher strategic future value
            float baseMultiplier = boardState.TurnCount < 3 ? _settings.EarlyGameExpensiveCardMultiplier : 1.0f;
            return card.CardType.ManaCost * _settings.FutureValueMultiplier * baseMultiplier;
        }

        private bool ShouldHoldCard(CardEvaluation cardData, BoardState boardState)
        {
            // Check for expensive card in early game
            bool isExpensiveCard = cardData.Card.CardType.ManaCost >= 4;
            bool isEarlyGame = boardState.TurnCount <= 3;

            if (isExpensiveCard && isEarlyGame)
            {
                float boardAdvantage = CalculateBoardAdvantage(boardState);

                if (boardAdvantage > _settings.EarlyStopBoardAdvantageThreshold &&
                    cardData.Score < _settings.HighValueCardThreshold &&
                    Random.value < _settings.HoldExpensiveCardChance)
                {
                    Debug.Log($"[CardPlayStrategist] Holding expensive card '{cardData.Card.CardName}' for future turns");
                    return true;
                }
            }

            // Check for card with high future value
            if (cardData.FutureValue > cardData.Score * _settings.FutureToCurrentValueRatio &&
                boardState.EnemyBoardControl > boardState.PlayerBoardControl &&
                Random.value < _settings.HoldHighFutureValueChance)
            {
                Debug.Log($"[CardPlayStrategist] Holding card '{cardData.Card.CardName}' for higher future value");
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
            bool hasGoodAdvantage = boardAdvantage > _settings.EarlyStopBoardAdvantageThreshold;

            bool remainingCardsLowValue = evaluatedCards
                .Where(c => c.Card.CardType.ManaCost <= remainingMana)
                .All(c => c.Score < _settings.LowValueCardThreshold);

            if (hasGoodAdvantage && remainingCardsLowValue && Random.value < _settings.StrategicStopChance)
            {
                Debug.Log("[CardPlayStrategist] Stopping card play with strategic advantage and low-value remaining cards");
                return true;
            }

            return false;
        }
        
        private class CardEvaluation
        {
            public Card Card { get; set; }
            public float Score { get; set; }
            public float FutureValue { get; set; }
        }
    }
}
