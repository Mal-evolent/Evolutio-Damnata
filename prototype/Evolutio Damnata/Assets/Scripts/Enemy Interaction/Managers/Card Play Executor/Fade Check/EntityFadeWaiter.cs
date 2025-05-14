using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyInteraction.Managers.Execution
{
    public class EntityFadeWaiter
    {
        private const float MAX_FADE_WAIT_TIME = 7.0f;
        private const float ADDITIONAL_CLARITY_DELAY = 0.2f;

        public IEnumerator WaitForFadeOutAtPosition(CombatStage combatStage, int position)
        {
            if (combatStage == null || position < 0)
                yield break;

            var enemyEntities = combatStage.SpritePositioning.EnemyEntities;
            if (position < enemyEntities.Count)
            {
                var entityAtPosition = enemyEntities[position];
                if (entityAtPosition != null)
                {
                    var entityManager = entityAtPosition.GetComponent<EntityManager>();
                    if (entityManager != null && entityManager.IsFadingOut)
                    {
                        Debug.Log($"[EntityFadeWaiter] Waiting for entity at position {position} to finish fading out");

                        // Wait until IsFadingOut becomes false
                        float elapsedTime = 0f;

                        while (entityManager.IsFadingOut && elapsedTime < MAX_FADE_WAIT_TIME)
                        {
                            yield return null;
                            elapsedTime += Time.deltaTime;
                        }

                        Debug.Log($"[EntityFadeWaiter] Entity at position {position} finished fading out (waited {elapsedTime:F2}s)");

                        // Add a small delay after fade completes for visual clarity
                        yield return new WaitForSeconds(ADDITIONAL_CLARITY_DELAY);
                    }
                }
            }
        }

        public IEnumerator WaitForTargetsFadeOut(SpellTargetSelector spellTargetSelector, CardData spellCard)
        {
            if (spellCard.EffectTypes == null || spellCard.EffectTypes.Count == 0)
                yield break;

            // For area effects and multi-target spells, we need to wait for any potential target
            // that's currently fading out
            var allPotentialTargets = new List<EntityManager>();

            foreach (var effect in spellCard.EffectTypes)
            {
                var targetsForEffect = spellTargetSelector.GetAllValidTargets(effect);
                if (targetsForEffect != null && targetsForEffect.Count > 0)
                {
                    allPotentialTargets.AddRange(targetsForEffect);
                }
            }

            // Remove duplicates
            allPotentialTargets = allPotentialTargets.Distinct().ToList();

            // Check if any targets are fading out
            bool anyFadingOut = allPotentialTargets.Any(t => t != null && t.IsFadingOut);

            if (anyFadingOut)
            {
                Debug.Log($"[EntityFadeWaiter] Waiting for targets of spell to finish fading out");

                // Wait until all relevant targets finish fading out
                float elapsedTime = 0f;

                while (allPotentialTargets.Any(t => t != null && t.IsFadingOut) && elapsedTime < MAX_FADE_WAIT_TIME)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                Debug.Log($"[EntityFadeWaiter] All spell targets finished fading out (waited {elapsedTime:F2}s)");

                // Add a small delay after fade completes for visual clarity
                yield return new WaitForSeconds(ADDITIONAL_CLARITY_DELAY);
            }
        }
    }
}
