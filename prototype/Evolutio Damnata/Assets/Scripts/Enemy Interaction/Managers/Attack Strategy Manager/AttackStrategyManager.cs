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
            var order = enemies.ToList();

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

            var targetScores = new Dictionary<EntityManager, float>();
            foreach (var target in validTargets)
            {
                try
                {
                    float score = _targetEvaluator.EvaluateTarget(attacker, target, boardState, mode);

                    // NEW: Apply turn order considerations to target selection
                    if (boardState != null)
                    {
                        // If player goes first next turn
                        if (boardState.IsNextTurnPlayerFirst)
                        {
                            // Prioritize killing high-attack targets that could harm us next turn
                            if (target.GetAttack() >= 4 && target.GetHealth() <= attacker.GetAttack())
                            {
                                score += 40f;
                                Debug.Log($"[AttackStrategyManager] Prioritizing killing {target.name} before player's next turn");
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
                                score += 20f;
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

            if (ShouldMakeSuboptimalDecision() && sortedTargets.Count > 1)
            {
                int randomIndex = Random.Range(1, Mathf.Min(sortedTargets.Count, 3));
                return sortedTargets[randomIndex].Key;
            }

            return sortedTargets[0].Key;
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
