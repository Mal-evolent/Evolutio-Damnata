using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;

namespace EnemyInteraction.Managers.AttackStrategy.Evaluators
{
    public class TargetEvaluator : ITargetEvaluator
    {
        public float EvaluateTarget(EntityManager attacker, EntityManager target, BoardState boardState, StrategicMode mode)
        {
            float score = 0;

            // Basic score from target's stats
            score += EvaluateBasicStats(attacker, target);

            // Score adjustments based on keywords
            score += EvaluateKeywords(attacker, target);

            // Keyword interactions with board state
            if (boardState != null)
            {
                score += EvaluateKeywordInteractions(attacker, target, boardState);
            }

            // Strategic mode adjustments
            score += EvaluateStrategicMode(attacker, target, mode);

            // Board state considerations
            if (boardState != null)
            {
                score += EvaluateBoardState(attacker, target, boardState);
                score += EvaluateTurnOrderConsiderations(attacker, target, boardState);
            }

            return score;
        }

        private float EvaluateBasicStats(EntityManager attacker, EntityManager target)
        {
            float score = 0;

            // Kill priority
            if (attacker.GetAttack() >= target.GetHealth())
            {
                score += 80f;
                score += target.GetAttack() * 5f;  // Value killing high attack targets more
            }

            // Value based on target's stats
            score += target.GetAttack() * 2f;  // High attack targets are threatening
            score += target.GetHealth() * 0.5f;  // Health is a secondary consideration

            return score;
        }

        private float EvaluateKeywords(EntityManager attacker, EntityManager target)
        {
            float score = 0;

            // Target keyword analysis
            if (target.HasKeyword(Keywords.MonsterKeyword.Taunt))
                score += 20f;  // Taunt units should be prioritized

            if (target.HasKeyword(Keywords.MonsterKeyword.Ranged))
                score += 30f;  // Ranged units can't be countered, focus them

            if (target.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
                score += 25f;  // Overwhelm units can do splash damage, focus them

            // Attacker keyword considerations
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Ranged))
            {
                score += 10f;  // Ranged attackers can safely attack any target
                score += target.GetAttack() * 1.5f;  // Prioritize high attack targets with ranged
            }

            return score;
        }

        public float EvaluateKeywordInteractions(EntityManager attacker, EntityManager target, BoardState boardState)
        {
            float score = 0;

            // Advanced keyword interactions based on board state
            if (boardState == null)
                return score;

            // Evaluate keyword effectiveness based on board context

            // Ranged attacker consideration in different board states
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Ranged))
            {
                // Ranged is more valuable when the board is contested
                float boardAdvantage = boardState.EnemyBoardControl / Mathf.Max(0.1f, boardState.PlayerBoardControl);
                if (boardAdvantage < 1.2f)
                {
                    score += 15f; // Ranged is more valuable when not clearly ahead
                }

                // Ranged is good against high attack targets
                if (target.GetAttack() >= 4)
                {
                    score += 25f;
                    Debug.Log($"[TargetEvaluator] {attacker.name} (Ranged) prioritizing high-attack target {target.name}");
                }
            }

            // Overwhelm is more valuable against clustered board
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && boardState.PlayerBoardControl > 20)
            {
                score += 20f;
                Debug.Log($"[TargetEvaluator] {attacker.name} (Overwhelm) gets bonus against clustered board");
            }

            // Anti-keyword considerations
            if (target.HasKeyword(Keywords.MonsterKeyword.Taunt) && attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
            {
                score += 15f; // Overwhelm is good against taunt
            }

            // Late game keyword considerations
            if (boardState.TurnCount >= 4)
            {
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Tough) && !target.HasKeyword(Keywords.MonsterKeyword.Ranged))
                {
                    score += 10f; // Tough is more valuable in extended combats
                }
            }

            return score;
        }

        private float EvaluateStrategicMode(EntityManager attacker, EntityManager target, StrategicMode mode)
        {
            float score = 0;

            // Adjust score based on strategic mode
            switch (mode)
            {
                case StrategicMode.Aggro:
                    score += target.GetAttack() * 3f;  // Higher priority on removing threats

                    if (attacker.GetAttack() >= target.GetHealth())
                        score += 30f;  // Killing is even more valuable when aggressive
                    break;

                case StrategicMode.Defensive:
                    if (attacker.HasKeyword(Keywords.MonsterKeyword.Ranged))
                        score += 40f;  // Ranged units are safer in defensive stance

                    if (target.GetAttack() >= attacker.GetHealth() && !attacker.HasKeyword(Keywords.MonsterKeyword.Ranged))
                        score -= 30f;  // Avoid suicide attacks when being defensive
                    break;
            }

            return score;
        }

        private float EvaluateBoardState(EntityManager attacker, EntityManager target, BoardState boardState)
        {
            float score = 0;

            // Turn order considerations
            if (boardState.IsNextTurnPlayerFirst)
            {
                if (target.GetAttack() >= 4 && attacker.GetAttack() >= target.GetHealth())
                {
                    score += 40f; // Prioritize killing high attack threats before player's turn
                }
            }

            return score;
        }

        public float EvaluateTurnOrderConsiderations(EntityManager attacker, EntityManager target, BoardState boardState)
        {
            float score = 0;

            if (boardState == null)
                return score;

            // Player goes first next turn considerations
            if (boardState.IsNextTurnPlayerFirst)
            {
                // Critical to remove threats if player goes next
                if (target.GetAttack() >= 3)
                {
                    score += 15f;

                    // Higher priority if we can actually kill it
                    if (attacker.GetAttack() >= target.GetHealth())
                    {
                        score += 30f;
                        Debug.Log($"[TargetEvaluator] Prioritizing killing {target.name} before player's turn");
                    }
                }

                // Consider key targets that could threaten lethal next turn
                if (boardState.EnemyHealth <= 10 && target.GetAttack() >= 3)
                {
                    score += 25f;
                    Debug.Log($"[TargetEvaluator] Defensive priority on {target.name} to prevent lethal next turn");
                }
            }
            // Enemy goes first next turn
            else
            {
                // Can set up for next turn kill
                if (target.GetHealth() > attacker.GetAttack() &&
                    target.GetHealth() <= attacker.GetAttack() * 2)
                {
                    score += 15f;
                    Debug.Log($"[TargetEvaluator] Setting up {target.name} for kill next turn when we go first");
                }

                // Setup combos for next turn when we go first
                if (boardState.EnemyBoardControl > boardState.PlayerBoardControl &&
                    target.GetAttack() >= 3)
                {
                    score += 10f;
                }
            }

            // Late game turn considerations
            if (boardState.TurnCount >= 5)
            {
                // In late game, each turn matters more
                if (boardState.IsNextTurnPlayerFirst && target.GetAttack() >= 2)
                {
                    score += 10f;
                }
            }

            return score;
        }
    }
}
