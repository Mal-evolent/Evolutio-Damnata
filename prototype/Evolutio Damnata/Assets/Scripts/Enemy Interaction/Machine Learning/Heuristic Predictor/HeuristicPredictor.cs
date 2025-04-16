using EnemyInteraction.Models;
using System.Linq;
using UnityEngine;

namespace EnemyInteraction.MachineLearning
{
    public class HeuristicPredictor
    {
        private readonly CardHistory cardHistorySystem;
        
        public HeuristicPredictor()
        {
            cardHistorySystem = CardHistory.Instance;
        }
        
        // Enhanced heuristic prediction with card history and enum awareness
        public float GetHeuristicPrediction(BoardState state, int actionType)
        {
            float prediction = 0.5f;

            if (actionType == 0) // Attack action
            {
                // Base logic with health consideration
                prediction = state.PlayerHealth < state.PlayerMaxHealth * 0.5f ? 0.8f : 0.3f;

                // Phase and board control logic remains unchanged
                if (state.CurrentPhase == CombatPhase.EnemyPrep)
                {
                    prediction *= 0.6f;
                }
                else if (state.CurrentPhase == CombatPhase.EnemyCombat)
                {
                    prediction *= 1.3f;
                }

                if (!state.IsNextTurnPlayerFirst && state.TurnCount < 5)
                {
                    prediction *= 1.2f;
                }

                if (state.EnemyBoardControl > state.PlayerBoardControl * 1.5f)
                {
                    prediction *= 1.25f;
                }

                // Card history influence with enum-based checks
                if (cardHistorySystem != null)
                {
                    int playerCards = cardHistorySystem.GetPlayerCardsPlayed();
                    if (playerCards > 0)
                    {
                        var playerHistory = cardHistorySystem.GetPlayerCardPlays();

                        // Look for aggressive cards using monster keywords
                        float aggressiveRatio = playerHistory.Count(c =>
                            HasMonsterKeyword(c.Keywords, Keywords.MonsterKeyword.Overwhelm) ||
                            HasMonsterKeyword(c.Keywords, Keywords.MonsterKeyword.Ranged)) / (float)playerCards;

                        prediction = (prediction + aggressiveRatio) / 2f;
                    }

                    // Same as before
                    int recentCards = 0;
                    for (int i = 0; i < 2; i++)
                    {
                        if (state.TurnCount - i > 0)
                        {
                            recentCards += cardHistorySystem.GetCardsPlayedInTurn(state.TurnCount - i);
                        }
                    }

                    if (recentCards > 4)
                    {
                        prediction *= 1.15f;
                    }
                }
            }
            else // Spell action
            {
                // Base logic with health consideration
                prediction = state.EnemyHealth < state.EnemyMaxHealth * 0.5f ? 0.7f : 0.4f;

                // Phase-specific modifications remain unchanged
                if (state.CurrentPhase == CombatPhase.EnemyPrep)
                {
                    prediction *= 1.2f;
                }
                else if (state.CurrentPhase == CombatPhase.EnemyCombat && state.BoardControlDifference < 0)
                {
                    prediction *= 1.15f;
                }

                if (state.IsNextTurnPlayerFirst && state.BoardControlDifference < 0)
                {
                    prediction *= 1.15f;
                }

                if (state.TurnCount <= 3)
                {
                    prediction *= 1.1f;
                }

                // Card history influence with enum-based checks
                if (cardHistorySystem != null)
                {
                    int playerCards = cardHistorySystem.GetPlayerCardsPlayed();
                    if (playerCards > 0)
                    {
                        var playerHistory = cardHistorySystem.GetPlayerCardPlays();

                        // Count cards with specific spell effects using enums
                        float spellRatio = playerHistory.Count(c =>
                            HasSpellEffect(c.Keywords, SpellEffect.Damage) ||
                            HasSpellEffect(c.Keywords, SpellEffect.Heal) ||
                            HasSpellEffect(c.Keywords, SpellEffect.Draw) ||
                            HasSpellEffect(c.Keywords, SpellEffect.Burn)) /
                            (float)playerCards;

                        prediction = (prediction + spellRatio) / 2f;
                    }
                }
            }

            return Mathf.Clamp01(prediction);
        }
        
        // Helper method to check for spell effect keywords in card history
        private bool HasSpellEffect(string keywords, SpellEffect effectType)
        {
            return keywords != null && keywords.Contains(effectType.ToString());
        }

        // Helper method to check for monster keywords in card history
        private bool HasMonsterKeyword(string keywords, Keywords.MonsterKeyword keyword)
        {
            return keywords != null && keywords.Contains(keyword.ToString());
        }
    }
}
