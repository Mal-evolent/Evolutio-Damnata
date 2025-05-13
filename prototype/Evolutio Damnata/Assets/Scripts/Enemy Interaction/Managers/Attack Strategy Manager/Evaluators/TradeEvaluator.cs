using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;

namespace EnemyInteraction.Managers.AttackStrategy.Evaluators
{
    public class TradeEvaluator
    {
        private readonly float _valuableTradeRatio;

        public TradeEvaluator(float valuableTradeRatio)
        {
            _valuableTradeRatio = valuableTradeRatio;
        }

        public bool IsValuableTrade(EntityManager attacker, EntityManager target, BoardState boardState = null)
        {
            float attackerValue = CalculateEntityValue(attacker);
            float targetValue = CalculateEntityValue(target);
            float valueRatio = targetValue / attackerValue;

            // High threat targets are always worth trading for
            if (target.GetAttack() >= 6)
            {
                Debug.Log($"[TradeEvaluator] High threat target ({target.name} with {target.GetAttack()} attack) - worth trading");
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
            Debug.Log($"[TradeEvaluator] Trade evaluation: {attacker.name} ({attackerValue:F1}) for {target.name} ({targetValue:F1}), " +
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
                Debug.Log($"[TradeEvaluator] Strong board advantage ({boardAdvantage:F2}x) - requiring better trades (+0.3)");
            }
            else if (boardAdvantage > 1.2f)
            {
                adjustedRatio += 0.15f;
                Debug.Log($"[TradeEvaluator] Slight board advantage ({boardAdvantage:F2}x) - requiring better trades (+0.15)");
            }
            else if (boardAdvantage < 0.8f)
            {
                adjustedRatio -= 0.2f; // Accept worse trades when behind
                Debug.Log($"[TradeEvaluator] Board disadvantage ({boardAdvantage:F2}x) - accepting worse trades (-0.2)");
            }

            // Turn count consideration
            if (boardState.TurnCount >= 4)
            {
                adjustedRatio -= 0.15f; // More aggressive in late game
                Debug.Log($"[TradeEvaluator] Late game (turn {boardState.TurnCount}) - accepting worse trades (-0.15)");
            }

            // Turn order considerations
            adjustedRatio += boardState.IsNextTurnPlayerFirst ? 0.1f : -0.1f;

            // Health considerations
            if (boardState.PlayerHealth <= 15)
            {
                adjustedRatio -= 0.2f; // More aggressive when player is low
                Debug.Log($"[TradeEvaluator] Player at low health ({boardState.PlayerHealth}) - more willing to trade (-0.2)");
            }
            else if (boardState.EnemyHealth <= 15)
            {
                adjustedRatio += 0.2f; // More careful when we're low
                Debug.Log($"[TradeEvaluator] Enemy at low health ({boardState.EnemyHealth}) - more careful about trades (+0.2)");
            }
            
            return adjustedRatio;
        }

        public float CalculateEntityValue(EntityManager entity)
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

        public bool WouldClearBoard(EntityManager attacker, EntityManager target, List<EntityManager> allTargets, bool isLastMonster)
        {
            if (!isLastMonster) return false;

            bool attackerWouldDie = !attacker.HasKeyword(Keywords.MonsterKeyword.Ranged) &&
                                   target.GetAttack() > 0 &&
                                   target.GetAttack() >= attacker.GetHealth();

            bool isLastTarget = allTargets.Count == 1;
            bool targetWouldDie = attacker.GetAttack() >= target.GetHealth();

            return attackerWouldDie && isLastTarget && targetWouldDie;
        }
    }
}
