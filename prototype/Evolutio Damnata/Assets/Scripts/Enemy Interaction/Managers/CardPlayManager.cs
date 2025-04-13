using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;

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

        private void Awake()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[CardPlayManager] Starting initialization...");
            
            // First, wait for scene essentials
            int maxAttempts = 30;
            int attempts = 0;
            
            // Find critical scene components first
            while (attempts < maxAttempts)
            {
                _combatManager = _combatManager ?? FindObjectOfType<CombatManager>();
                _combatStage = _combatStage ?? FindObjectOfType<CombatStage>();
                
                if (_combatManager != null && _combatStage != null)
                    break;
                    
                Debug.Log("[CardPlayManager] Searching for scene components...");
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
            
            if (_combatManager == null)
            {
                Debug.LogError("[CardPlayManager] Failed to find CombatManager in scene!");
            }
            
            if (_combatStage == null)
            {
                Debug.LogError("[CardPlayManager] Failed to find CombatStage in scene!");
            }
            
            // If we've found CombatStage, wait for its initialization
            if (_combatStage != null)
            {
                attempts = 0;
                while ((_combatStage.SpritePositioning == null || _combatStage.SpellEffectApplier == null) && attempts < maxAttempts)
                {
                    Debug.Log("[CardPlayManager] Waiting for CombatStage to be fully initialized...");
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }
                
                // Try to get SpritePositioning from CombatStage if not set in inspector
                if (_spritePositioning == null && _combatStage.SpritePositioning != null)
                {
                    _spritePositioning = _combatStage.SpritePositioning as SpritePositioning;
                    Debug.Log("[CardPlayManager] Got SpritePositioning from CombatStage");
                }
                
                // Try to get SpellEffectApplier from CombatStage
                if (_spellEffectApplier == null && _combatStage.SpellEffectApplier != null)
                {
                    _spellEffectApplier = _combatStage.SpellEffectApplier;
                    Debug.Log("[CardPlayManager] Got SpellEffectApplier from CombatStage");
                }
            }
            
            // Wait for AIServices to be ready (optional)
            attempts = 0;
            while (AIServices.Instance == null && attempts < maxAttempts)
            {
                Debug.Log("[CardPlayManager] Waiting for AIServices to be initialized...");
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
            
            // Get dependencies from AIServices if possible
            if (AIServices.Instance != null)
            {
                var services = AIServices.Instance;
                
                if (_keywordEvaluator == null)
                    _keywordEvaluator = services.KeywordEvaluator;
                    
                if (_effectEvaluator == null)
                    _effectEvaluator = services.EffectEvaluator;
                    
                if (_boardStateManager == null)
                    _boardStateManager = services.BoardStateManager;
                    
                Debug.Log("[CardPlayManager] Tried to get services from AIServices");
            }
            
            // Create any missing services locally if needed
            if (_keywordEvaluator == null)
            {
                var keywordEvaluatorObj = new GameObject("KeywordEvaluator_Local");
                keywordEvaluatorObj.transform.SetParent(transform);
                _keywordEvaluator = keywordEvaluatorObj.AddComponent<KeywordEvaluator>();
                Debug.Log("[CardPlayManager] Created local KeywordEvaluator");
            }
            
            if (_effectEvaluator == null)
            {
                var effectEvaluatorObj = new GameObject("EffectEvaluator_Local");
                effectEvaluatorObj.transform.SetParent(transform);
                _effectEvaluator = effectEvaluatorObj.AddComponent<EffectEvaluator>();
                Debug.Log("[CardPlayManager] Created local EffectEvaluator");
            }
            
            if (_boardStateManager == null)
            {
                var boardStateManagerObj = new GameObject("BoardStateManager_Local");
                boardStateManagerObj.transform.SetParent(transform);
                _boardStateManager = boardStateManagerObj.AddComponent<BoardStateManager>();
                Debug.Log("[CardPlayManager] Created local BoardStateManager");
            }
            
            // If we still don't have SpritePositioning, try to create a minimal one
            if (_spritePositioning == null)
            {
                Debug.LogWarning("[CardPlayManager] Unable to get SpritePositioning from scene, functionality will be limited");
            }

            Debug.Log("[CardPlayManager] Initialization completed - some dependencies may be missing but we'll handle it gracefully");
        }

        public IEnumerator PlayCards()
        {
            Debug.Log("[CardPlayManager] Starting PlayCards");
            
            // Validate that the required dependencies are available
            if (_combatManager == null || _spritePositioning == null)
            {
                Debug.LogWarning("[CardPlayManager] Required components are null in PlayCards! Using a placeholder implementation.");
                
                // Simple placeholder implementation that doesn't depend on any components
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[CardPlayManager] Simulating enemy playing cards (placeholder)");
                yield return new WaitForSeconds(0.5f);
                
                Debug.Log("[CardPlayManager] PlayCards completed (placeholder)");
                yield break;
            }
            
            // Variables we'll need both inside and outside the try block
            Deck enemyDeck = null;
            bool hasCards = false;
            bool errorOccurred = false;
            List<Card> playableCards = new List<Card>();
            
            try
            {
                // Check if we have cards to play
                enemyDeck = _combatManager.EnemyDeck;
                hasCards = enemyDeck != null && enemyDeck.Hand != null && enemyDeck.Hand.Count > 0;
                
                if (!hasCards)
                {
                    Debug.Log("[CardPlayManager] No cards in hand to play");
                }
                else
                {
                    Debug.Log($"[CardPlayManager] Enemy has {enemyDeck.Hand.Count} cards in hand");
                    
                    // Find playable cards (those we can afford with our mana)
                    foreach (var card in enemyDeck.Hand)
                    {
                        if (IsPlayableCard(card))
                        {
                            playableCards.Add(card);
                        }
                    }
                    
                    if (playableCards.Count == 0)
                    {
                        Debug.Log("[CardPlayManager] No playable cards found (not enough mana or no valid targets)");
                    }
                    else
                    {
                        Debug.Log($"[CardPlayManager] Found {playableCards.Count} playable cards");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardPlayManager] Error in PlayCards: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }
            
            // If we encountered an error or have no cards, just exit early with a small delay
            if (errorOccurred || playableCards.Count == 0)
            {
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[CardPlayManager] PlayCards completed - no action taken");
                yield break;
            }
            
            // Get current board state for evaluation
            BoardState boardState = null;
            if (_boardStateManager != null)
            {
                boardState = _boardStateManager.EvaluateBoardState();
            }
            else
            {
                // Create a simple board state if no manager available
                boardState = new BoardState
                {
                    EnemyMana = _combatManager.EnemyMana,
                    TurnCount = _combatManager.TurnCount
                };
            }
            
            // Evaluate all playable cards to find the best ones
            Dictionary<Card, float> cardScores = new Dictionary<Card, float>();
            foreach (var card in playableCards)
            {
                float score = EvaluateCardPlay(card, boardState);
                cardScores[card] = score;
                Debug.Log($"[CardPlayManager] Card '{card.CardName}' scored {score}");
            }
            
            // Sort cards by score (highest first)
            var sortedCards = cardScores.OrderByDescending(kvp => kvp.Value)
                                        .Select(kvp => kvp.Key)
                                        .ToList();
            
            // Play cards until we run out of mana or valid positions
            foreach (var card in sortedCards)
            {
                // Skip if we can't afford this card anymore
                if (card.CardType.ManaCost > _combatManager.EnemyMana)
                {
                    Debug.Log($"[CardPlayManager] Can't afford {card.CardName} anymore, skipping");
                    continue;
                }
                
                // Try to play this card
                yield return new WaitForSeconds(0.5f); // Add delay for visual effect
                
                bool cardSuccessfullyPlayed = false;
                
                if (card.CardType.IsMonsterCard)
                {
                    int position = FindBestMonsterPosition(card, boardState);
                    if (position >= 0)
                    {
                        bool success = _combatStage.EnemyCardSpawner.SpawnCard(card.CardName, position);
                        
                        if (success)
                        {
                            Debug.Log($"[CardPlayManager] Successfully played monster card {card.CardName} at position {position}");
                            // Update mana through the property setter
                            _combatManager.EnemyMana -= card.CardType.ManaCost;
                            cardSuccessfullyPlayed = true;
                            
                            // Re-evaluate board state after playing a card
                            if (_boardStateManager != null)
                            {
                                boardState = _boardStateManager.EvaluateBoardState();
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[CardPlayManager] Failed to play monster card {card.CardName}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[CardPlayManager] No valid position found for monster card {card.CardName}");
                    }
                }
                else if (card.CardType.IsSpellCard && _spellEffectApplier != null)
                {
                    // Check if the spell card has effect types
                    if (card.CardType.EffectTypes == null || !card.CardType.EffectTypes.Any())
                    {
                        Debug.LogWarning($"[CardPlayManager] Spell card {card.CardName} has no effect types, skipping");
                        continue;
                    }
                    
                    // For spell cards, find a target
                    EntityManager target = GetBestSpellTarget(card.CardType);
                    
                    if (target != null)
                    {
                        // Is this a health icon or a normal entity?
                        bool isHealthIcon = target is HealthIconManager;
                        
                        // Only check placed status for normal entities, not health icons
                        // Health icons are always considered "placed"
                        if (isHealthIcon || target.placed)
                        {
                            // Apply spell effects
                            bool success = false;
                            
                            try
                            {
                                // Use correct method for SpellEffectApplier
                                _spellEffectApplier.ApplySpellEffectsAI(target, card.CardType, 0);
                                success = true;
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"[CardPlayManager] Error applying spell effects: {e.Message}");
                                success = false;
                            }
                            
                            if (success)
                            {
                                string targetType = isHealthIcon ? 
                                    $"{((target as HealthIconManager).IsPlayerIcon ? "Player" : "Enemy")} health icon" : 
                                    target.name;
                                    
                                Debug.Log($"[CardPlayManager] Successfully played spell card {card.CardName} targeting {targetType}");
                                // Update mana through the property setter
                                _combatManager.EnemyMana -= card.CardType.ManaCost;
                                cardSuccessfullyPlayed = true;
                                
                                // Re-evaluate board state after playing a card
                                if (_boardStateManager != null)
                                {
                                    boardState = _boardStateManager.EvaluateBoardState();
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[CardPlayManager] Target {target.name} is not placed on the board");
                        }
                    }
                    else
                    {
                        Debug.Log($"[CardPlayManager] No valid target found for spell card {card.CardName}");
                    }
                }
                
                // Remove card from hand after successful play
                if (cardSuccessfullyPlayed)
                {
                    enemyDeck.RemoveCard(card);
                    Debug.Log($"[CardPlayManager] Removed card {card.CardName} from enemy hand");
                }
                
                // Limit the number of cards played per turn for balance
                if (_combatManager.EnemyMana < 1) // Break if out of mana
                {
                    Debug.Log("[CardPlayManager] Out of mana, stopping card play");
                    break;
                }
            }
            
            // Additional delay at the end
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[CardPlayManager] PlayCards completed successfully");
        }

        private float EvaluateCardPlay(Card card, BoardState boardState)
        {
            float score = 0f;

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

            if (boardState.HealthAdvantage < 0)
            {
                if (card.CardType.HasKeyword(Keywords.MonsterKeyword.Taunt))
                    score += 30f;
                if (card.CardType.EffectTypes.Contains(SpellEffect.Heal))
                    score += 40f;
            }
            else if (boardState.HealthAdvantage > 0)
            {
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

            score += card.CardType.AttackPower * 0.8f;
            score += card.CardType.Health * 0.6f;

            // Consider attack/health ratio and counter-attack vulnerability
            float attackHealthRatio = card.CardType.AttackPower / (float)card.CardType.Health;
            
            // Higher attack/health ratio is generally better
            if (attackHealthRatio > 1.0f)
            {
                score += 10f; // Bonus for high-attack, lower-health units
            }
            
            // Check for keywords that affect counter-attacks
            bool hasRanged = card.CardType.HasKeyword(Keywords.MonsterKeyword.Ranged);
            
            if (hasRanged)
            {
                // Ranged units don't take counter-attack damage - big bonus
                score += 25f;
                
                // Especially valuable if they have high attack but low health
                if (attackHealthRatio > 1.5f)
                {
                    score += 15f;
                }
            }
            else
            {
                // For non-ranged units, consider durability against counter-attacks
                // Units with higher health are better for trading
                if (card.CardType.Health >= 5)
                {
                    score += 20f; // Bonus for tanky non-ranged units
                }
                
                // Small penalty for fragile non-ranged units
                if (card.CardType.Health <= 2 && card.CardType.AttackPower > 3)
                {
                    score -= 15f; // Penalty for glass cannons that will die to counter-attacks
                }
            }

            // Evaluate all other keywords
            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (keyword != Keywords.MonsterKeyword.Ranged && card.CardType.HasKeyword(keyword))
                {
                    score += _keywordEvaluator.EvaluateKeyword(keyword, true, boardState);
                }
            }

            // Consider board state context
            if (boardState.BoardControlDifference < 0)
                score += 25f;

            if (boardState.TurnCount <= 3 && card.CardType.ManaCost <= 3)
                score += 15f;

            return score;
        }

        private float EvaluateSpellCard(Card card, BoardState boardState)
        {
            float score = 0f;

            foreach (var effect in card.CardType.EffectTypes)
            {
                var target = _effectEvaluator.GetBestTargetForEffect(effect, true, boardState);
                if (target != null)
                {
                    score += _effectEvaluator.EvaluateEffect(effect, true, target, boardState);
                }
            }

            return score;
        }

        private int FindBestMonsterPosition(Card card, BoardState boardState)
        {
            List<int> availablePositions = new List<int>();
            
            // Find all available positions
            for (int i = 0; i < _spritePositioning.EnemyEntities.Count; i++)
            {
                if (_spritePositioning.EnemyEntities[i] == null) continue;
                
                var entity = _spritePositioning.EnemyEntities[i].GetComponent<EntityManager>();
                if (entity != null && !entity.placed)
                {
                    availablePositions.Add(i);
                }
            }
            
            // If no positions available, return -1
            if (availablePositions.Count == 0)
                return -1;
                
            // If only one position available, return it
            if (availablePositions.Count == 1)
                return availablePositions[0];
                
            // For multiple available positions, choose strategically
            
            // Get player entities and their positions for consideration
            var playerEntities = boardState.PlayerMonsters;
            
            // Default to first available position
            int bestPosition = availablePositions[0];
            float bestScore = float.MinValue;
            
            // Check if card has ranged keyword
            bool hasRanged = card.CardType.HasKeyword(Keywords.MonsterKeyword.Ranged);
            bool hasTaunt = card.CardType.HasKeyword(Keywords.MonsterKeyword.Taunt);
            
            foreach (int position in availablePositions)
            {
                float positionScore = 0;
                
                // Basic positioning: prefer middle spots for better flexibility
                int middlePosition = _spritePositioning.EnemyEntities.Count / 2;
                float distanceFromMiddle = Mathf.Abs(position - middlePosition);
                positionScore += (1 - distanceFromMiddle / middlePosition) * 10f;
                
                // Strategic positioning based on card type and board state
                if (hasRanged)
                {
                    // For ranged units, prefer backline positions (further from player units)
                    positionScore += position * 5f;
                }
                else if (hasTaunt)
                {
                    // For taunt units, prefer frontline positions
                    positionScore += (_spritePositioning.EnemyEntities.Count - position) * 5f;
                }
                else
                {
                    // For normal units, consider matchups with opponent units
                    // Check for positions that would create favorable matchups
                    // This is a simplified approach - a more complex one would consider
                    // the exact positions of player entities
                    
                    // If we have high health, prefer positions that can tank enemy damage
                    if (card.CardType.Health >= 5)
                    {
                        // Prefer frontline positions for tanky units
                        positionScore += (_spritePositioning.EnemyEntities.Count - position) * 3f;
                    }
                    else if (card.CardType.AttackPower >= 5)
                    {
                        // For high attack units, position where they can attack vulnerable targets
                        // Mid-positions provide flexibility
                        float midPositionValue = 1 - Mathf.Abs(position - (_spritePositioning.EnemyEntities.Count / 2f)) / (_spritePositioning.EnemyEntities.Count / 2f);
                        positionScore += midPositionValue * 15f;
                    }
                }
                
                if (positionScore > bestScore)
                {
                    bestScore = positionScore;
                    bestPosition = position;
                }
            }
            
            return bestPosition;
        }

        private EntityManager GetBestSpellTarget(CardData cardType)
        {
            if (cardType == null || cardType.EffectTypes == null || !cardType.EffectTypes.Any())
            {
                Debug.LogWarning($"[CardPlayManager] Card has no effect types: {cardType?.CardName ?? "Unknown"}");
                return null;
            }
            
            var boardState = _boardStateManager.EvaluateBoardState();
            return _effectEvaluator.GetBestTargetForEffect(cardType.EffectTypes.First(), true, boardState);
        }

        private bool IsPlayableCard(Card card)
        {
            if (card == null || card.CardType == null || card.CardType.ManaCost > _combatManager.EnemyMana)
            {
                return false;
            }
            
            // Spell cards can be played in either prep or combat phase
            if (card.CardType.IsSpellCard)
            {
                // Make sure the spell card has valid effect types
                if (card.CardType.EffectTypes == null || !card.CardType.EffectTypes.Any())
                {
                    Debug.LogWarning($"[CardPlayManager] Spell card {card.CardName} has no effect types, marking as unplayable");
                    return false;
                }
                
                return _combatManager.IsEnemyPrepPhase() || _combatManager.IsEnemyCombatPhase();
            }
            
            // Monster cards can only be played during prep phase
            if (card.CardType.IsMonsterCard)
            {
                return _combatManager.IsEnemyPrepPhase();
            }
            
            return false; // Unrecognized card type
        }

        private bool ValidateCombatState()
        {
            if (_combatManager == null)
            {
                Debug.LogError("[CardPlayManager] CombatManager reference is missing!");
                return false;
            }

            // Allow card playing during both prep and combat phases
            if (!_combatManager.IsEnemyPrepPhase() && !_combatManager.IsEnemyCombatPhase())
            {
                Debug.LogWarning("[CardPlayManager] Cannot play cards - Not in enemy's turn (needs to be EnemyPrep or EnemyCombat phase)");
                return false;
            }

            return true;
        }
    }
} 