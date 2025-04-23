using EnemyInteraction.Evaluation;
using EnemyInteraction.Models;
using UnityEngine;

namespace EnemyInteraction.Managers.Evaluation
{
    public class SpellCardEvaluator
    {
        private readonly IEffectEvaluator _effectEvaluator;

        // Constants for evaluation logic
        private const float LETHAL_REJECTION_SCORE = -1000000f;
        private const float LOW_PLAYER_HEALTH_THRESHOLD = 10f;
        private const float CRITICAL_ENEMY_HEALTH_THRESHOLD = 0.4f;
        private const float LOW_ENEMY_HEALTH_THRESHOLD = 10f;

        public SpellCardEvaluator(IEffectEvaluator effectEvaluator)
        {
            _effectEvaluator = effectEvaluator;
        }

        public float EvaluateSpellCard(Card card, BoardState boardState)
        {
            if (card.CardType.EffectTypes == null) return 0f;

            // Check for lethal blood price immediately before any other evaluation
            bool hasBloodpriceEffect = card.CardType.EffectTypes.Contains(SpellEffect.Bloodprice);
            if (hasBloodpriceEffect && card.CardType.BloodpriceValue >= boardState.EnemyHealth)
            {
                Debug.Log($"[SpellCardEvaluator] Card {card.CardName} rejected - blood price would be lethal");
                return LETHAL_REJECTION_SCORE;
            }

            float totalScore = 0f;

            // Identify card effect types for specialized evaluation
            bool hasDrawEffect = card.CardType.EffectTypes.Contains(SpellEffect.Draw);
            bool hasDamageEffect = card.CardType.EffectTypes.Contains(SpellEffect.Damage);
            bool hasBurnEffect = card.CardType.EffectTypes.Contains(SpellEffect.Burn);
            bool hasHealEffect = card.CardType.EffectTypes.Contains(SpellEffect.Heal);

            // Special case: Evaluate draw cards differently
            if (hasDrawEffect)
            {
                totalScore += EvaluateCardDrawValue(card, boardState);
            }
            else
            {
                // Evaluate each effect individually for non-draw cards
                foreach (var effect in card.CardType.EffectTypes)
                {
                    var target = _effectEvaluator?.GetBestTargetForEffect(effect, true, boardState);
                    float effectScore = _effectEvaluator?.EvaluateEffect(effect, true, target, boardState) ?? 0f;
                    totalScore += effectScore;
                }
            }

            // Apply specialized card type evaluations
            ApplySpecializedEffectCombinationLogic(card, boardState, ref totalScore,
                hasHealEffect, hasDamageEffect, hasBurnEffect);

            // Apply turn order considerations for spell effects - this is unique to spell evaluation
            // and doesn't overlap with board state evaluator
            ApplyTurnOrderConsiderations(card, boardState, ref totalScore,
                hasDrawEffect, hasBloodpriceEffect, hasHealEffect, hasDamageEffect, hasBurnEffect);

            // Special case for cards with both Draw and Bloodprice effects
            if (hasDrawEffect && hasBloodpriceEffect)
            {
                ApplyDrawBloodpriceEvaluation(card, boardState, ref totalScore);
            }

            return totalScore;
        }

        /// <summary>
        /// Evaluates the value of card draw effects based on current hand size vs maximum hand size,
        /// with increased value when hand is less than half full and penalties for low deck size
        /// </summary>
        private float EvaluateCardDrawValue(Card card, BoardState boardState)
        {
            float baseScore = 0f;

            // Ensure card has valid draw effect
            if (card?.CardType == null || !card.CardType.EffectTypes.Contains(SpellEffect.Draw))
                return baseScore;

            int drawValue = card.CardType.DrawValue;
            if (drawValue <= 0)
                return baseScore;

            // First evaluate the effect itself to establish base score
            var target = _effectEvaluator?.GetBestTargetForEffect(SpellEffect.Draw, true, boardState);
            baseScore = _effectEvaluator?.EvaluateEffect(SpellEffect.Draw, true, target, boardState) ?? 0f;

            // Get current and maximum hand sizes
            int currentHandSize = boardState.EnemyHandSize;
            int maxHandSize = boardState.EnemyMaxHandSize;

            // If max hand size data isn't available, use a reasonable default
            if (maxHandSize > 10)
            {
                Debug.LogWarning($"[SpellCardEvaluator] Max hand size is unusually high: {maxHandSize}");
            }

            // Calculate how full the hand is as a percentage (0.0 to 1.0)
            float handFullnessRatio = (float)currentHandSize / maxHandSize;
            int fullnessPercentage = Mathf.FloorToInt(handFullnessRatio * 100);
            float emptyPercentage = 1.0f - handFullnessRatio;

            // Base multiplier calculation
            float baseMultiplier = 1.0f + (emptyPercentage * 1.5f);

            // Add critical thresholds based on hand fullness percentage
            if (fullnessPercentage == 0) // Completely empty hand
            {
                baseMultiplier += 1.0f;
                Debug.Log($"[SpellCardEvaluator] Draw card {card.CardName} is extremely valuable with empty hand");
            }
            else if (fullnessPercentage <= 20) // 20% full
            {
                baseMultiplier += 0.7f;
                Debug.Log($"[SpellCardEvaluator] Draw card {card.CardName} is very valuable with almost empty hand ({fullnessPercentage}% full)");
            }
            else if (fullnessPercentage <= 40) // 40% full
            {
                baseMultiplier += 0.4f;
                Debug.Log($"[SpellCardEvaluator] Draw card {card.CardName} is highly valuable with nearly empty hand ({fullnessPercentage}% full)");
            }
            else if (fullnessPercentage <= 50) // 50% full
            {
                baseMultiplier += 0.2f;
                Debug.Log($"[SpellCardEvaluator] Draw card {card.CardName} is somewhat valuable with half-full hand ({fullnessPercentage}% full)");
            }

            // Apply card-specific draw value scaling - cards that draw more are proportionally more valuable
            float drawValueMultiplier = Mathf.Min(1.0f + (drawValue * 0.1f), 1.5f);
            if (emptyPercentage > 0.3f) // Boost multi-draw cards when hand is at least 30% empty
            {
                baseMultiplier *= drawValueMultiplier;
                if (drawValue > 1)
                {
                    Debug.Log($"[SpellCardEvaluator] Additional {drawValueMultiplier:F2}x multiplier for drawing {drawValue} cards");
                }
            }

            // Consider hand overflow - calculate efficiency
            int availableSlots = maxHandSize - currentHandSize;
            float drawEfficiency = availableSlots >= drawValue ? 1.0f : (float)availableSlots / drawValue;

            // Only apply waste penalty when we would actually waste draws
            if (drawEfficiency < 1.0f)
            {
                // Significant penalty for wasting draws
                baseMultiplier *= (0.3f + (drawEfficiency * 0.7f));
                Debug.Log($"[SpellCardEvaluator] Efficiency penalty ({drawEfficiency:P0}) for {card.CardName} - would waste {drawValue - availableSlots} draws");
            }

            // DECK SIZE CHECK - Critical logic
            // Consider deck size - more critical when deck is small
            if (boardState.EnemyDeckSize < drawValue)
            {
                // Extreme penalty when there aren't enough cards to draw
                baseMultiplier *= 0.1f;
                Debug.Log($"[SpellCardEvaluator] Critical penalty for {card.CardName} - not enough cards in deck ({boardState.EnemyDeckSize} cards left)");
            }
            else if (boardState.EnemyDeckSize < drawValue * 2)
            {
                // Severe penalty when the draw would nearly empty the deck
                baseMultiplier *= 0.5f;
                Debug.Log($"[SpellCardEvaluator] Severe penalty for {card.CardName} - would nearly empty deck ({boardState.EnemyDeckSize} cards left)");
            }
            else if (boardState.EnemyDeckSize < 5)
            {
                // Moderate penalty for low deck
                baseMultiplier *= 0.8f;
                Debug.Log($"[SpellCardEvaluator] Moderate penalty for {card.CardName} due to low deck size ({boardState.EnemyDeckSize} cards)");
            }

            // Strategic consideration: card draw more valuable in early turns
            if (boardState.TurnCount <= 3)
            {
                baseMultiplier *= 1.2f;
                Debug.Log($"[SpellCardEvaluator] Early game bonus for card draw");
            }

            // Apply the final multiplier to the score
            float adjustedScore = baseScore * baseMultiplier;

            Debug.Log($"[SpellCardEvaluator] Draw evaluation for {card.CardName}: Base={baseScore:F1}, " +
                      $"Hand={currentHandSize}/{maxHandSize} ({fullnessPercentage}% full), " +
                      $"Deck size={boardState.EnemyDeckSize}, " +
                      $"Final Multiplier={baseMultiplier:F2}, " +
                      $"Final Score={adjustedScore:F1}");

            return adjustedScore;
        }

        /// <summary>
        /// Applies specialized logic for specific effect combinations without duplicating
        /// board state evaluator logic
        /// </summary>
        private void ApplySpecializedEffectCombinationLogic(
            Card card, BoardState boardState, ref float totalScore,
            bool hasHealEffect, bool hasDamageEffect, bool hasBurnEffect)
        {
            // These are card-specific evaluations that don't overlap with board state evaluation

            // Emergency healing value when at critical health
            if (hasHealEffect && boardState.EnemyHealth < boardState.EnemyMaxHealth * CRITICAL_ENEMY_HEALTH_THRESHOLD)
            {
                float emergencyHealValue = 20f; // Reduced from previous 25f to avoid inflation
                totalScore += emergencyHealValue;
                Debug.Log($"[SpellCardEvaluator] Emergency heal value for {card.CardName} at low health (+{emergencyHealValue})");
            }

            // Lethal potential for damage spells when player is at low health
            if ((hasDamageEffect || hasBurnEffect) && boardState.PlayerHealth <= LOW_PLAYER_HEALTH_THRESHOLD)
            {
                float lethalPotentialValue = 25f; // Reduced from previous 35f to avoid inflation
                totalScore += lethalPotentialValue;
                Debug.Log($"[SpellCardEvaluator] Lethal potential for damage spell {card.CardName} (+{lethalPotentialValue})");
            }
        }

        /// <summary>
        /// Apply turn order considerations specific to spell types
        /// </summary>
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
                    totalScore *= 1.2f; // Reduced from previous 1.3f to avoid inflation
                    Debug.Log($"[SpellCardEvaluator] Boosting heal spell {card.CardName} since player goes first next turn");

                    // Special case for heal + bloodprice combination
                    if (hasBloodpriceEffect)
                    {
                        // Calculate heal-to-bloodprice ratio
                        float healAmount = card.CardType.EffectValue;
                        float bloodpriceAmount = card.CardType.BloodpriceValue;
                        float ratio = bloodpriceAmount > 0 ? healAmount / bloodpriceAmount : 0;

                        if (ratio >= 2.0f)
                        {
                            // Very efficient healing - worth the bloodprice
                            totalScore *= 1.15f; // Reduced from previous 1.2f
                            Debug.Log($"[SpellCardEvaluator] Heal/Bloodprice card {card.CardName} is efficient with ratio {ratio:F2}");
                        }
                        else if (ratio < 1.0f && boardState.EnemyHealth < boardState.EnemyMaxHealth * CRITICAL_ENEMY_HEALTH_THRESHOLD)
                        {
                            // Inefficient healing at low health is dangerous
                            totalScore *= 0.6f;
                            Debug.Log($"[SpellCardEvaluator] Heal/Bloodprice card {card.CardName} is too risky at low health");
                        }
                    }
                }

                // Card draw less valuable with player going first next turn
                if (hasDrawEffect && !hasBloodpriceEffect)
                {
                    totalScore *= 0.9f;
                }

                // Direct damage to player health more valuable with player going first
                if ((hasDamageEffect || hasBurnEffect) && boardState.PlayerHealth <= LOW_PLAYER_HEALTH_THRESHOLD)
                {
                    totalScore *= 1.3f; // Reduced from previous 1.4f to avoid inflation
                    Debug.Log($"[SpellCardEvaluator] Boosting damage spell {card.CardName} for potential lethal before player's turn");
                }
            }
            else
            {
                // Enemy goes first next - can be more strategic

                // Card draw more valuable when we go first next turn
                if (hasDrawEffect)
                {
                    totalScore *= 1.15f; // Reduced from previous 1.2f to avoid inflation
                    Debug.Log($"[SpellCardEvaluator] Boosting draw spell {card.CardName} for more options on our next turn");
                }

                // Burn effects more valuable with follow-up turn
                if (hasBurnEffect)
                {
                    totalScore *= 1.2f; // Reduced from previous 1.25f to avoid inflation
                    Debug.Log($"[SpellCardEvaluator] Boosting burn spell {card.CardName} since we can follow-up next turn");
                }

                // We can be slightly more aggressive with bloodprice when we go next
                if (hasBloodpriceEffect && hasHealEffect && boardState.EnemyHealth > boardState.EnemyMaxHealth * 0.3f)
                {
                    totalScore *= 1.1f;
                    Debug.Log($"[SpellCardEvaluator] More willing to use Heal/Bloodprice card {card.CardName} when we go first next turn");
                }
            }
        }

        /// <summary>
        /// Special evaluation for cards with both Draw and Bloodprice effects
        /// </summary>
        private void ApplyDrawBloodpriceEvaluation(
            Card card, BoardState boardState, ref float totalScore)
        {
            // Calculate the draw-to-bloodprice value ratio
            float bloodpriceValue = card.CardType.BloodpriceValue > 0 ? card.CardType.BloodpriceValue : 1f;
            float drawValueRatio = card.CardType.DrawValue / bloodpriceValue;

            // If we get more cards than health lost, increase the score
            if (drawValueRatio > 1.5f)
            {
                totalScore *= 1.15f; // Reduced from previous 1.2f to avoid inflation
                Debug.Log($"[SpellCardEvaluator] Card {card.CardName} has favorable draw-to-bloodprice ratio: {drawValueRatio}");
            }

            // Consider current health situation
            if (boardState.EnemyHealth < LOW_ENEMY_HEALTH_THRESHOLD && card.CardType.BloodpriceValue > 3)
            {
                totalScore *= 0.65f; // Adjusted from previous 0.6f for consistency
                Debug.Log($"[SpellCardEvaluator] Reduced score for {card.CardName} due to low health ({boardState.EnemyHealth})");

                // Further reduce if player goes first next turn - too risky
                if (boardState.IsNextTurnPlayerFirst)
                {
                    totalScore *= 0.75f; // Adjusted from previous 0.7f for consistency
                    Debug.Log($"[SpellCardEvaluator] Further reducing bloodprice card {card.CardName} when low health and player goes first next turn");
                }
            }
        }
    }


}
