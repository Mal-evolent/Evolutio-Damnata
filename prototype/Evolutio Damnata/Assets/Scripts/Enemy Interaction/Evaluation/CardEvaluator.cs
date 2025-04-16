using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Interfaces;
using UnityEngine;

namespace EnemyInteraction.Managers.Evaluation
{
    public class CardEvaluator : ICardEvaluator
    {
        private readonly ICombatManager _combatManager;
        private readonly IKeywordEvaluator _keywordEvaluator;
        private readonly IEffectEvaluator _effectEvaluator;
        private readonly float _suboptimalPlayChance;
        private readonly float _evaluationVariance;

        public CardEvaluator(ICombatManager combatManager, 
                            IKeywordEvaluator keywordEvaluator, 
                            IEffectEvaluator effectEvaluator,
                            float suboptimalPlayChance = 0.1f,
                            float evaluationVariance = 0.15f)
        {
            _combatManager = combatManager;
            _keywordEvaluator = keywordEvaluator;
            _effectEvaluator = effectEvaluator;
            _suboptimalPlayChance = suboptimalPlayChance;
            _evaluationVariance = evaluationVariance;
        }

        public List<Card> DetermineCardPlayOrder(List<Card> playableCards, BoardState boardState)
        {
            var scoredCards = playableCards
                .Select(card => new
                {
                    Card = card,
                    Score = ApplyDecisionVariance(EvaluateCardPlay(card, boardState))
                })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Card)
                .ToList();

            Debug.Log($"[CardEvaluator] Highest scored card: {scoredCards.FirstOrDefault()?.CardName}");
            return scoredCards;
        }

        public float ApplyDecisionVariance(float baseScore)
        {
            // Occasionally make suboptimal choices
            if (Random.value < _suboptimalPlayChance)
            {
                baseScore *= Random.Range(0.5f, 0.8f);
                Debug.Log("[AI] Making suboptimal play for variety");
            }

            // Add small random variance to scores
            return baseScore * Random.Range(1f - _evaluationVariance, 1f + _evaluationVariance);
        }

        public List<Card> GetPlayableCards(IEnumerable<Card> hand)
        {
            return hand.Where(card =>
                card != null &&
                card.CardType != null &&
                card.CardType.ManaCost <= _combatManager.EnemyMana &&
                IsCardPlayableInCurrentPhase(card))
                .ToList();
        }

        public bool IsCardPlayableInCurrentPhase(Card card)
        {
            if (card == null || card.CardType == null)
                return false;

            // For monster cards, only allow play during prep phase
            if (card.CardType.IsMonsterCard)
                return _combatManager.IsEnemyPrepPhase();

            // For spell cards, check that they have valid effect types
            if (card.CardType.IsSpellCard)
            {
                bool hasValidEffects = card.CardType.EffectTypes != null &&
                                    card.CardType.EffectTypes.Any();

                // Allow damaging spells in combat phase, utility spells only in prep phase
                if (_combatManager.IsEnemyCombatPhase())
                {
                    // During combat, only allow damaging spells
                    bool hasDamagingEffect = card.CardType.EffectTypes.Any(e =>
                        e == SpellEffect.Damage || e == SpellEffect.Burn);
                    return hasValidEffects && hasDamagingEffect;
                }
                else if (_combatManager.IsEnemyPrepPhase())
                {
                    // During prep, allow any spell with valid effects
                    return hasValidEffects;
                }
            }

            return false;
        }

        public float EvaluateCardPlay(Card card, BoardState boardState)
        {
            if (card?.CardType == null) return 0f;

            float score = 0f;
            float manaRatio = 1 - (card.CardType.ManaCost / (float)Mathf.Max(1, _combatManager.EnemyMana));
            score += manaRatio * 50f;

            if (card.CardType.IsMonsterCard)
            {
                // Delegate to specialized monster card evaluator
                var monsterEvaluator = new MonsterCardEvaluator(_keywordEvaluator);
                score += monsterEvaluator.EvaluateMonsterCard(card, boardState);
            }
            else
            {
                // Delegate to specialized spell card evaluator
                var spellEvaluator = new SpellCardEvaluator(_effectEvaluator);
                score += spellEvaluator.EvaluateSpellCard(card, boardState);
            }

            // Strategic modifiers based on board state
            if (boardState.HealthAdvantage < 0) // Losing
            {
                if (card.CardType.HasKeyword(Keywords.MonsterKeyword.Taunt))
                    score += 30f;
                if (card.CardType.EffectTypes.Contains(SpellEffect.Heal))
                    score += 40f;
            }
            else // Winning or even
            {
                if (card.CardType.AttackPower > 0)
                    score += 20f;
                if (card.CardType.EffectTypes.Contains(SpellEffect.Damage))
                    score += 30f;
            }

            // Turn order strategic considerations
            ApplyTurnOrderConsiderations(card, boardState, ref score);

            return score;
        }

        private void ApplyTurnOrderConsiderations(Card card, BoardState boardState, ref float score)
        {
            if (boardState.IsNextTurnPlayerFirst)
            {
                // Player goes first next turn, adjust strategy

                // Defensive priority if player goes next
                if (card.CardType.HasKeyword(Keywords.MonsterKeyword.Taunt))
                {
                    score += 25f;
                    Debug.Log($"[CardEvaluator] Prioritizing Taunt unit {card.CardName} since player goes first next turn");
                }

                // Emergency healing is more valuable when player goes next
                if (card.CardType.EffectTypes.Contains(SpellEffect.Heal) && boardState.EnemyHealth < 15)
                {
                    score += 35f;
                    Debug.Log($"[CardEvaluator] Prioritizing Healing spell {card.CardName} since player goes first next turn");
                }

                // For monsters, favor those that can withstand player's next turn
                if (card.CardType.IsMonsterCard && card.CardType.Health >= 4)
                {
                    score += 15f;
                    Debug.Log($"[CardEvaluator] Prioritizing high-health unit {card.CardName} to survive player's next turn");
                }
            }
            else
            {
                // Enemy goes first next turn, can be more aggressive

                // Prioritize higher attack monsters for follow-up attacks
                if (card.CardType.IsMonsterCard && card.CardType.AttackPower >= 4)
                {
                    score += 20f;
                    Debug.Log($"[CardEvaluator] Prioritizing high-attack unit {card.CardName} for follow-up attack next turn");
                }

                // Card draw is more valuable when we go first next turn
                if (card.CardType.EffectTypes.Contains(SpellEffect.Draw))
                {
                    score += 25f;
                    Debug.Log($"[CardEvaluator] Prioritizing card draw from {card.CardName} for more options next turn");
                }
            }
        }
    }
}
