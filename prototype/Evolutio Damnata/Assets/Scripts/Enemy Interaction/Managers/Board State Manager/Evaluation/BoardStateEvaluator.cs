using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using EnemyInteraction.Models;
using EnemyInteraction.Interfaces;

namespace EnemyInteraction.Managers
{
    public class BoardStateEvaluator
    {
        private readonly ICombatManager _combatManager;
        private readonly BoardStateSettings _settings;

        public BoardStateEvaluator(ICombatManager combatManager, BoardStateSettings settings)
        {
            _combatManager = combatManager;
            _settings = settings;
        }

        /// <summary>
        /// Calculates the overall board control value for a set of entities
        /// Focuses on the raw strength of units on the board, without card-specific evaluations
        /// </summary>
        public float CalculateBoardControl(List<EntityManager> entities, bool isEnemy)
        {
            if (entities == null || entities.Count == 0) return 0f;

            bool playerGoesFirstNextTurn = _combatManager?.PlayerGoesFirst ?? false;
            float totalValue = 0f;
            int entityCount = entities.Count;

            // Apply non-linear scaling with entity count
            float countMultiplier = Mathf.Sqrt(entityCount);

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                // Calculate base value of the entity's stats
                float attackValue = entity.GetAttack() * 1.2f;
                float healthValue = entity.GetHealth();
                float baseValue = attackValue + healthValue;

                // Apply keyword value multipliers - this is a board-wide evaluation
                // rather than a card-specific one
                float keywordValue = CalculateKeywordValue(entity);

                // Health ratio influences entity value
                float healthRatio = entity.GetHealth() / entity.GetMaxHealth();
                float healthFactor = healthRatio < 0.5f ? 0.7f + (0.6f * healthRatio) : 1f;

                // Consider available attacks
                float attacksFactor = entity.GetRemainingAttacks() > 0 ? 1.2f : 1f;

                // Combine all factors 
                float entityValue = baseValue * keywordValue * healthFactor * attacksFactor;
                totalValue += entityValue;

                // Debug for significant entities
                if (entityValue > 15)
                {
                    Debug.Log($"[BoardStateManager] High-value {(isEnemy ? "enemy" : "player")} entity: {entityValue:F1} " +
                        $"({entity.GetAttack()} atk, {entity.GetHealth()} hp, keywords: {keywordValue:F2}x)");
                }
            }

            // Apply non-linear board size scaling
            return totalValue * countMultiplier;
        }

        /// <summary>
        /// Calculate the core value of keywords for a board entity - 
        /// This evaluates the absolute contribution to board strength,
        /// not the card-specific evaluation done by KeywordEvaluator
        /// </summary>
        public float CalculateKeywordValue(EntityManager entity)
        {
            float multiplier = 1f;
            int keywordCount = 0;

            if (entity.HasKeyword(Keywords.MonsterKeyword.Taunt))
            {
                float healthRatio = entity.GetHealth() / entity.GetMaxHealth();
                multiplier *= _settings.TauntValue * Mathf.Max(0.7f, healthRatio);
                keywordCount++;
            }

            if (entity.HasKeyword(Keywords.MonsterKeyword.Ranged))
            {
                // Ranged scales better with attack
                multiplier *= _settings.RangedValue * (1f + (0.03f * entity.GetAttack()));
                keywordCount++;
            }

            if (entity.HasKeyword(Keywords.MonsterKeyword.Tough))
            {
                // Tough scales better with health
                multiplier *= _settings.ToughValue * (1f + (0.02f * entity.GetHealth()));
                keywordCount++;
            }

            if (entity.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
            {
                // Overwhelm scales with attack
                multiplier *= _settings.OverwhelmValue * (1f + (0.03f * entity.GetAttack()));
                keywordCount++;
            }

            // Multiple keywords synergy bonus
            if (keywordCount > 1)
            {
                multiplier *= (1f + (_settings.KeywordSynergyBonus * (keywordCount - 1)));
            }

            return multiplier;
        }

        /// <summary>
        /// Applies tactical formation bonuses to board control
        /// This evaluates synergies between units on the board
        /// </summary>
        public void ApplyBoardPositioningFactors(BoardState state)
        {
            int enemyCount = state.EnemyMonsters.Count;
            int playerCount = state.PlayerMonsters.Count;

            // Board presence advantage
            if (enemyCount > 0 && playerCount > 0)
            {
                float boardPresenceRatio = enemyCount / (float)playerCount;
                if (boardPresenceRatio > 1.5f)
                {
                    float bonus = _settings.BoardPresenceMultiplier * (boardPresenceRatio - 1f);
                    state.EnemyBoardControl *= (1f + bonus);
                    Debug.Log($"[BoardStateManager] Enemy has board presence advantage: {enemyCount} vs {playerCount} units, boosting by {bonus:F2}");
                }
            }

            // Check for tactical formations like taunters protecting ranged units
            int taunters = state.EnemyMonsters.Count(e => e.HasKeyword(Keywords.MonsterKeyword.Taunt));
            int rangedUnits = state.EnemyMonsters.Count(e => e.HasKeyword(Keywords.MonsterKeyword.Ranged));

            if (taunters > 0 && rangedUnits > 0)
            {
                float formationBonus = 0.15f * Mathf.Min(taunters, rangedUnits);
                state.EnemyBoardControl *= (1f + formationBonus);
                Debug.Log($"[BoardStateManager] Enemy has good formation with {taunters} taunters protecting {rangedUnits} ranged units, bonus: {formationBonus:F2}");
            }
        }

        /// <summary>
        /// Applies resource advantage factors to board control
        /// This is unique to the overall board evaluation and doesn't duplicate card evaluations
        /// </summary>
        public void ApplyResourceAdvantages(BoardState state)
        {
            if (state == null)
            {
                Debug.LogError("[BoardStateEvaluator] BoardState is null in ApplyResourceAdvantages.");
                return;
            }

            // Check if _settings is null
            if (_settings == null)
            {
                Debug.LogError("[BoardStateEvaluator] BoardStateSettings is null in ApplyResourceAdvantages.");
                return;
            }

            // Ensure EnemyBoardControl is initialized
            if (float.IsNaN(state.EnemyBoardControl))
            {
                state.EnemyBoardControl = 0;
                Debug.LogWarning("[BoardStateEvaluator] EnemyBoardControl was NaN, initializing to 0.");
            }

            // Card advantage - apply only if it's properly set and greater than 0
            if (state.CardAdvantage > 0)
            {
                try
                {
                    float cardAdvantageBonus = state.CardAdvantage * _settings.ResourceAdvantageWeight;
                    state.EnemyBoardControl += cardAdvantageBonus;
                    Debug.Log($"[BoardStateManager] Enemy has card advantage of {state.CardAdvantage}, adding {cardAdvantageBonus:F2} to board control");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BoardStateEvaluator] Error applying card advantage: {ex.Message}");
                }
            }

            // Mana advantage - apply only if it's properly set and greater than 0
            if (state.EnemyMana > 0)
            {
                try
                {
                    float manaBonus = state.EnemyMana * _settings.ResourceAdvantageWeight;
                    state.EnemyBoardControl += manaBonus;
                    Debug.Log($"[BoardStateManager] Enemy has {state.EnemyMana} mana, adding {manaBonus:F2} to board control");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BoardStateEvaluator] Error applying mana bonus: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Applies health factors to the board state evaluation, focusing on
        /// the general impact of health on gameplay rather than card-specific evaluations
        /// </summary>
        public void ApplyHealthBasedFactors(BoardState state)
        {
            // Calculate health ratios
            float enemyHealthRatio = state.EnemyHealth / (float)state.EnemyMaxHealth;
            float playerHealthRatio = state.PlayerHealth / (float)state.PlayerMaxHealth;

            // Basic health-based factors
            float baseHealthFactor = 1.0f;

            // Adjust based on game progression
            if (state.TurnCount <= 3)
            {
                // Early game - health is less critical
                baseHealthFactor *= 0.8f;
            }
            else if (state.TurnCount >= _settings.LateGameTurnThreshold)
            {
                // Late game - health becomes more critical
                baseHealthFactor *= 1.5f;
            }

            // Store the health importance factor for use by other evaluators
            state.HealthImportanceFactor = baseHealthFactor;

            // Base health influence using the importance factor
            float healthFactor = _settings.HealthInfluenceFactor * baseHealthFactor;

            // Add raw health values to board control, scaled by health factor
            state.EnemyBoardControl += state.EnemyHealth * healthFactor;
            state.PlayerBoardControl += state.PlayerHealth * healthFactor;

            // Critical health thresholds - apply these to board control only,
            // card evaluators will handle specific card evaluations
            if (enemyHealthRatio < _settings.CriticalHealthThreshold)
            {
                float penalty = 0.2f * (1f - (enemyHealthRatio / _settings.CriticalHealthThreshold));
                state.EnemyBoardControl *= (1f - penalty);
                Debug.Log($"[BoardStateManager] Enemy at critical health ({enemyHealthRatio:P0}), reducing board control by {penalty:P0}");
            }

            if (playerHealthRatio < _settings.CriticalHealthThreshold)
            {
                float bonus = 0.15f * (1f - (playerHealthRatio / _settings.CriticalHealthThreshold));
                state.EnemyBoardControl *= (1f + bonus);
                Debug.Log($"[BoardStateManager] Player at critical health ({playerHealthRatio:P0}), increasing enemy advantage by {bonus:P0}");
            }

            // Lethal detection - this is a board-wide assessment
            float totalEnemyAttack = state.EnemyMonsters.Sum(e => e.GetAttack());
            if (totalEnemyAttack >= state.PlayerHealth && !state.IsNextTurnPlayerFirst)
            {
                // Enemy can potentially win next turn
                state.EnemyBoardControl *= 1.5f;
                Debug.Log("[BoardStateManager] Enemy has potential lethal next turn - critical advantage");
            }
        }

        /// <summary>
        /// Applies overall turn order influence to board control
        /// Focuses on strategic board advantage, not card-specific evaluations
        /// </summary>
        public void ApplyTurnOrderInfluence(BoardState state)
        {
            // Apply basic turn order advantage/disadvantage
            if (!state.IsNextTurnPlayerFirst)
            {
                // Enemy goes first - apply a basic advantage factor
                state.EnemyBoardControl *= 1.15f;
                Debug.Log("[BoardStateManager] Enemy goes first next turn - applying basic turn advantage factor");

                // Only calculate lethal potential here, card-specific evaluations happen elsewhere
                float potentialDamage = state.EnemyMonsters.Sum(e => e.GetAttack());
                if (potentialDamage > state.PlayerHealth * 0.35f)
                {
                    float advantage = Mathf.Min(0.25f, potentialDamage / state.PlayerHealth);
                    state.EnemyBoardControl *= (1f + advantage);
                    Debug.Log($"[BoardStateManager] Enemy going first with lethal potential: {potentialDamage} damage");
                }
            }
            else
            {
                // Player goes first - apply a basic disadvantage factor
                state.EnemyBoardControl *= 0.9f;
                Debug.Log("[BoardStateManager] Player goes first next turn - applying basic turn disadvantage factor");
            }
        }

        /// <summary>
        /// Apply all evaluation factors to the board state in the correct sequence
        /// </summary>
        /// <param name="state">The board state to apply factors to</param>
        public void ApplyAllFactors(BoardState state)
        {
            if (state == null)
            {
                Debug.LogError("[BoardStateEvaluator] Cannot apply factors to null board state");
                return;
            }

            // Apply all evaluation factors in the appropriate order
            ApplyBoardPositioningFactors(state);
            ApplyResourceAdvantages(state);
            ApplyHealthBasedFactors(state);
            ApplyTurnOrderInfluence(state);

            Debug.Log($"[BoardStateEvaluator] All evaluation factors applied. " +
                $"Final enemy board control: {state.EnemyBoardControl:F2}, " +
                $"player board control: {state.PlayerBoardControl:F2}");
        }

    }
}
