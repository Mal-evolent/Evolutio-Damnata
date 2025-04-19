using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Extensions;
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
            float result = baseScore;

            // Occasionally make suboptimal choices with a fixed penalty instead of percentage
            if (Random.value < _suboptimalPlayChance)
            {
                // Apply a fixed penalty instead of a multiplier
                float penalty = Mathf.Min(baseScore * 0.3f, 30f);
                result -= penalty;
                Debug.Log($"[AI] Making suboptimal play: reducing score by {penalty:F1}");
            }

            // Add variance as a small additive bonus/penalty instead of a multiplier
            float variance = baseScore * _evaluationVariance;
            float randomVariance = Random.Range(-variance, variance);

            // Cap the maximum variance to prevent extreme values
            float cappedVariance = Mathf.Clamp(randomVariance, -20f, 20f);
            result += cappedVariance;

            return result;
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

            // Apply mana efficiency evaluation - this is core to CardEvaluator and shouldn't be in specialized evaluators
            float manaRatio = 1 - (card.CardType.ManaCost / (float)Mathf.Max(1, _combatManager.EnemyMana));
            score += manaRatio * 50f;

            // Delegate to specialized evaluators for card-type specific evaluation
            if (card.CardType.IsMonsterCard)
            {
                var monsterEvaluator = new MonsterCardEvaluator(_keywordEvaluator);
                score += monsterEvaluator.EvaluateMonsterCard(card, boardState);
            }
            else
            {
                var spellEvaluator = new SpellCardEvaluator(_effectEvaluator);
                score += spellEvaluator.EvaluateSpellCard(card, boardState);
            }

            // Add final logging for total evaluation
            Debug.Log($"[CardEvaluator] Final score for {card.CardName}: {score:F1}");

            return score;
        }
    }
}
