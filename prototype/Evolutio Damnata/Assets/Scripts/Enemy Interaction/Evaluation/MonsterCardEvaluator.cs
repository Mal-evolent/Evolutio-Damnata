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
            
            if (boardState.HealthAdvantage > 0)
            {
                // When already ahead, Overwhelm helps close the game faster
                score += 30f;
            }

            // High attack is more valuable with Overwhelm
            if (card.CardType.AttackPower >= 4)
            {
                score += card.CardType.AttackPower * 2.5f;
            }

            // Check if player has lots of low-health units that Overwhelm would be good against
            bool playerHasLowHealthUnits = boardState.PlayerMonsters != null &&
                                         boardState.PlayerMonsters.Any(m => m.GetHealth() <= 2);
            if (playerHasLowHealthUnits)
            {
                score += 25f;
                Debug.Log($"[MonsterCardEvaluator] Prioritizing Overwhelm unit {card.CardName} against low-health defenders");
            }

            // Overwhelm is extremely valuable when player is low on health
            if (boardState.PlayerHealth <= 10)
            {
                score += 40f;
                Debug.Log($"[MonsterCardEvaluator] Prioritizing Overwhelm unit {card.CardName} for potential lethal damage");
            }

            // Turn order consideration for Overwhelm
            if (!boardState.IsNextTurnPlayerFirst)
            {
                // If we go first next turn, Overwhelm units can follow up with another attack
                score += 30f;
                Debug.Log($"[MonsterCardEvaluator] Prioritizing Overwhelm unit {card.CardName} for consecutive attacks next turn");
            }

            EvaluateSplashDamage(card, boardState, ref score);
        }

        private void EvaluateSplashDamage(Card card, BoardState boardState, ref float score)
        {
            if (boardState.PlayerMonsters == null || boardState.PlayerMonsters.Count == 0) return;
            
            // Estimate splash damage (assuming splash is 50% of attack)
            float splashDamage = card.CardType.AttackPower * 0.5f;

            // Count how many units would die to splash damage
            int potentialSplashKills = boardState.PlayerMonsters.Count(m =>
                m.GetHealth() <= splashDamage);

            // Value multi-kill potential highly
            if (potentialSplashKills > 0)
            {
                float splashKillBonus = potentialSplashKills * 20f;
                score += splashKillBonus;
                Debug.Log($"[MonsterCardEvaluator] Overwhelm unit {card.CardName} could kill {potentialSplashKills} units with splash damage! Adding {splashKillBonus} to score.");
            }

            // Additional bonus if there are clustered low-health units (even if they won't die)
            int lowHealthUnits = boardState.PlayerMonsters.Count(m => m.GetHealth() <= splashDamage * 2);
            if (lowHealthUnits >= 2)
            {
                float damageEfficiencyBonus = lowHealthUnits * 10f;
                score += damageEfficiencyBonus;
                Debug.Log($"[MonsterCardEvaluator] Overwhelm unit {card.CardName} effective against {lowHealthUnits} clustered low-health units. Adding {damageEfficiencyBonus} to score.");
            }

            // Consider splash value against combinations of units and player health
            if (boardState.PlayerHealth <= splashDamage * 2 && boardState.PlayerMonsters.Count >= 1)
            {
                score += 50f;
                Debug.Log($"[MonsterCardEvaluator] Overwhelm unit {card.CardName} could damage both units AND player with low health! Major strategic advantage.");
            }
        }
    }
}
