using EnemyInteraction.Evaluation;
using EnemyInteraction.Models;
using System.Linq;
using UnityEngine;

namespace EnemyInteraction.Managers.Evaluation
{
    public class MonsterCardEvaluator
    {
        private readonly IKeywordEvaluator _keywordEvaluator;

        public MonsterCardEvaluator(IKeywordEvaluator keywordEvaluator)
        {
            _keywordEvaluator = keywordEvaluator;
        }

        public float EvaluateMonsterCard(Card card, BoardState boardState)
        {
            float score = card.CardType.AttackPower * 1.0f + card.CardType.Health * 0.7f;
            float attackHealthRatio = card.CardType.AttackPower / (float)Mathf.Max(1, card.CardType.Health);

            // Prioritize ranged units more
            if (card.CardType.HasKeyword(Keywords.MonsterKeyword.Ranged))
            {
                score += 30f + (attackHealthRatio > 1.5f ? 20f : 0f);

                // Ranged units with low health are risky if player goes next
                if (boardState.IsNextTurnPlayerFirst && card.CardType.Health <= 2)
                {
                    score -= 15f;
                    Debug.Log($"[MonsterCardEvaluator] Lowering score for fragile ranged unit {card.CardName} when player goes first next turn");
                }
            }
            else
            {
                if (card.CardType.Health >= 5) score += 25f;
                if (card.CardType.Health <= 2 && card.CardType.AttackPower > 3) score -= 10f;
            }

            // Add specific handling for Tough and Overwhelm keywords
            bool hasTough = card.CardType.HasKeyword(Keywords.MonsterKeyword.Tough);
            bool hasOverwhelm = card.CardType.HasKeyword(Keywords.MonsterKeyword.Overwhelm);

            EvaluateToughKeyword(hasTough, card, boardState, ref score);
            EvaluateOverwhelmKeyword(hasOverwhelm, card, boardState, ref score);

            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                // Skip keywords we've already explicitly handled
                if (keyword == Keywords.MonsterKeyword.Ranged ||
                    (keyword == Keywords.MonsterKeyword.Tough && hasTough) ||
                    (keyword == Keywords.MonsterKeyword.Overwhelm && hasOverwhelm))
                    continue;

                if (card.CardType.HasKeyword(keyword))
                {
                    score += (_keywordEvaluator?.EvaluateKeyword(keyword, true, boardState) ?? 0f) * 1.2f;
                }
            }

            return score;
        }

        private void EvaluateToughKeyword(bool hasTough, Card card, BoardState boardState, ref float score)
        {
            if (!hasTough) return;

            if (boardState.HealthAdvantage < 0)
            {
                // When at health disadvantage, tough units help us stabilize
                score += 25f;
            }

            // Tough is more valuable on high-health units
            if (card.CardType.Health >= 4)
            {
                score += 15f;
            }

            // Check if player has high-attack monsters that Tough would be good against
            bool playerHasHighAttackUnits = boardState.PlayerMonsters != null &&
                                          boardState.PlayerMonsters.Any(m => m.GetAttack() >= 4);
            if (playerHasHighAttackUnits)
            {
                score += 20f;
                Debug.Log($"[MonsterCardEvaluator] Prioritizing Tough unit {card.CardName} against high-attack enemies");
            }

            // Turn order consideration for Tough
            if (boardState.IsNextTurnPlayerFirst)
            {
                score += 20f; // Tough is more valuable when player goes next
                Debug.Log($"[MonsterCardEvaluator] Prioritizing Tough unit {card.CardName} since player goes first next turn");
            }
        }

        private void EvaluateOverwhelmKeyword(bool hasOverwhelm, Card card, BoardState boardState, ref float score)
        {
            if (!hasOverwhelm) return;

            float overwhelmBonus = 0f;

            // Limit health advantage bonus
            if (boardState.HealthAdvantage > 0)
            {
                overwhelmBonus += 15f; // Reduced from 30f
            }

            // Cap the high attack bonus
            if (card.CardType.AttackPower >= 4)
            {
                // Use a fixed bonus instead of a multiplier
                overwhelmBonus += 15f + Mathf.Min(card.CardType.AttackPower * 0.8f, 15f); // Capped at +30 total
            }

            // Low health units bonus (reduced)
            bool playerHasLowHealthUnits = boardState.PlayerMonsters != null &&
                                         boardState.PlayerMonsters.Any(m => m.GetHealth() <= 2);
            if (playerHasLowHealthUnits)
            {
                overwhelmBonus += 15f; // Reduced from 25f
                Debug.Log($"[MonsterCardEvaluator] Overwhelm unit {card.CardName} effective against low-health defenders");
            }

            // Low player health bonus (reduced)
            if (boardState.PlayerHealth <= 10)
            {
                overwhelmBonus += 20f; // Reduced from 40f
                Debug.Log($"[MonsterCardEvaluator] Overwhelm unit {card.CardName} valuable for potential lethal damage");
            }

            // Turn order bonus (reduced)
            if (!boardState.IsNextTurnPlayerFirst)
            {
                overwhelmBonus += 15f; // Reduced from 30f
                Debug.Log($"[MonsterCardEvaluator] Overwhelm unit {card.CardName} valuable for consecutive attacks next turn");
            }

            // Add splash damage evaluation with capped values
            float splashBonus = EvaluateSplashDamageBalanced(card, boardState);
            overwhelmBonus += splashBonus;

            // Cap the total Overwhelm bonus to avoid extreme values
            float cappedBonus = Mathf.Min(overwhelmBonus, 60f);
            score += cappedBonus;

            if (cappedBonus < overwhelmBonus)
            {
                Debug.Log($"[MonsterCardEvaluator] Capped Overwhelm bonus from {overwhelmBonus} to {cappedBonus}");
            }
        }

        // New balanced splash damage evaluation method
        private float EvaluateSplashDamageBalanced(Card card, BoardState boardState)
        {
            if (boardState.PlayerMonsters == null || boardState.PlayerMonsters.Count == 0)
                return 0f;

            float splashBonus = 0f;
            float splashDamage = card.CardType.AttackPower * 0.5f;

            // Count splash kill potential (with reduced value)
            int potentialSplashKills = boardState.PlayerMonsters.Count(m => m.GetHealth() <= splashDamage);
            if (potentialSplashKills > 0)
            {
                // Cap the bonus per kill and the total bonus
                float killBonus = Mathf.Min(potentialSplashKills * 10f, 25f);
                splashBonus += killBonus;
                Debug.Log($"[MonsterCardEvaluator] Overwhelm unit could kill {potentialSplashKills} units with splash damage, adding {killBonus} bonus");
            }

            // Bonus for multiple low health units (reduced)
            int lowHealthUnits = boardState.PlayerMonsters.Count(m => m.GetHealth() <= splashDamage * 2);
            if (lowHealthUnits >= 2)
            {
                // Cap the bonus for low health units
                float healthBonus = Mathf.Min(lowHealthUnits * 5f, 15f);
                splashBonus += healthBonus;
                Debug.Log($"[MonsterCardEvaluator] Overwhelm unit effective against {lowHealthUnits} low-health units, adding {healthBonus} bonus");
            }

            return splashBonus;
        }
    }
}
