using EnemyInteraction.Evaluation;
using EnemyInteraction.Models;
using UnityEngine;

namespace EnemyInteraction.Managers.Evaluation
{
    public class SpellCardEvaluator
    {
        private readonly IEffectEvaluator _effectEvaluator;

        public SpellCardEvaluator(IEffectEvaluator effectEvaluator)
        {
            _effectEvaluator = effectEvaluator;
        }

        public float EvaluateSpellCard(Card card, BoardState boardState)
        {
            if (card.CardType.EffectTypes == null) return 0f;

            float totalScore = 0f;

            // Special evaluations for cards with specific effect combinations
            bool hasDrawEffect = card.CardType.EffectTypes.Contains(SpellEffect.Draw);
            bool hasBloodpriceEffect = card.CardType.EffectTypes.Contains(SpellEffect.Bloodprice);
            bool hasDamageEffect = card.CardType.EffectTypes.Contains(SpellEffect.Damage);
            bool hasBurnEffect = card.CardType.EffectTypes.Contains(SpellEffect.Burn);
            bool hasHealEffect = card.CardType.EffectTypes.Contains(SpellEffect.Heal);

            // Evaluate each effect individually
            foreach (var effect in card.CardType.EffectTypes)
            {
                var target = _effectEvaluator?.GetBestTargetForEffect(effect, true, boardState);
                float effectScore = _effectEvaluator?.EvaluateEffect(effect, true, target, boardState) ?? 0f;
                totalScore += effectScore;
            }

            // Turn order considerations for spell effects
            ApplyTurnOrderConsiderations(card, boardState, ref totalScore, hasDrawEffect, hasBloodpriceEffect, hasHealEffect, hasDamageEffect, hasBurnEffect);

            // Special case for cards with both Draw and Bloodprice effects
            ApplyDrawBloodpriceEvaluation(card, boardState, ref totalScore, hasDrawEffect, hasBloodpriceEffect);

            // Extra bonus for Draw cards when hand is nearly empty
            if (hasDrawEffect && boardState.enemyHandSize <= 1)
            {
                totalScore *= 1.5f;
                Debug.Log($"[SpellCardEvaluator] Increased score for Draw card {card.CardName} due to low hand size");
            }

            return totalScore;
        }

        private void ApplyTurnOrderConsiderations(
            Card card, BoardState boardState, ref float totalScore,
            bool hasDrawEffect, bool hasBloodpriceEffect, bool hasHealEffect,
            bool hasDamageEffect, bool hasBurnEffect)
        {
            if (boardState.IsNextTurnPlayerFirst)
            {
                // Player goes first next - defensive spells more valuable
                if (hasHealEffect)
                {
                    totalScore *= 1.3f; // Increase healing value 
                    Debug.Log($"[SpellCardEvaluator] Boosting heal spell {card.CardName} since player goes first next turn");
                }

                // Card draw less valuable with player going first next turn
                if (hasDrawEffect && !hasBloodpriceEffect)
                {
                    totalScore *= 0.9f;
                }

                // Direct damage to player health more valuable with player going first
                if ((hasDamageEffect || hasBurnEffect) && boardState.PlayerHealth <= 10)
                {
                    totalScore *= 1.4f; // Prioritize finishing the player if possible
                    Debug.Log($"[SpellCardEvaluator] Boosting damage spell {card.CardName} for potential lethal before player's turn");
                }
            }
            else
            {
                // Enemy goes first next - can be more strategic

                // Card draw more valuable when we go first next turn
                if (hasDrawEffect)
                {
                    totalScore *= 1.2f;
                    Debug.Log($"[SpellCardEvaluator] Boosting draw spell {card.CardName} for more options on our next turn");
                }

                // Burn effects more valuable with follow-up turn
                if (hasBurnEffect)
                {
                    totalScore *= 1.25f;
                    Debug.Log($"[SpellCardEvaluator] Boosting burn spell {card.CardName} since we can follow-up next turn");
                }
            }
        }

        private void ApplyDrawBloodpriceEvaluation(
            Card card, BoardState boardState, ref float totalScore,
            bool hasDrawEffect, bool hasBloodpriceEffect)
        {
            if (hasDrawEffect && hasBloodpriceEffect)
            {
                // Calculate the draw-to-bloodprice value ratio
                float bloodpriceValue = card.CardType.BloodpriceValue > 0 ? card.CardType.BloodpriceValue : 1f;
                float drawValueRatio = card.CardType.DrawValue / bloodpriceValue;

                // If we get more cards than health lost, increase the score
                if (drawValueRatio > 1.5f)
                {
                    totalScore *= 1.2f;
                    Debug.Log($"[SpellCardEvaluator] Card {card.CardName} has favorable draw-to-bloodprice ratio: {drawValueRatio}");
                }

                // Consider current health situation
                if (boardState.EnemyHealth < 15 && card.CardType.BloodpriceValue > 3)
                {
                    totalScore *= 0.6f; // Reduce score if health is low and blood price is high
                    Debug.Log($"[SpellCardEvaluator] Reduced score for {card.CardName} due to low health ({boardState.EnemyHealth})");

                    // Further reduce if player goes first next turn - too risky
                    if (boardState.IsNextTurnPlayerFirst)
                    {
                        totalScore *= 0.7f;
                        Debug.Log($"[SpellCardEvaluator] Further reducing bloodprice card {card.CardName} when low health and player goes first next turn");
                    }
                }
            }
        }
    }
}
