using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;
using EnemyInteraction.Managers.AttackStrategy.Evaluators;

namespace EnemyInteraction.Managers.AttackStrategy.Strategies
{
    public class TargetSelectionStrategy
    {
        private readonly ITargetEvaluator _targetEvaluator;
        private readonly float _decisionVariance;
        private readonly OverwhelmEvaluator _overwhelmEvaluator;
        private readonly TradeEvaluator _tradeEvaluator;

        public TargetSelectionStrategy(ITargetEvaluator targetEvaluator, float decisionVariance)
        {
            _targetEvaluator = targetEvaluator;
            _decisionVariance = decisionVariance;
            _overwhelmEvaluator = new OverwhelmEvaluator();
            _tradeEvaluator = new TradeEvaluator(2.0f);
        }

        public EntityManager SelectTarget(
            EntityManager attacker, 
            List<EntityManager> targets, 
            BoardState boardState, 
            StrategicMode mode,
            bool isLastMonster,
            float lastMonsterIgnoreChance)
        {
            if (targets == null || targets.Count == 0)
                return null;

            // Handle taunt units
            bool hasTaunts = targets.Any(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt));
            if (hasTaunts)
            {
                var tauntTargets = targets.Where(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt)).ToList();

                // Special handling for Overwhelm against taunt
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && tauntTargets.Count > 0)
                    return _overwhelmEvaluator.SelectTargetForOverwhelmAttacker(attacker, tauntTargets, boardState);

                return SelectBestTarget(attacker, tauntTargets, boardState, mode, isLastMonster, lastMonsterIgnoreChance);
            }

            // Handle overwhelm with multiple targets
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && targets.Count > 1)
                return _overwhelmEvaluator.SelectTargetForOverwhelmAttacker(attacker, targets, boardState);

            return SelectBestTarget(attacker, targets, boardState, mode, isLastMonster, lastMonsterIgnoreChance);
        }

        private EntityManager SelectBestTarget(
            EntityManager attacker, 
            List<EntityManager> targets,
            BoardState boardState, 
            StrategicMode mode,
            bool isLastMonster,
            float lastMonsterIgnoreChance)
        {
            if (attacker == null || targets == null || targets.Count == 0)
                return null;

            var validTargets = targets.Where(t => t != null && !t.dead && !t.IsFadingOut).ToList();
            if (validTargets.Count == 0) return null;
            if (validTargets.Count == 1) return validTargets[0];

            // Chance to ignore last monster protection (simulate human error)
            if (Random.value < lastMonsterIgnoreChance && isLastMonster)
            {
                Debug.Log("[TargetSelectionStrategy] Occasionally ignoring last monster protection (simulating human error)");
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
                    if (isLastMonster && _tradeEvaluator.WouldClearBoard(attacker, target, validTargets, true))
                    {
                        score += 200f;
                        Debug.Log($"[TargetSelectionStrategy] Trading last monster with {target.name} would clear the board - strategically valuable");
                    }

                    targetScores[target] = score;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[TargetSelectionStrategy] Error evaluating target: {e.Message}");
                }
            }

            var sortedTargets = targetScores.OrderByDescending(kvp => kvp.Value).ToList();
            if (sortedTargets.Count == 0) return null;

            // Sometimes make suboptimal choice to simulate human error
            if (ShouldMakeSuboptimalDecision() && sortedTargets.Count > 1)
            {
                int randomIndex = Random.Range(1, Mathf.Min(sortedTargets.Count, 3));
                Debug.Log($"[TargetSelectionStrategy] Making suboptimal choice (human error simulation)");
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
                bool isValuableTrade = _tradeEvaluator.IsValuableTrade(attacker, target, boardState);

                if (isValuableTrade)
                {
                    Debug.Log($"[TargetSelectionStrategy] Allowing valuable trade: {attacker.name} for {target.name}");
                    return baseScore + 50f;
                }
                else
                {
                    Debug.Log($"[TargetSelectionStrategy] Avoiding attacking {target.name} with our only monster - not worth the trade");
                    return baseScore - 1000f;
                }
            }
            else if (target.GetAttack() == 0)
            {
                // Target has 0 attack - perfectly safe to attack
                Debug.Log($"[TargetSelectionStrategy] Target {target.name} has 0 attack - safe to attack with our last monster");
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
                        score += _tradeEvaluator.IsValuableTrade(attacker, target, boardState) ? 30f : -30f;
                    }
                    else
                    {
                        score += 40f;
                        Debug.Log($"[TargetSelectionStrategy] Prioritizing killing {target.name} before player's next turn");
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
                        score += _tradeEvaluator.IsValuableTrade(attacker, target, boardState) ? 20f : -10f;
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
                    Debug.Log($"[TargetSelectionStrategy] Damaging {target.name} to finish next turn when we go first");
                }
            }

            return score;
        }

        private bool ShouldMakeSuboptimalDecision() => Random.value < _decisionVariance;
    }
}
