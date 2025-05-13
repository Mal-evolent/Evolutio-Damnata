using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;

namespace EnemyInteraction.Managers.AttackStrategy.Strategies
{
    public class AttackOrderStrategy
    {
        private readonly float _decisionVariance;
        private readonly float _attackOrderRandomizationChance;

        public AttackOrderStrategy(float decisionVariance, float attackOrderRandomizationChance)
        {
            _decisionVariance = decisionVariance;
            _attackOrderRandomizationChance = attackOrderRandomizationChance;
        }

        public List<EntityManager> DetermineAttackOrder(
            List<EntityManager> attackers, 
            List<EntityManager> players, 
            BoardState boardState, 
            bool isLethalPossible,
            System.Func<EntityManager, bool> isLastMonsterFunc)
        {
            var order = attackers.ToList();

            // Potentially randomize order
            if (Random.value < _attackOrderRandomizationChance && order.Count > 1)
                order = GetPartiallyShuffledAttackers(order);

            // Determine optimal attack order based on situation
            return isLethalPossible
                ? OptimizeForLethal(order, players, isLastMonsterFunc)
                : OptimizeForBoardControl(order, players, boardState);
        }

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

        private List<EntityManager> OptimizeForLethal(List<EntityManager> attackers, List<EntityManager> playerEntities, 
                                               System.Func<EntityManager, bool> isLastMonsterFunc)
        {
            Debug.Log("[AttackOrderStrategy] Optimizing attack order for lethal");

            bool shouldIgnoreLastMonsterProtection = Random.value < _decisionVariance;

            if (attackers.Any(a => isLastMonsterFunc(a)) && !shouldIgnoreLastMonsterProtection)
            {
                bool wouldLoseLastMonster = attackers.Any(a =>
                    !a.HasKeyword(Keywords.MonsterKeyword.Ranged) &&
                    playerEntities.Any(p => p.GetAttack() >= a.GetHealth() && p.HasKeyword(Keywords.MonsterKeyword.Taunt)));

                if (wouldLoseLastMonster)
                {
                    // If high-attack taunt present, worth sacrificing
                    if (playerEntities.Any(p => p.HasKeyword(Keywords.MonsterKeyword.Taunt) && p.GetAttack() >= 4))
                    {
                        Debug.Log("[AttackOrderStrategy] Sacrificing last monster due to high-threat taunt target");
                        return attackers.OrderByDescending(e => e.GetAttack()).ToList();
                    }

                    Debug.Log("[AttackOrderStrategy] Lethal available but would lose our only monster - being cautious");
                    return attackers.OrderByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Ranged) ? 1 : 0)
                                  .ThenByDescending(e => e.GetAttack())
                                  .ThenBy(e => e.GetHealth())
                                  .ToList();
                }
            }
            else if (shouldIgnoreLastMonsterProtection && attackers.Any(a => isLastMonsterFunc(a)))
            {
                Debug.Log("[AttackOrderStrategy] Human error: Ignoring last monster protection for lethal opportunity");
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
            Debug.Log("[AttackOrderStrategy] Applied partial randomization to attack order");
            return shuffled;
        }
    }
}
