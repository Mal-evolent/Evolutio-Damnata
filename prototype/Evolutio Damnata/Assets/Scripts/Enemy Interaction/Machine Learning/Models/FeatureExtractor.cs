using EnemyInteraction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyInteraction.MachineLearning
{
    public class FeatureExtractor
    {
        // Configuration
        private const int BaseInputSize = 15;
        private const int CardHistoryFeatures = 8;
        private const int InputSize = BaseInputSize + CardHistoryFeatures;
        
        // Reference to card history system
        private CardHistory cardHistorySystem;
        
        public FeatureExtractor()
        {
            // Find the CardHistory system
            cardHistorySystem = CardHistory.Instance;
            if (cardHistorySystem == null)
            {
                Debug.LogWarning("[FeatureExtractor] CardHistory system not found. Running with limited features.");
            }
        }
        
        // Feature extraction - converts game state and card history to numerical features
        public float[] ExtractFeatures(BoardState state, int actionType)
        {
            float[] features = new float[InputSize];

            // Base features (same as before)
            features[0] = state.PlayerHealth / (float)state.PlayerMaxHealth;
            features[1] = state.EnemyHealth / (float)state.EnemyMaxHealth;
            features[2] = Mathf.Clamp(state.PlayerBoardControl / 20f, 0f, 1f);
            features[3] = Mathf.Clamp(state.EnemyBoardControl / 20f, 0f, 1f);
            features[4] = Mathf.Clamp(state.TurnCount / 20f, 0f, 1f);
            features[5] = Mathf.Clamp(state.CardAdvantage / 5f, -1f, 1f);
            features[6] = (actionType == 0) ? 1f : 0f; // Is attack action
            features[7] = (actionType == 1) ? 1f : 0f; // Is spell action
            features[8] = (state.CurrentPhase == CombatPhase.PlayerPrep) ? 1f : 0f;
            features[9] = (state.CurrentPhase == CombatPhase.PlayerCombat) ? 1f : 0f;
            features[10] = (state.CurrentPhase == CombatPhase.EnemyPrep) ? 1f : 0f;
            features[11] = (state.CurrentPhase == CombatPhase.EnemyCombat) ? 1f : 0f;
            features[12] = (state.CurrentPhase == CombatPhase.CleanUp) ? 1f : 0f;
            features[13] = state.IsPlayerTurn ? 1f : 0f;
            features[14] = state.IsNextTurnPlayerFirst ? 1f : 0f;

            // Card History features - now with proper enum checks
            if (cardHistorySystem != null)
            {
                // Feature 15: Player spell usage ratio with proper SpellEffect enum
                int playerCards = cardHistorySystem.GetPlayerCardsPlayed();
                int totalSpellCards = 0;

                var playerCardPlays = cardHistorySystem.GetPlayerCardPlays();
                if (playerCards > 0 && playerCardPlays != null)
                {
                    foreach (var record in playerCardPlays)
                    {
                        // Check for specific spell effects using the SpellEffect enum
                        if (HasSpellEffect(record.Keywords, SpellEffect.Damage) ||
                            HasSpellEffect(record.Keywords, SpellEffect.Heal) ||
                            HasSpellEffect(record.Keywords, SpellEffect.Draw) ||
                            HasSpellEffect(record.Keywords, SpellEffect.Burn) ||
                            HasSpellEffect(record.Keywords, SpellEffect.Bloodprice))
                        {
                            totalSpellCards++;
                        }
                    }
                    features[15] = (float)totalSpellCards / playerCards;
                }
                else
                {
                    features[15] = 0.5f; // Default value when no data available
                }

                // Feature 16: Cards played this turn (normalized)
                features[16] = Mathf.Clamp01(cardHistorySystem.GetCardsPlayedInTurn(state.TurnCount) / 5f);

                // Feature 17: Player aggression trend (based on monster keywords)
                int aggressiveCards = 0;
                if (playerCards > 0 && playerCardPlays != null)
                {
                    foreach (var record in playerCardPlays)
                    {
                        // Check for aggressive monster keywords
                        if (HasMonsterKeyword(record.Keywords, Keywords.MonsterKeyword.Overwhelm) ||
                            HasMonsterKeyword(record.Keywords, Keywords.MonsterKeyword.Ranged))
                        {
                            aggressiveCards++;
                        }
                    }
                    features[17] = (float)aggressiveCards / playerCards;
                }
                else
                {
                    features[17] = 0.5f;
                }

                // Feature 18: Recent turn activity (last 3 turns)
                int recentCards = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (state.TurnCount - i > 0)
                    {
                        recentCards += cardHistorySystem.GetCardsPlayedInTurn(state.TurnCount - i);
                    }
                }
                features[18] = Mathf.Clamp01(recentCards / 10f);

                // Feature 19: Card play balance (enemy vs player)
                int enemyCards = cardHistorySystem.GetEnemyCardsPlayed();
                int totalCards = cardHistorySystem.GetTotalCardsPlayed();
                features[19] = totalCards > 0 ? (float)enemyCards / totalCards : 0.5f;

                // Feature 20: Player activity in current phase
                var currentPhaseCards = playerCardPlays.Count(c => c.TurnNumber == state.TurnCount);
                features[20] = Mathf.Clamp01(currentPhaseCards / 3f);

                // Feature 21: Defensive strategy preference (Taunt + Tough cards)
                float defensiveRatio = 0.5f;
                if (playerCardPlays != null && playerCardPlays.Count > 0)
                {
                    int defensiveCount = playerCardPlays.Count(c =>
                        HasMonsterKeyword(c.Keywords, Keywords.MonsterKeyword.Taunt) ||
                        HasMonsterKeyword(c.Keywords, Keywords.MonsterKeyword.Tough));
                    defensiveRatio = (float)defensiveCount / playerCardPlays.Count;
                }
                features[21] = defensiveRatio;

                // Feature 22: Resource manipulation preference (Draw + Bloodprice)
                float resourceManipulationRatio = 0.5f;
                if (playerCardPlays != null && playerCardPlays.Count > 0)
                {
                    int resourceCards = playerCardPlays.Count(c =>
                        HasSpellEffect(c.Keywords, SpellEffect.Draw) ||
                        HasSpellEffect(c.Keywords, SpellEffect.Bloodprice));
                    resourceManipulationRatio = (float)resourceCards / playerCardPlays.Count;
                }
                features[22] = resourceManipulationRatio;
            }
            else
            {
                // Fill with default values if CardHistory system is not available
                for (int i = BaseInputSize; i < InputSize; i++)
                {
                    features[i] = 0.5f;
                }
            }

            return features;
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
        
        public int GetInputSize()
        {
            return InputSize;
        }
    }
}
