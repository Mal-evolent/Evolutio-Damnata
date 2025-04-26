using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;
using EnemyInteraction.Utilities;

namespace EnemyInteraction.Managers
{
    public class AttackStrategyManager : MonoBehaviour, IAttackStrategyManager
    {
        #region Configuration
        [SerializeField, Range(0f, 1f), Tooltip("Chance to make suboptimal decisions")]
        private float _decisionVariance = 0.10f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Chance to randomize attack order")]
        private float _attackOrderRandomizationChance = 0.10f;

        [SerializeField, Tooltip("Health difference to switch strategies")]
        private float _healthThresholdForAggro = 8f;

        [SerializeField, Tooltip("Turn count to become more aggressive")]
        private int _aggressiveTurnThreshold = 4;

        [SerializeField, Tooltip("Whether to avoid losing the last monster")]
        private bool _avoidLosingLastMonster = true;

        [SerializeField, Range(0f, 0.3f), Tooltip("Chance to ignore last monster protection")]
        private float _lastMonsterIgnoreChance = 0.15f;

        [SerializeField, Range(1f, 10f), Tooltip("Min value ratio for beneficial last monster trade")]
        private float _valuableTradeRatio = 2.0f;
        #endregion

        private ITargetEvaluator _targetEvaluator;
        private IEntityCacheManager _entityCacheManager;

        public void Initialize(ITargetEvaluator targetEvaluator, IEntityCacheManager entityCacheManager)
        {
            _targetEvaluator = targetEvaluator;
            _entityCacheManager = entityCacheManager;
        }

        #region Public Interface Methods
        public List<EntityManager> GetAttackOrder(List<EntityManager> enemies, List<EntityManager> players,
                                             HealthIconManager healthIcon, BoardState boardState)
        {
            // Filter out valid attackers (attack > 0)
            var validAttackers = enemies.Where(e => e != null && e.GetAttack() > 0).ToList();
            LogDefensiveUnits(enemies.Where(e => e != null && e.GetAttack() == 0).ToList());

            if (validAttackers.Count == 0)
            {
                Debug.Log("[AttackStrategyManager] No valid attackers with attack > 0 available");
                return new List<EntityManager>();
            }

            var order = validAttackers.ToList();

            // Potentially randomize order
            if (Random.value < _attackOrderRandomizationChance && order.Count > 1)
                order = GetPartiallyShuffledAttackers(order);

            // Determine optimal attack order based on situation
            return CanKillPlayerThisTurn(order, players, healthIcon)
                ? OptimizeForLethal(order, players)
                : OptimizeForBoardControl(order, players, boardState);
        }

        public EntityManager SelectTarget(EntityManager attacker, List<EntityManager> playerEntities,
                                     HealthIconManager playerHealthIcon, BoardState boardState, StrategicMode mode)
        {
            if (playerEntities == null || playerEntities.Count == 0)
                return null;

            // Handle taunt units
            bool hasTaunts = playerEntities.Any(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt));
            if (hasTaunts)
            {
                var tauntTargets = playerEntities.Where(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt)).ToList();

                // Special handling for Overwhelm against taunt
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && tauntTargets.Count > 0)
                    return SelectTargetForOverwhelmAttacker(attacker, tauntTargets, boardState);

                return SelectBestTarget(attacker, tauntTargets, boardState, mode);
            }

            // Handle overwhelm with multiple targets
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && playerEntities.Count > 1)
                return SelectTargetForOverwhelmAttacker(attacker, playerEntities, boardState);

            return SelectBestTarget(attacker, playerEntities, boardState, mode);
        }

        public StrategicMode DetermineStrategicMode(BoardState boardState)
        {
            if (boardState == null)
                return StrategicMode.Defensive;

            // Calculate strategic factors
            bool healthAdvantage = boardState.EnemyHealth > boardState.PlayerHealth + _healthThresholdForAggro;
            bool boardAdvantage = boardState.EnemyBoardControl > boardState.PlayerBoardControl * 1.3f;
            bool lateGame = boardState.TurnCount > _aggressiveTurnThreshold;
            bool playerLowHealth = boardState.PlayerHealth <= 15;
            bool enemyNextTurn = !boardState.IsNextTurnPlayerFirst;
            bool hasOnlyOneMonster = CountActiveEnemyMonsters() <= 1;

            // Single monster defensive strategy
            if (hasOnlyOneMonster && Random.value > 0.25f)
            {
                Debug.Log("[AttackStrategyManager] Only one monster on field - adopting defensive strategy");
                return StrategicMode.Defensive;
            }

            // Turn order considerations
            if (enemyNextTurn)
            {
                Debug.Log("[AttackStrategyManager] Enemy goes first next turn - adopting more aggressive strategy");
                return StrategicMode.Aggro;
            }

            if (boardState.IsNextTurnPlayerFirst && !healthAdvantage && !boardAdvantage && !playerLowHealth)
            {
                Debug.Log("[AttackStrategyManager] Player goes first next turn - adopting more defensive strategy");
                return StrategicMode.Defensive;
            }

            // Situation-based strategy
            if (healthAdvantage || boardAdvantage || lateGame || playerLowHealth)
                return StrategicMode.Aggro;

            if (boardState.EnemyHealth < 15 || boardState.PlayerBoardControl > boardState.EnemyBoardControl)
                return StrategicMode.Defensive;

            // Default slightly favors aggression
            return Random.value < 0.6f ? StrategicMode.Aggro : StrategicMode.Defensive;
        }

        public bool ShouldAttackHealthIcon(EntityManager attacker, List<EntityManager> playerEntities,
                                     HealthIconManager playerHealthIcon, BoardState boardState)
        {
            if (attacker == null || playerHealthIcon == null)
                return false;

            // Occasionally make human-like mistake
            if (Random.value < _lastMonsterIgnoreChance)
            {
                Debug.Log("[AttackStrategyManager] Making human-like mistake - ignoring last monster protection");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            // Last monster protection logic
            if (_avoidLosingLastMonster && IsLastMonster(attacker) &&
                playerHealthIcon.GetHealth() > attacker.GetAttack() * 1.5f)
            {
                // Health icons don't counter-attack, so it's safe
                Debug.Log("[AttackStrategyManager] Safe direct health attack with our only monster - no counterattack risk");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            // Turn order considerations
            bool playerGoesFirstNextTurn = boardState != null && boardState.IsNextTurnPlayerFirst;

            // Priority for direct attacks when player health is low
            if (playerGoesFirstNextTurn && playerHealthIcon.GetHealth() <= 10)
            {
                Debug.Log("[AttackStrategyManager] Prioritizing direct health attack for potential lethal before player's turn");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            // Strategic approach when enemy goes next
            if (!playerGoesFirstNextTurn && playerHealthIcon.GetHealth() <= attacker.GetAttack() * 1.5f)
            {
                Debug.Log("[AttackStrategyManager] Strategic direct health attack to set up lethal on our next turn");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            return AIUtilities.CanTargetHealthIcon(playerEntities);
        }
        #endregion

        #region Target Selection Methods
        private EntityManager SelectTargetForOverwhelmAttacker(EntityManager attacker, List<EntityManager> targets, BoardState boardState)
        {
            if (targets == null || targets.Count <= 1)
                return targets?.FirstOrDefault();

            float splashDamage = Mathf.Floor(attacker.GetAttack() * 0.5f);
            var targetScores = targets.ToDictionary(target => target, target => CalculateOverwhelmScore(attacker, target, targets, splashDamage));

            return targetScores.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private float CalculateOverwhelmScore(EntityManager attacker, EntityManager target, List<EntityManager> allTargets, float splashDamage)
        {
            float score = 0;
            var otherTargets = allTargets.Where(t => t != target).ToList();

            // Base score for killing main target
            if (attacker.GetAttack() >= target.GetHealth())
            {
                score += 100f;
                score += target.GetAttack() * 5f; // Prefer high attack targets if we can kill them
            }

            // Splash damage potential
            int potentialKills = otherTargets.Count(t => t.GetHealth() <= splashDamage);
            int damagedTargets = otherTargets.Count(t => t.GetHealth() > splashDamage);

            score += potentialKills * 50f;
            score += damagedTargets * splashDamage * 2f;
            score += otherTargets.Count * 5f; // Prefer targets surrounded by many others

            return score;
        }

        private EntityManager SelectBestTarget(EntityManager attacker, List<EntityManager> targets,
                                        BoardState boardState, StrategicMode mode)
        {
            if (attacker == null || targets == null || targets.Count == 0)
                return null;

            var validTargets = targets.Where(t => t != null && !t.dead && !t.IsFadingOut).ToList();
            if (validTargets.Count == 0) return null;
            if (validTargets.Count == 1) return validTargets[0];

            // Check if this is our only monster
            bool isLastMonster = _avoidLosingLastMonster && IsLastMonster(attacker);

            // Chance to ignore last monster protection (simulate human error)
            if (Random.value < _lastMonsterIgnoreChance && isLastMonster)
            {
                Debug.Log("[AttackStrategyManager] Occasionally ignoring last monster protection (simulating human error)");
                isLastMonster = false;
            }

            // Calculate scores for all targets
            var targetScores = new Dictionary<EntityManager, float>();
            foreach (var target in validTargets)
            {
                try
                {
                    float score = _targetEvaluator.EvaluateTarget(attacker, target, boardState, mode);
                    score = AdjustScoreForLastMonster(score, attacker, target, boardState, isLastMonster);
                    score = AdjustScoreForTurnOrder(score, attacker, target, boardState, isLastMonster);

                    // Special case: board clearing consideration
                    if (IsLastMonster(attacker) && WouldClearBoard(attacker, target, validTargets))
                    {
                        score += 200f;
                        Debug.Log($"[AttackStrategyManager] Trading last monster with {target.name} would clear the board - strategically valuable");
                    }

                    targetScores[target] = score;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AttackStrategyManager] Error evaluating target: {e.Message}");
                }
            }

            var sortedTargets = targetScores.OrderByDescending(kvp => kvp.Value).ToList();
            if (sortedTargets.Count == 0) return null;

            // Sometimes make suboptimal choice to simulate human error
            if (ShouldMakeSuboptimalDecision() && sortedTargets.Count > 1)
            {
                int randomIndex = Random.Range(1, Mathf.Min(sortedTargets.Count, 3));
                Debug.Log($"[AttackStrategyManager] Making suboptimal choice (human error simulation)");
                return sortedTargets[randomIndex].Key;
            }

            return sortedTargets[0].Key;
        }

        private float AdjustScoreForLastMonster(float baseScore, EntityManager attacker, EntityManager target,
                                         BoardState boardState, bool isLastMonster)
        {
            if (!isLastMonster) return baseScore;

            // Check if attacker would die from counterattack
            bool wouldDieFromCounterattack =
                attacker.GetHealth() <= target.GetAttack() &&
                !attacker.HasKeyword(Keywords.MonsterKeyword.Ranged) &&
                target.GetAttack() > 0;

            if (wouldDieFromCounterattack)
            {
                bool isValuableTrade = IsValuableTrade(attacker, target, boardState);

                if (isValuableTrade)
                {
                    Debug.Log($"[AttackStrategyManager] Allowing valuable trade: {attacker.name} for {target.name}");
                    return baseScore + 50f;
                }
                else
                {
                    Debug.Log($"[AttackStrategyManager] Avoiding attacking {target.name} with our only monster - not worth the trade");
                    return baseScore - 1000f;
                }
            }
            else if (target.GetAttack() == 0)
            {
                // Target has 0 attack - perfectly safe to attack
                Debug.Log($"[AttackStrategyManager] Target {target.name} has 0 attack - safe to attack with our last monster");
                return baseScore + 100f;
            }

            return baseScore;
        }

        private float AdjustScoreForTurnOrder(float baseScore, EntityManager attacker, EntityManager target,
                                       BoardState boardState, bool isLastMonster)
        {
            if (boardState == null) return baseScore;

            float score = baseScore;

            // Player goes first next turn
            if (boardState.IsNextTurnPlayerFirst)
            {
                // High attack target that we can kill
                if (target.GetAttack() >= 4 && target.GetHealth() <= attacker.GetAttack())
                {
                    if (isLastMonster && attacker.GetHealth() <= target.GetAttack())
                    {
                        score += IsValuableTrade(attacker, target, boardState) ? 30f : -30f;
                    }
                    else
                    {
                        score += 40f;
                        Debug.Log($"[AttackStrategyManager] Prioritizing killing {target.name} before player's next turn");
                    }
                }
                else if (target.GetAttack() >= 4 && target.GetHealth() > attacker.GetAttack())
                {
                    score -= 20f;
                }
            }
            // Enemy goes first next turn
            else
            {
                if (target.GetHealth() <= attacker.GetAttack())
                {
                    if (isLastMonster && attacker.GetHealth() <= target.GetAttack())
                    {
                        score += IsValuableTrade(attacker, target, boardState) ? 20f : -10f;
                    }
                    else
                    {
                        score += 20f;
                    }
                }

                // Setup for next turn kill
                if (target.GetHealth() > attacker.GetAttack() && target.GetHealth() <= attacker.GetAttack() * 2)
                {
                    score += 25f;
                    Debug.Log($"[AttackStrategyManager] Damaging {target.name} to finish next turn when we go first");
                }
            }

            return score;
        }
        #endregion

        #region Trade Evaluation Methods
        private bool IsValuableTrade(EntityManager attacker, EntityManager target, BoardState boardState = null)
        {
            float attackerValue = CalculateEntityValue(attacker);
            float targetValue = CalculateEntityValue(target);
            float valueRatio = targetValue / attackerValue;

            // High threat targets are always worth trading for
            if (target.GetAttack() >= 6)
            {
                Debug.Log($"[AttackStrategyManager] High threat target ({target.name} with {target.GetAttack()} attack) - worth trading");
                return true;
            }

            // Start with base ratio and adjust based on board state
            float acceptableRatio = 1.3f;

            if (boardState != null)
            {
                acceptableRatio = AdjustTradeRatioBasedOnBoardState(acceptableRatio, boardState, attacker, target);
            }

            // Ensure ratio stays within reasonable bounds
            acceptableRatio = Mathf.Clamp(acceptableRatio, 1.0f, _valuableTradeRatio);

            // Log decision factors
            string decision = valueRatio >= acceptableRatio ? "ACCEPT" : "REJECT";
            Debug.Log($"[AttackStrategyManager] Trade evaluation: {attacker.name} ({attackerValue:F1}) for {target.name} ({targetValue:F1}), " +
                      $"Ratio: {valueRatio:F2}, Required: {acceptableRatio:F2} - {decision}");

            return valueRatio >= acceptableRatio;
        }

        private float AdjustTradeRatioBasedOnBoardState(float baseRatio, BoardState boardState,
                                                 EntityManager attacker, EntityManager target)
        {
            float adjustedRatio = baseRatio;
            float boardAdvantage = boardState.EnemyBoardControl / Mathf.Max(1f, boardState.PlayerBoardControl);

            // Board control adjustments
            if (boardAdvantage > 1.5f)
            {
                adjustedRatio += 0.3f; // Be more selective when ahead
                Debug.Log($"[AttackStrategyManager] Strong board advantage ({boardAdvantage:F2}x) - requiring better trades (+0.3)");
            }
            else if (boardAdvantage > 1.2f)
            {
                adjustedRatio += 0.15f;
                Debug.Log($"[AttackStrategyManager] Slight board advantage ({boardAdvantage:F2}x) - requiring better trades (+0.15)");
            }
            else if (boardAdvantage < 0.8f)
            {
                adjustedRatio -= 0.2f; // Accept worse trades when behind
                Debug.Log($"[AttackStrategyManager] Board disadvantage ({boardAdvantage:F2}x) - accepting worse trades (-0.2)");
            }

            // Turn count consideration
            if (boardState.TurnCount >= _aggressiveTurnThreshold)
            {
                adjustedRatio -= 0.15f; // More aggressive in late game
                Debug.Log($"[AttackStrategyManager] Late game (turn {boardState.TurnCount}) - accepting worse trades (-0.15)");
            }

            // Turn order considerations
            adjustedRatio += boardState.IsNextTurnPlayerFirst ? 0.1f : -0.1f;
            Debug.Log($"[AttackStrategyManager] {(boardState.IsNextTurnPlayerFirst ? "Player" : "Enemy")} goes first next turn - " +
                      $"{(boardState.IsNextTurnPlayerFirst ? "more cautious" : "more willing")} about trades");

            // Health considerations
            if (boardState.PlayerHealth <= 15)
            {
                adjustedRatio -= 0.2f; // More aggressive when player is low
                Debug.Log($"[AttackStrategyManager] Player at low health ({boardState.PlayerHealth}) - more willing to trade (-0.2)");
            }
            else if (boardState.EnemyHealth <= 15)
            {
                adjustedRatio += 0.2f; // More careful when we're low
                Debug.Log($"[AttackStrategyManager] Enemy at low health ({boardState.EnemyHealth}) - more careful about trades (+0.2)");
            }

            // Board clearing consideration
            if (IsLastMonster(attacker) && WouldClearBoard(attacker, target,
                boardState.PlayerMonsters.Where(t => t != null && !t.dead && !t.IsFadingOut).ToList()))
            {
                adjustedRatio -= 0.3f; // More willing to trade if it clears both sides
                Debug.Log("[AttackStrategyManager] Trade would clear the board - more willing to accept (-0.3)");
            }

            return adjustedRatio;
        }

        private float CalculateEntityValue(EntityManager entity)
        {
            if (entity == null) return 0;

            float value = entity.GetAttack() * 2 + entity.GetHealth();

            // Add value for keywords
            if (entity.HasKeyword(Keywords.MonsterKeyword.Taunt)) value += 3;
            if (entity.HasKeyword(Keywords.MonsterKeyword.Ranged)) value += 4;
            if (entity.HasKeyword(Keywords.MonsterKeyword.Overwhelm)) value += 3;
            if (entity.HasKeyword(Keywords.MonsterKeyword.Tough)) value += 2;

            return value;
        }
        #endregion

        #region Attack Order Methods
        private List<EntityManager> OptimizeForBoardControl(List<EntityManager> attackers, List<EntityManager> players, BoardState boardState)
        {
            bool playerGoesFirstNextTurn = boardState != null && boardState.IsNextTurnPlayerFirst;
            bool hasMultiplePlayerEntities = players != null && players.Count > 1;

            return attackers
                // Player turn order considerations
                .OrderByDescending(e => playerGoesFirstNextTurn && e.HasKeyword(Keywords.MonsterKeyword.Ranged) ? 3 : 0)
                // Setup for next turn kills
                .ThenByDescending(e => !playerGoesFirstNextTurn ?
                    players.Count(p => p.GetHealth() > e.GetAttack() && p.GetHealth() <= e.GetAttack() * 2) : 0)
                // Keyword priorities
                .ThenByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && hasMultiplePlayerEntities ? 2 : 0)
                .ThenByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Ranged) ? 1 : 0)
                // Kill priority
                .ThenByDescending(e => players.Any(p => e.GetAttack() >= p.GetHealth()) ? 1 : 0)
                // Tough attackers against high attack targets
                .ThenByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Tough) && players.Any(p => p.GetAttack() >= 4) ? 1 : 0)
                // Basic stats ordering
                .ThenByDescending(e => e.GetAttack())
                .ThenBy(e => e.GetHealth())
                .ToList();
        }

        private List<EntityManager> OptimizeForLethal(List<EntityManager> attackers, List<EntityManager> playerEntities)
        {
            Debug.Log("[AttackStrategyManager] Optimizing attack order for lethal");

            bool shouldIgnoreLastMonsterProtection = Random.value < _lastMonsterIgnoreChance;

            if (_avoidLosingLastMonster && CountActiveEnemyMonsters() <= 1 && !shouldIgnoreLastMonsterProtection)
            {
                bool wouldLoseLastMonster = attackers.Any(a =>
                    !a.HasKeyword(Keywords.MonsterKeyword.Ranged) &&
                    playerEntities.Any(p => p.GetAttack() >= a.GetHealth() && p.HasKeyword(Keywords.MonsterKeyword.Taunt)));

                if (wouldLoseLastMonster)
                {
                    // If high-attack taunt present, worth sacrificing
                    if (playerEntities.Any(p => p.HasKeyword(Keywords.MonsterKeyword.Taunt) && p.GetAttack() >= 4))
                    {
                        Debug.Log("[AttackStrategyManager] Sacrificing last monster due to high-threat taunt target");
                        return attackers.OrderByDescending(e => e.GetAttack()).ToList();
                    }

                    Debug.Log("[AttackStrategyManager] Lethal available but would lose our only monster - being cautious");
                    return attackers.OrderByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Ranged) ? 1 : 0)
                                  .ThenByDescending(e => e.GetAttack())
                                  .ThenBy(e => e.GetHealth())
                                  .ToList();
                }
            }
            else if (shouldIgnoreLastMonsterProtection && CountActiveEnemyMonsters() <= 1)
            {
                Debug.Log("[AttackStrategyManager] Human error: Ignoring last monster protection for lethal opportunity");
            }

            // Optimize for taunt clearing if needed
            if (playerEntities?.Any(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt)) == true)
            {
                return attackers
                    .OrderBy(e => e.GetHealth())
                    .ThenByDescending(e => e.GetAttack())
                    .ToList();
            }

            // Standard lethal optimization
            return attackers.OrderByDescending(e => e.GetAttack()).ToList();
        }

        private List<EntityManager> GetPartiallyShuffledAttackers(List<EntityManager> original)
        {
            var shuffled = new List<EntityManager>(original);
            for (int i = 0; i < shuffled.Count - 1; i++)
            {
                if (Random.value < 0.4f)
                {
                    var temp = shuffled[i];
                    shuffled[i] = shuffled[i + 1];
                    shuffled[i + 1] = temp;
                }
            }
            Debug.Log("[AttackStrategyManager] Applied partial randomization to attack order");
            return shuffled;
        }
        #endregion

        #region Utility Methods
        private bool WouldClearBoard(EntityManager attacker, EntityManager target, List<EntityManager> allTargets)
        {
            if (!IsLastMonster(attacker)) return false;

            bool attackerWouldDie = !attacker.HasKeyword(Keywords.MonsterKeyword.Ranged) &&
                                   target.GetAttack() > 0 &&
                                   target.GetAttack() >= attacker.GetHealth();

            bool isLastTarget = allTargets.Count == 1;
            bool targetWouldDie = attacker.GetAttack() >= target.GetHealth();

            return attackerWouldDie && isLastTarget && targetWouldDie;
        }

        private bool IsLastMonster(EntityManager monster) => CountActiveEnemyMonsters() <= 1;

        private int CountActiveEnemyMonsters()
        {
            if (_entityCacheManager == null) return 0;

            _entityCacheManager.RefreshEntityCaches();
            var enemies = _entityCacheManager.CachedEnemyEntities;

            if (enemies == null) return 0;

            return enemies.Count(e => e != null && !e.dead && e.placed && !e.IsFadingOut);
        }

        private bool ShouldMakeSuboptimalDecision() => Random.value < _decisionVariance;

        private bool CanKillPlayerThisTurn(List<EntityManager> attackers, List<EntityManager> playerEntities,
                                    HealthIconManager playerHealthIcon)
        {
            if (attackers == null || playerHealthIcon == null) return false;

            // Check for taunt units
            bool hasTaunt = playerEntities?.Any(e => e != null && !e.dead && e.placed &&
                                            !e.IsFadingOut && e.HasKeyword(Keywords.MonsterKeyword.Taunt)) == true;

            if (hasTaunt)
            {
                float totalDamage = attackers.Sum(a => a?.GetAttack() ?? 0);
                float remainingDamage = totalDamage;

                // Calculate damage needed to clear taunts
                var tauntUnits = playerEntities.Where(e => e != null && !e.dead && e.placed &&
                                                    !e.IsFadingOut && e.HasKeyword(Keywords.MonsterKeyword.Taunt))
                                            .OrderBy(e => e.GetHealth());

                foreach (var tauntUnit in tauntUnits)
                {
                    remainingDamage -= tauntUnit.GetHealth();
                }

                return remainingDamage >= (playerHealthIcon.GetHealth() - 2);
            }

            // Direct damage calculation
            float attackDamage = attackers.Sum(a => a?.GetAttack() ?? 0);
            return attackDamage >= (playerHealthIcon.GetHealth() - 1);
        }

        private void LogDefensiveUnits(List<EntityManager> defensiveUnits)
        {
            if (!defensiveUnits.Any()) return;

            Debug.Log($"[AttackStrategyManager] Excluding {defensiveUnits.Count} defensive units with 0 attack from attack order");
            foreach (var unit in defensiveUnits)
            {
                Debug.Log($"[AttackStrategyManager] - {unit.name} is a defensive unit (0 attack)");
            }
        }
        #endregion
    }
}
