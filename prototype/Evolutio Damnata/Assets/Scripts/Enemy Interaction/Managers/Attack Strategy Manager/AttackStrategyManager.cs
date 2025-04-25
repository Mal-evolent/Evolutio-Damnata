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

        private ITargetEvaluator _targetEvaluator;
        private IEntityCacheManager _entityCacheManager;

        public void Initialize(ITargetEvaluator targetEvaluator, IEntityCacheManager entityCacheManager)
        {
            _targetEvaluator = targetEvaluator;
            _entityCacheManager = entityCacheManager;
        }

        public List<EntityManager> GetAttackOrder(List<EntityManager> enemies, List<EntityManager> players,
                                             HealthIconManager healthIcon, BoardState boardState)
        {
            // Filter out entities with 0 attack - they are defensive units and shouldn't attack
            var validAttackers = enemies.Where(e => e != null && e.GetAttack() > 0).ToList();

            // Log information about defensive units being excluded
            var defensiveUnits = enemies.Where(e => e != null && e.GetAttack() == 0).ToList();
            if (defensiveUnits.Any())
            {
                Debug.Log($"[AttackStrategyManager] Excluding {defensiveUnits.Count} defensive units with 0 attack from attack order");
                foreach (var unit in defensiveUnits)
                {
                    Debug.Log($"[AttackStrategyManager] - {unit.name} is a defensive unit (0 attack)");
                }
            }

            // If no valid attackers after filtering, return empty list
            if (validAttackers.Count == 0)
            {
                Debug.Log("[AttackStrategyManager] No valid attackers with attack > 0 available");
                return new List<EntityManager>();
            }

            var order = validAttackers.ToList();

            if (Random.value < _attackOrderRandomizationChance && order.Count > 1)
            {
                order = GetPartiallyShuffledAttackers(order);
            }

            if (CanKillPlayerThisTurn(order, players, healthIcon))
            {
                order = OptimizeForLethal(order, players);
            }
            else
            {
                // Consider turn order when determining attack order
                bool playerGoesFirstNextTurn = boardState != null && boardState.IsNextTurnPlayerFirst;
                bool hasMultiplePlayerEntities = players != null && players.Count > 1;

                order = order
                    // If player goes next, prioritize ranged attackers to preserve board
                    .OrderByDescending(e => playerGoesFirstNextTurn && e.HasKeyword(Keywords.MonsterKeyword.Ranged) ? 3 : 0)
                    // If enemy goes next, prioritize attackers that almost kill targets
                    .ThenByDescending(e => !playerGoesFirstNextTurn ?
                        players.Count(p => p.GetHealth() > e.GetAttack() &&
                                          p.GetHealth() <= e.GetAttack() * 2) : 0)
                    // Overwhelm is most valuable with multiple targets
                    .ThenByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && hasMultiplePlayerEntities ? 2 : 0)
                    // Ranged attackers are always valuable
                    .ThenByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Ranged) ? 1 : 0)
                    // Prioritize attackers that can kill targets
                    .ThenByDescending(e => players.Any(p => e.GetAttack() >= p.GetHealth()) ? 1 : 0)
                    // Tough attackers are better for high-attack targets
                    .ThenByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Tough) && players.Any(p => p.GetAttack() >= 4) ? 1 : 0)
                    .ThenByDescending(e => e.GetAttack())
                    .ThenBy(e => e.GetHealth())
                    .ToList();
            }

            return order;
        }


        public EntityManager SelectTarget(EntityManager attacker, List<EntityManager> playerEntities,
                                     HealthIconManager playerHealthIcon, BoardState boardState, StrategicMode mode)
        {
            if (playerEntities == null || playerEntities.Count == 0)
                return null;

            bool hasTaunts = playerEntities.Any(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt));

            if (hasTaunts)
            {
                var tauntTargets = playerEntities.Where(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt)).ToList();

                // For Overwhelm attackers against taunt, use specialized targeting
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && tauntTargets.Count > 0)
                    return SelectTargetForOverwhelmAttacker(attacker, tauntTargets, boardState);

                return SelectBestTarget(attacker, tauntTargets, boardState, mode);
            }

            // For Overwhelm attackers with multiple targets
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && playerEntities.Count > 1)
                return SelectTargetForOverwhelmAttacker(attacker, playerEntities, boardState);

            return SelectBestTarget(attacker, playerEntities, boardState, mode);
        }

        public StrategicMode DetermineStrategicMode(BoardState boardState)
        {
            if (boardState == null)
                return StrategicMode.Defensive;

            bool healthAdvantage = boardState.EnemyHealth > boardState.PlayerHealth + _healthThresholdForAggro;
            bool boardAdvantage = boardState.EnemyBoardControl > boardState.PlayerBoardControl * 1.3f;
            bool lateGame = boardState.TurnCount > _aggressiveTurnThreshold;
            bool playerLowHealth = boardState.PlayerHealth <= 15;
            bool enemyNextTurn = !boardState.IsNextTurnPlayerFirst;

            // If we have only one monster, be more defensive generally, but not always
            bool hasOnlyOneMonster = CountActiveEnemyMonsters() <= 1;
            if (hasOnlyOneMonster && Random.value > 0.25f) // 75% chance to be defensive with one monster
            {
                Debug.Log("[AttackStrategyManager] Only one monster on field - adopting defensive strategy");
                return StrategicMode.Defensive;
            }

            // If we go first next turn, we can be more aggressive
            if (enemyNextTurn)
            {
                Debug.Log("[AttackStrategyManager] Enemy goes first next turn - adopting more aggressive strategy");
                return StrategicMode.Aggro;
            }

            // If player goes first next turn, be more cautious
            if (boardState.IsNextTurnPlayerFirst && !healthAdvantage && !boardAdvantage && !playerLowHealth)
            {
                Debug.Log("[AttackStrategyManager] Player goes first next turn - adopting more defensive strategy");
                return StrategicMode.Defensive;
            }

            if (healthAdvantage || boardAdvantage || lateGame || playerLowHealth)
                return StrategicMode.Aggro;

            if (boardState.EnemyHealth < 15 || boardState.PlayerBoardControl > boardState.EnemyBoardControl)
                return StrategicMode.Defensive;

            // Default to aggressive more often
            return Random.value < 0.6f ? StrategicMode.Aggro : StrategicMode.Defensive;
        }

        public bool ShouldAttackHealthIcon(EntityManager attacker, List<EntityManager> playerEntities,
                                     HealthIconManager playerHealthIcon, BoardState boardState)
        {
            if (attacker == null || playerHealthIcon == null)
                return false;

            // Occasionally make a mistake by ignoring last monster protection
            if (Random.value < _lastMonsterIgnoreChance)
            {
                Debug.Log("[AttackStrategyManager] Making human-like mistake - ignoring last monster protection");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            // If this is our only monster, only be cautious about direct health attacks when it might result in loss
            if (_avoidLosingLastMonster && IsLastMonster(attacker) &&
                playerHealthIcon.GetHealth() > attacker.GetAttack() * 1.5f)
            {
                // Check if attacking the health icon would result in counterattack
                // Since health icons typically don't counter-attack, this is likely always safe
                bool wouldTakeCounterDamage = false; // Health icons don't counter

                if (!wouldTakeCounterDamage)
                {
                    // Safe to attack - no counter damage from health icon
                    Debug.Log("[AttackStrategyManager] Safe direct health attack with our only monster - no counterattack risk");
                    return AIUtilities.CanTargetHealthIcon(playerEntities);
                }

                Debug.Log("[AttackStrategyManager] Avoiding direct health attack with our only monster");
                return false;
            }

            // Consider turn order for direct health attacks
            bool playerGoesFirstNextTurn = boardState != null && boardState.IsNextTurnPlayerFirst;

            // If player goes first next and their health is low, prioritize direct attacks
            if (playerGoesFirstNextTurn && playerHealthIcon.GetHealth() <= 10)
            {
                Debug.Log("[AttackStrategyManager] Prioritizing direct health attack for potential lethal before player's turn");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }
            // If enemy goes first next, be more strategic
            else if (!playerGoesFirstNextTurn)
            {
                bool nearLethal = playerHealthIcon.GetHealth() <= attacker.GetAttack() * 1.5f;
                if (nearLethal)
                {
                    Debug.Log("[AttackStrategyManager] Strategic direct health attack to set up lethal on our next turn");
                    return AIUtilities.CanTargetHealthIcon(playerEntities);
                }
            }

            return AIUtilities.CanTargetHealthIcon(playerEntities);
        }

        private EntityManager SelectTargetForOverwhelmAttacker(EntityManager attacker, List<EntityManager> targets, BoardState boardState)
        {
            if (targets == null || targets.Count <= 1)
                return targets?.FirstOrDefault();

            // Calculate splash damage (50% of attacker's damage)
            float splashDamage = Mathf.Floor(attacker.GetAttack() * 0.5f);

            var scoredTargets = new Dictionary<EntityManager, float>();

            foreach (var target in targets)
            {
                float score = 0;

                // Base score for being able to kill the main target
                if (attacker.GetAttack() >= target.GetHealth())
                    score += 100f;

                // Calculate splash potential against other targets
                var otherTargets = targets.Where(t => t != target).ToList();
                int potentialKills = otherTargets.Count(t => t.GetHealth() <= splashDamage);
                int damagedTargets = otherTargets.Count(t => t.GetHealth() > splashDamage);

                // Add score for potential splash kills and damage
                score += potentialKills * 50f;
                score += damagedTargets * splashDamage * 2f;

                // Prefer targets with high attack if we can kill them
                if (attacker.GetAttack() >= target.GetHealth())
                    score += target.GetAttack() * 5f;

                // Prefer targets surrounded by many other targets to maximize splash
                score += otherTargets.Count * 5f;

                scoredTargets[target] = score;
            }

            // Return target with highest score
            return scoredTargets.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private EntityManager SelectBestTarget(EntityManager attacker, List<EntityManager> targets,
                                        BoardState boardState, StrategicMode mode)
        {
            if (attacker == null || targets == null || targets.Count == 0)
                return null;

            var validTargets = targets.Where(t => t != null && !t.dead && !t.IsFadingOut).ToList();

            if (validTargets.Count == 0)
                return null;

            if (validTargets.Count == 1)
                return validTargets[0];

            // Check if this is our only monster and we would die from counterattack
            bool isLastMonster = _avoidLosingLastMonster && IsLastMonster(attacker);

            // Chance to ignore last monster protection (simulate human error)
            bool ignoreLastMonsterProtection = Random.value < _lastMonsterIgnoreChance;
            if (ignoreLastMonsterProtection && isLastMonster)
            {
                Debug.Log("[AttackStrategyManager] Occasionally ignoring last monster protection (simulating human error)");
                isLastMonster = false;
            }

            var targetScores = new Dictionary<EntityManager, float>();
            foreach (var target in validTargets)
            {
                try
                {
                    float score = _targetEvaluator.EvaluateTarget(attacker, target, boardState, mode);

                    // If this is our only monster, consider preventing its death, but allow valuable trades
                    if (isLastMonster)
                    {
                        // Check if the monster would die from counterattack
                        // Modified to check if target's attack is > 0
                        bool wouldDieFromCounterattack =
                            attacker.GetHealth() <= target.GetAttack() &&
                            !attacker.HasKeyword(Keywords.MonsterKeyword.Ranged) &&
                            target.GetAttack() > 0; // Added check for target's attack > 0

                        if (wouldDieFromCounterattack)
                        {
                            // Check if this would be a valuable trade using the new gradual system
                            bool isValuableTrade = IsValuableTrade(attacker, target, boardState);

                            if (isValuableTrade)
                            {
                                // This is a good trade even for our last monster
                                score += 50f;
                                Debug.Log($"[AttackStrategyManager] Allowing valuable trade: {attacker.name} for {target.name}");
                            }
                            else
                            {
                                // Not valuable enough to sacrifice our last monster
                                score -= 1000f;
                                Debug.Log($"[AttackStrategyManager] Avoiding attacking {target.name} with our only monster - not worth the trade");
                            }
                        }
                        else if (target.GetAttack() == 0)
                        {
                            // Target has 0 attack - perfectly safe to attack with our last monster
                            score += 100f;
                            Debug.Log($"[AttackStrategyManager] Target {target.name} has 0 attack - safe to attack with our last monster");
                        }
                    }

                    // Apply turn order considerations to target selection
                    if (boardState != null)
                    {
                        // If player goes first next turn
                        if (boardState.IsNextTurnPlayerFirst)
                        {
                            // Prioritize killing high-attack targets that could harm us next turn
                            if (target.GetAttack() >= 4 && target.GetHealth() <= attacker.GetAttack())
                            {
                                // If this is our only monster, be more cautious about trades unless it's valuable
                                if (isLastMonster && attacker.GetHealth() <= target.GetAttack())
                                {
                                    if (IsValuableTrade(attacker, target, boardState))
                                    {
                                        // It's worth trading our last monster for this target
                                        score += 30f;
                                        Debug.Log($"[AttackStrategyManager] Worth trading our last monster to remove {target.name}");
                                    }
                                    else
                                    {
                                        score -= 30f; // Reduce priority of trading our only monster
                                    }
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
                        // If enemy goes first next turn
                        else
                        {
                            // Slightly reduce priority on killing targets as we'll attack again soon
                            if (target.GetHealth() <= attacker.GetAttack())
                            {
                                // If this is our only monster, still be cautious about trades unless valuable
                                if (isLastMonster && attacker.GetHealth() <= target.GetAttack())
                                {
                                    if (IsValuableTrade(attacker, target, boardState))
                                    {
                                        score += 20f; // Valuable trade is still good even with our last monster
                                    }
                                    else
                                    {
                                        score -= 10f; // Less penalty because we'll go first next
                                    }
                                }
                                else
                                {
                                    score += 20f;
                                }
                            }

                            // Prioritize damaging high-health targets for next turn
                            if (target.GetHealth() > attacker.GetAttack() &&
                                target.GetHealth() <= attacker.GetAttack() * 2)
                            {
                                score += 25f;
                                Debug.Log($"[AttackStrategyManager] Damaging {target.name} to finish next turn when we go first");
                            }
                        }
                    }

                    // Special case: if attacking this target would clear the board, consider it highly
                    if (IsLastMonster(attacker) && WouldClearBoard(attacker, target, validTargets))
                    {
                        // If no monsters would remain after this trade, it could be advantageous
                        score += 200f;
                        Debug.Log($"[AttackStrategyManager] Trading last monster with {target.name} would clear the board - this is strategically valuable");
                    }

                    targetScores[target] = score;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AttackStrategyManager] Error evaluating target: {e.Message}");
                }
            }

            var sortedTargets = targetScores.OrderByDescending(kvp => kvp.Value).ToList();

            if (sortedTargets.Count == 0)
                return null;

            // Add human-like error by sometimes selecting a suboptimal target
            if (ShouldMakeSuboptimalDecision() && sortedTargets.Count > 1)
            {
                int randomIndex = Random.Range(1, Mathf.Min(sortedTargets.Count, 3));
                Debug.Log($"[AttackStrategyManager] Making suboptimal choice (human error simulation)");
                return sortedTargets[randomIndex].Key;
            }

            return sortedTargets[0].Key;
        }

        // Helper method to determine if a trade is valuable enough to sacrifice our last monster
        private bool IsValuableTrade(EntityManager attacker, EntityManager target, BoardState boardState = null)
        {
            // Calculate trade value based on stats and keywords
            float attackerValue = CalculateEntityValue(attacker);
            float targetValue = CalculateEntityValue(target);

            // Calculate value ratio (target value / attacker value)
            float valueRatio = targetValue / attackerValue;

            // Special condition: High threat targets (with attack ≥ 6) are worth trading for
            bool isHighThreatTarget = target.GetAttack() >= 6;
            if (isHighThreatTarget)
            {
                Debug.Log($"[AttackStrategyManager] High threat target ({target.name} with {target.GetAttack()} attack) - worth trading for regardless of value ratio");
                return true;
            }

            // Start with base acceptance threshold - less strict than the flat 2.0x
            float acceptableRatio = 1.3f; // Base minimum ratio (instead of flat 2.0)

            // Adjust based on board state if available
            if (boardState != null)
            {
                // Calculate board advantage
                float boardAdvantage = boardState.EnemyBoardControl / Mathf.Max(1f, boardState.PlayerBoardControl);

                // 1. When we have strong board advantage, be more selective with trades
                if (boardAdvantage > 1.5f)
                {
                    acceptableRatio += 0.3f; // Require better trades when ahead
                    Debug.Log($"[AttackStrategyManager] Strong board advantage ({boardAdvantage:F2}x) - requiring better trades (+0.3)");
                }
                // When slightly ahead, be a bit more selective
                else if (boardAdvantage > 1.2f)
                {
                    acceptableRatio += 0.15f;
                    Debug.Log($"[AttackStrategyManager] Slight board advantage ({boardAdvantage:F2}x) - requiring somewhat better trades (+0.15)");
                }

                // 2. When behind, be more willing to trade
                else if (boardAdvantage < 0.8f)
                {
                    acceptableRatio -= 0.2f; // Accept worse trades when behind
                    Debug.Log($"[AttackStrategyManager] Board disadvantage ({boardAdvantage:F2}x) - accepting less favorable trades (-0.2)");
                }

                // 3. Turn count consideration - more aggressive in late game
                if (boardState.TurnCount >= _aggressiveTurnThreshold)
                {
                    acceptableRatio -= 0.15f; // More willing to trade in late game
                    Debug.Log($"[AttackStrategyManager] Late game (turn {boardState.TurnCount}) - accepting slightly worse trades (-0.15)");
                }

                // 4. Turn order considerations
                if (!boardState.IsNextTurnPlayerFirst)
                {
                    // We go first next, can be more open to trades
                    acceptableRatio -= 0.1f;
                    Debug.Log("[AttackStrategyManager] Enemy goes first next turn - more willing to trade (-0.1)");
                }
                else
                {
                    // Player goes first next, be more cautious
                    acceptableRatio += 0.1f;
                    Debug.Log("[AttackStrategyManager] Player goes first next turn - more cautious about trades (+0.1)");
                }

                // 5. Health considerations
                if (boardState.PlayerHealth <= 15)
                {
                    acceptableRatio -= 0.2f; // More aggressive when player is low
                    Debug.Log($"[AttackStrategyManager] Player at low health ({boardState.PlayerHealth}) - more willing to trade (-0.2)");
                }
                else if (boardState.EnemyHealth <= 15)
                {
                    acceptableRatio += 0.2f; // More careful when we're low
                    Debug.Log($"[AttackStrategyManager] Enemy at low health ({boardState.EnemyHealth}) - more careful about trades (+0.2)");
                }

                // 6. Consider if trading would create a cleared board situation
                if (IsLastMonster(attacker) && WouldClearBoard(attacker, target,
                    boardState.PlayerMonsters.Where(t => t != null && !t.dead && !t.IsFadingOut).ToList()))
                {
                    acceptableRatio -= 0.3f; // More willing to trade if it clears both sides
                    Debug.Log("[AttackStrategyManager] Trade would clear the board - more willing to accept (-0.3)");
                }
            }

            // Ensure the ratio stays within reasonable bounds (1.0 to max value)
            acceptableRatio = Mathf.Clamp(acceptableRatio, 1.0f, _valuableTradeRatio);

            // Log decision factors
            string decision = valueRatio >= acceptableRatio ? "ACCEPT" : "REJECT";
            Debug.Log($"[AttackStrategyManager] Trade evaluation: {attacker.name} ({attackerValue:F1}) for {target.name} ({targetValue:F1}), " +
                      $"Ratio: {valueRatio:F2}, Required: {acceptableRatio:F2} - {decision}");

            // Return final trade decision
            return valueRatio >= acceptableRatio;
        }

        // Calculate a value score for an entity based on its stats and keywords
        private float CalculateEntityValue(EntityManager entity)
        {
            if (entity == null) return 0;

            float value = entity.GetAttack() * 2 + entity.GetHealth();

            // Add value for important keywords
            if (entity.HasKeyword(Keywords.MonsterKeyword.Taunt))
                value += 3;

            if (entity.HasKeyword(Keywords.MonsterKeyword.Ranged))
                value += 4;

            if (entity.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
                value += 3;

            if (entity.HasKeyword(Keywords.MonsterKeyword.Tough))
                value += 2;

            return value;
        }

        // Check if this attack would result in an empty board (both sides)
        private bool WouldClearBoard(EntityManager attacker, EntityManager target, List<EntityManager> allTargets)
        {
            // If this isn't the last monster, then the board won't be cleared
            if (!IsLastMonster(attacker))
                return false;

            // Check if this would kill our attacker
            // Modified to check if target's attack is > 0
            bool attackerWouldDie = !attacker.HasKeyword(Keywords.MonsterKeyword.Ranged) &&
                                   target.GetAttack() > 0 &&
                                   target.GetAttack() >= attacker.GetHealth();

            // Check if this target is the last one and would die too
            bool isLastTarget = allTargets.Count == 1;
            bool targetWouldDie = attacker.GetAttack() >= target.GetHealth();

            // Both sides would be cleared if:
            // 1. Our attacker would die from counterattack AND
            // 2. The target is the last one AND it would die
            return attackerWouldDie && isLastTarget && targetWouldDie;
        }

        // New method to check if this is the last monster on the field
        private bool IsLastMonster(EntityManager monster)
        {
            int activeMonsterCount = CountActiveEnemyMonsters();
            return activeMonsterCount <= 1;
        }

        // New method to count active enemy monsters
        private int CountActiveEnemyMonsters()
        {
            if (_entityCacheManager == null)
                return 0;

            _entityCacheManager.RefreshEntityCaches();
            var enemies = _entityCacheManager.CachedEnemyEntities;

            if (enemies == null)
                return 0;

            return enemies.Count(e => e != null && !e.dead && e.placed && !e.IsFadingOut);
        }

        private bool ShouldMakeSuboptimalDecision()
        {
            return Random.value < _decisionVariance;
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

        private bool CanKillPlayerThisTurn(List<EntityManager> attackers, List<EntityManager> playerEntities,
                                    HealthIconManager playerHealthIcon)
        {
            if (attackers == null || playerHealthIcon == null)
                return false;

            // If there are taunt monsters, we must attack them first
            bool hasTaunt = playerEntities != null &&
                            playerEntities.Any(e => e != null &&
                                              !e.dead &&
                                              e.placed &&
                                              !e.IsFadingOut &&
                                              e.HasKeyword(Keywords.MonsterKeyword.Taunt));

            if (hasTaunt)
            {
                // Calculate remaining damage after dealing with taunts
                float totalDamage = attackers.Sum(a => a?.GetAttack() ?? 0);
                float remainingDamage = totalDamage;

                // Get all taunt units
                var tauntUnits = playerEntities.Where(e => e != null &&
                                                      !e.dead &&
                                                      e.placed &&
                                                      !e.IsFadingOut &&
                                                      e.HasKeyword(Keywords.MonsterKeyword.Taunt))
                                              .OrderBy(e => e.GetHealth())
                                              .ToList();

                // Calculate damage needed to clear taunts
                foreach (var tauntUnit in tauntUnits)
                {
                    remainingDamage -= tauntUnit.GetHealth();
                }

                // More accurate lethal calculation
                float tauntThreshold = playerHealthIcon.GetHealth() - 2;
                return remainingDamage >= tauntThreshold;
            }

            // If no taunt units
            float attackDamage = attackers.Sum(a => a?.GetAttack() ?? 0);
            float directThreshold = playerHealthIcon.GetHealth() - 1;
            return attackDamage >= directThreshold;
        }

        private List<EntityManager> OptimizeForLethal(List<EntityManager> attackers, List<EntityManager> playerEntities)
        {
            Debug.Log("[AttackStrategyManager] Optimizing attack order for lethal");

            // If this gives us lethal but we would lose our only monster, reconsider (but with chance for error)
            bool shouldIgnoreLastMonsterProtection = Random.value < _lastMonsterIgnoreChance;

            if (_avoidLosingLastMonster && CountActiveEnemyMonsters() <= 1 && !shouldIgnoreLastMonsterProtection)
            {
                bool wouldLoseLastMonster = attackers.Any(a =>
                    !a.HasKeyword(Keywords.MonsterKeyword.Ranged) &&
                    playerEntities.Any(p => p.GetAttack() >= a.GetHealth() &&
                                          p.HasKeyword(Keywords.MonsterKeyword.Taunt)));

                if (wouldLoseLastMonster)
                {
                    // We'll be cautious but check if player health is very low (lethal next turn)
                    if (playerEntities.Any(p => p.HasKeyword(Keywords.MonsterKeyword.Taunt) && p.GetAttack() >= 4))
                    {
                        // If there's a high-attack taunt, it might be worth sacrificing our monster
                        Debug.Log("[AttackStrategyManager] Sacrificing last monster due to high-threat taunt target");
                        return attackers.OrderByDescending(e => e.GetAttack()).ToList();
                    }
                    else
                    {
                        Debug.Log("[AttackStrategyManager] Lethal available but would lose our only monster - being cautious");
                        return attackers.OrderByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Ranged) ? 1 : 0)
                                      .ThenByDescending(e => e.GetAttack())
                                      .ThenBy(e => e.GetHealth())
                                      .ToList();
                    }
                }
            }
            else if (shouldIgnoreLastMonsterProtection && CountActiveEnemyMonsters() <= 1)
            {
                Debug.Log("[AttackStrategyManager] Human error: Ignoring last monster protection for lethal opportunity");
            }

            // Otherwise, optimize for lethal normally
            if (playerEntities != null && playerEntities.Any(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt)))
            {
                return attackers
                    .OrderBy(e => e.GetHealth())
                    .ThenByDescending(e => e.GetAttack())
                    .ToList();
            }

            return attackers
                .OrderByDescending(e => e.GetAttack())
                .ToList();
        }
    }
}
