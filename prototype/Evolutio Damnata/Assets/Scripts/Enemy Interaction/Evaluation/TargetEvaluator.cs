using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;
using EnemyInteraction.Evaluation;

namespace EnemyInteraction.Managers
{
    public class TargetEvaluator : MonoBehaviour, ITargetEvaluator
    {
        private IKeywordEvaluator _keywordEvaluator;
        private IEntityCacheManager _entityCacheManager;
        
        [SerializeField, Range(0f, 0.5f), Tooltip("Variance in target evaluation scores")]
        private float _evaluationVariance = 0.15f;
        
        public void Initialize(IKeywordEvaluator keywordEvaluator, IEntityCacheManager entityCacheManager)
        {
            _keywordEvaluator = keywordEvaluator;
            _entityCacheManager = entityCacheManager;
        }
        
        public float EvaluateTarget(EntityManager attacker, EntityManager target, BoardState boardState, StrategicMode mode)
        {
            if (attacker == null || target == null)
                return float.MinValue;

            float score = 0;

            // Base scoring
            score += attacker.GetAttack() * 1.2f - target.GetHealth() * 0.8f;

            // Strategy-specific scoring
            if (mode == StrategicMode.Aggro)
            {
                score += target.GetAttack() * 0.7f;
                if (target.GetHealth() <= attacker.GetAttack())
                    score += 90f;
            }
            else // Defensive
            {
                score -= attacker.GetHealth() * 0.2f;
                if (target.HasKeyword(Keywords.MonsterKeyword.Taunt))
                    score += 60f;
            }

            // Turn order considerations
            score += EvaluateTurnOrderConsiderations(attacker, target, boardState);

            // Keyword interactions
            score += EvaluateKeywordInteractions(attacker, target, boardState);

            // External keyword evaluation
            if (_keywordEvaluator != null)
            {
                score += _keywordEvaluator.EvaluateKeywords(attacker, target, boardState) * 1.2f;
            }

            // Counterattack consideration for non-ranged attackers
            if (!attacker.HasKeyword(Keywords.MonsterKeyword.Ranged))
            {
                if (attacker.GetHealth() <= target.GetAttack())
                {
                    score -= 80f;
                }
                else
                {
                    score -= (target.GetAttack() / attacker.GetHealth()) * 40f;
                }
            }

            // Clamp the score to a reasonable range before adding variance
            float clampedScore = Mathf.Clamp(score, -100f, 200f);

            // Add randomness to simulate human decision making - use additive instead of multiplicative
            float varianceAmount = Mathf.Min(Mathf.Abs(clampedScore) * _evaluationVariance, 20f);
            float randomVariance = Random.Range(-varianceAmount, varianceAmount);

            return clampedScore + randomVariance;
        }
        
        public float EvaluateTurnOrderConsiderations(EntityManager attacker, EntityManager target, BoardState boardState)
        {
            if (boardState == null) return 0f;

            float score = 0f;
            bool playerGoesFirstNextTurn = boardState.IsNextTurnPlayerFirst;

            // When player goes first next turn
            if (playerGoesFirstNextTurn)
            {
                // Prioritize killing threats before player's turn
                if (target.GetAttack() >= 4)
                {
                    score += 30f;
                    Debug.Log($"[TargetEvaluator] Targeting high attack unit {target.name} before player's turn");

                    if (target.GetHealth() <= attacker.GetAttack())
                    {
                        score += 40f;
                        Debug.Log($"[TargetEvaluator] Prioritizing killing {target.name} before player's next turn");
                    }
                }

                // Prioritize protecting valuable attackers that might die to counterattack
                if (attacker.GetAttack() >= 4 && target.GetAttack() >= attacker.GetHealth())
                {
                    score -= 30f; // Bad trade before player's turn
                }

                // Value ranged and tough attackers more
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Ranged)) score += 20f;
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Tough)) score += 15f;
            }
            // When enemy goes first next turn
            else
            {
                // Setup for two-turn kills
                if (target.GetHealth() > attacker.GetAttack() && target.GetHealth() <= attacker.GetAttack() * 2)
                {
                    score += 30f;
                    Debug.Log($"[TargetEvaluator] Setting up {target.name} for kill next turn when we go first");
                }

                // Less concerned about immediate kills since we attack again soon
                if (target.GetHealth() <= attacker.GetAttack())
                {
                    score -= 10f;
                }

                // Less concerned about immediate survival since we go again soon
                if (attacker.GetHealth() <= target.GetAttack())
                {
                    score += 15f; // Sacrifice can be worth it if we go first next
                }

                // Overwhelm attackers get extra value if we go first next
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
                {
                    score += 20f;
                    Debug.Log($"[TargetEvaluator] Prioritizing Overwhelm attacks to be followed up next turn");
                }
            }

            return score;
        }
        
        public float EvaluateKeywordInteractions(EntityManager attacker, EntityManager target, BoardState boardState)
        {
            if (attacker == null || target == null)
                return 0f;

            float score = 0f;

            // Evaluate Overwhelm offensive potential
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
            {
                // Calculate potential splash damage
                float splashDamage = Mathf.Floor(attacker.GetAttack() * 0.5f);

                // Get all other entities on target's side
                var targetSideEntities = target.GetMonsterType() == EntityManager.MonsterType.Enemy ?
                    _entityCacheManager.CachedEnemyEntities : _entityCacheManager.CachedPlayerEntities;

                // Count how many entities could be damaged by splash
                int splashTargets = targetSideEntities.Count(e => e != target && !e.dead && !e.IsFadingOut);

                // Count how many could potentially die from splash damage
                int potentialSplashKills = targetSideEntities.Count(e =>
                    e != target && !e.dead && !e.IsFadingOut && e.GetHealth() <= splashDamage);

                // Use balanced additive scoring instead of large multipliers
                // Cap the splash damage value to avoid inflation
                float splashDamageBonus = Mathf.Min(splashDamage * splashTargets * 0.8f, 30f);
                score += splashDamageBonus;

                // Cap the kill bonus to prevent score inflation with grouped targets
                float killBonus = Mathf.Min(potentialSplashKills * 15f, 35f);
                score += killBonus;

                Debug.Log($"[TargetEvaluator] Evaluating Overwhelm: {splashTargets} splash targets, " +
                          $"{potentialSplashKills} potential splash kills, adding total {splashDamageBonus + killBonus} to score");
            }

            // Evaluate attacking against Tough defenders
            if (target.HasKeyword(Keywords.MonsterKeyword.Tough))
            {
                // Tough reduces damage by half, making the target less attractive
                score -= 15f; // Reduced penalty

                float damageAfterTough = Mathf.Floor(attacker.GetAttack() / 2f);
                if (damageAfterTough >= target.GetHealth())
                {
                    // We can still kill it despite Tough - high priority target, but capped
                    score += 25f; // Reduced from 40f
                    Debug.Log($"[TargetEvaluator] Can kill Tough entity {target.name} with {attacker.name}");
                }

                // If we have Overwhelm, targeting a Tough unit might still be good
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
                {
                    score += 10f; // Reduced from 15f
                    Debug.Log($"[TargetEvaluator] Overwhelm attack against Tough target still valuable for splash");
                }
            }

            // Evaluate attacking with Tough attackers
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Tough))
            {
                // Tough attackers take less counter damage
                score += 10f; // Reduced from 15f

                // Extra value when attacking a high-attack target
                if (target.GetAttack() >= 4)
                {
                    score += 15f; // Reduced from 20f
                    Debug.Log($"[TargetEvaluator] Using Tough attacker {attacker.name} against high-attack target {target.name}");
                }

                // If attacker won't die from counter attack due to Tough
                float counterDamage = Mathf.Floor(target.GetAttack() / 2f);
                if (counterDamage < attacker.GetHealth())
                {
                    score += 20f; // Reduced from 30f
                    Debug.Log($"[TargetEvaluator] Tough attacker {attacker.name} will survive counter attack");
                }
            }

            // Apply a global cap to prevent extreme values from keyword interactions
            return Mathf.Clamp(score, -60f, 60f);
        }
    }
}
