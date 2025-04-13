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
            
            try
            {
                // Basic implementation that just logs what would happen
                enemyDeck = _combatManager.EnemyDeck;
                hasCards = enemyDeck != null && enemyDeck.Hand != null && enemyDeck.Hand.Count > 0;
                
                if (!hasCards)
                {
                    Debug.Log("[CardPlayManager] No cards in hand to play");
                }
                else
                {
                    Debug.Log($"[CardPlayManager] Enemy has {enemyDeck.Hand.Count} cards in hand");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardPlayManager] Error in PlayCards: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }
            
            // If we encountered an error or have no cards, just exit early with a small delay
            if (errorOccurred || !hasCards)
            {
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[CardPlayManager] PlayCards completed");
                yield break;
            }
            
            // Simulate playing cards with a delay
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[CardPlayManager] Enemy would play cards here");
            
            // Additional delay at the end
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[CardPlayManager] PlayCards completed");
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

            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (card.CardType.HasKeyword(keyword))
                {
                    score += _keywordEvaluator.EvaluateKeyword(keyword, true, boardState);
                }
            }

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

        private EntityManager GetBestSpellTarget(CardData cardType)
        {
            var boardState = _boardStateManager.EvaluateBoardState();
            return _effectEvaluator.GetBestTargetForEffect(cardType.EffectTypes.First(), true, boardState);
        }

        private bool IsPlayableCard(Card card)
        {
            return card != null && card.CardType.ManaCost <= _combatManager.EnemyMana;
        }

        private bool ValidateCombatState()
        {
            if (_combatManager == null)
            {
                Debug.LogError("[CardPlayManager] CombatManager reference is missing!");
                return false;
            }

            if (!_combatManager.IsEnemyPrepPhase())
            {
                Debug.LogWarning("[CardPlayManager] Basic dependencies check failed - Not in EnemyPrep phase");
                return false;
            }

            return true;
        }
    }
} 