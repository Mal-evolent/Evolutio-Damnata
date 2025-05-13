using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;

namespace EnemyInteraction.Managers.AttackStrategy.Evaluators
{
    public class OverwhelmEvaluator
    {
        public EntityManager SelectTargetForOverwhelmAttacker(EntityManager attacker, List<EntityManager> targets, BoardState boardState)
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
    }
}
