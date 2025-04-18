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

                // Special evaluation for cards with both Heal and Bloodprice effects
                if (card.CardType.EffectTypes.Contains(SpellEffect.Heal) &&
                    card.CardType.EffectTypes.Contains(SpellEffect.Bloodprice))
                {
                    score = EvaluateHealBloodpriceTradeoff(card, boardState, score);
                }
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

        /// <summary>
        /// Evaluates the tradeoff between healing benefit and bloodprice cost for cards with both effects
        /// </summary>
        private float EvaluateHealBloodpriceTradeoff(Card card, BoardState boardState, float currentScore)
        {
            // Make sure the card has the required data
            if (card.CardType.EffectValue <= 0 || card.CardType.BloodpriceValue <= 0)
                return currentScore;

            // Get heal amount and bloodprice cost
            float healAmount = card.CardType.EffectValue;
            float bloodpriceCost = card.CardType.BloodpriceValue;

            // Calculate net healing (heal amount - bloodprice cost)
            float netHealing = healAmount - bloodpriceCost;

            // Reject cards that would kill the caster or cause negative net healing
            // when the AI is already at low health
            if (bloodpriceCost >= boardState.EnemyHealth)
            {
                Debug.Log($"[CardEvaluator] Rejecting {card.CardName} - bloodprice would be lethal");
                return -1000000f;
            }

            // Consider entity health situation - used for targeting
            EntityManager bestHealTarget = _effectEvaluator.GetBestTargetForEffect(SpellEffect.Heal, true, boardState);
            float targetHealthPercentage = bestHealTarget != null ?
                bestHealTarget.GetHealth() / bestHealTarget.GetMaxHealth() : 1.0f;

            // Consider entity value - prioritize healing valuable units
            float targetValue = 0f;
            if (bestHealTarget != null)
            {
                targetValue += bestHealTarget.GetAttackPower() * 1.2f;
                targetValue += bestHealTarget.GetMaxHealth() * 0.8f;

                // Bonus for special keywords
                if (bestHealTarget.HasKeyword(Keywords.MonsterKeyword.Taunt))
                    targetValue += 30f;
                if (bestHealTarget.HasKeyword(Keywords.MonsterKeyword.Ranged))
                    targetValue += 25f;
            }

            // Calculate heal efficiency (heal amount per bloodprice point)
            float healEfficiency = healAmount / bloodpriceCost;

            // Consider own health percentage
            float ownHealthPercentage = boardState.EnemyHealth / (float)boardState.EnemyMaxHealth;

            // Adjust score based on various factors
            float adjustedScore = currentScore;

            // Factor 1: Heal efficiency - good cards should heal more than they damage
            if (healEfficiency > 1.5f)
            {
                adjustedScore *= 1.3f;
                Debug.Log($"[CardEvaluator] {card.CardName} has excellent heal efficiency: {healEfficiency:F2}");
            }
            else if (healEfficiency > 1.0f)
            {
                adjustedScore *= 1.1f;
                Debug.Log($"[CardEvaluator] {card.CardName} has positive heal efficiency: {healEfficiency:F2}");
            }
            else if (healEfficiency < 1.0f && ownHealthPercentage < 0.3f)
            {
                // Negative efficiency is especially bad at low health
                adjustedScore *= 0.5f;
                Debug.Log($"[CardEvaluator] {card.CardName} has poor heal efficiency at low health: {healEfficiency:F2}");
            }

            // Factor 2: Target health - more valuable to heal nearly-dead entities
            if (targetHealthPercentage < 0.3f && healAmount > bestHealTarget.GetMaxHealth() * 0.3f)
            {
                // Big heal on critical entity
                adjustedScore *= 1.4f;
                Debug.Log($"[CardEvaluator] {card.CardName} would save critical entity {bestHealTarget.name}");
            }
            else if (targetHealthPercentage > 0.7f)
            {
                // Less valuable to heal healthy entities
                adjustedScore *= 0.8f;
            }

            // Factor 3: Own health situation
            if (ownHealthPercentage < 0.2f)
            {
                // Very risky to use bloodprice at low health
                adjustedScore *= 0.6f;
                Debug.Log($"[CardEvaluator] {card.CardName} is risky at current health ({boardState.EnemyHealth})");
            }
            else if (ownHealthPercentage > 0.7f)
            {
                // Safer to use bloodprice at high health
                adjustedScore *= 1.2f;
            }

            // Factor 4: Target value - worth risking health for valuable entities
            if (targetValue > 15f)
            {
                adjustedScore *= 1.2f;
                Debug.Log($"[CardEvaluator] {card.CardName} targets high-value entity {bestHealTarget.name}");
            }

            // Factor 5: Board state context
            if (boardState.EnemyBoardControl < boardState.PlayerBoardControl * 0.7f)
            {
                // When losing board control, healing key units is more important
                adjustedScore *= 1.25f;
                Debug.Log($"[CardEvaluator] {card.CardName} more valuable when behind on board control");
            }

            // Factor 6: Turn order considerations
            if (boardState.IsNextTurnPlayerFirst && boardState.PlayerBoardControl > boardState.EnemyBoardControl)
            {
                // More important to heal when player goes next and has board advantage
                adjustedScore *= 1.3f;
                Debug.Log($"[CardEvaluator] {card.CardName} more valuable before player's turn");
            }

            // Factor 7: Net healing consideration - prioritize positive net healing
            if (netHealing > 0)
            {
                adjustedScore += netHealing * 5f;
                Debug.Log($"[CardEvaluator] {card.CardName} provides positive net healing: {netHealing}");
            }
            else
            {
                // Negative net healing should be penalized
                adjustedScore += netHealing * 2f;
            }

            return adjustedScore;
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
