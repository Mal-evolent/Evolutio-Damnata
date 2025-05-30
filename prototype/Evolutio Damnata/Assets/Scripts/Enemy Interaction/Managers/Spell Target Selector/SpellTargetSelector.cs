using EnemyInteraction.Models;
using EnemyInteraction.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyInteraction.Managers.Targeting
{
    public class SpellTargetSelector
    {
        private readonly SpritePositioning _spritePositioning;
        private readonly Dictionary<GameObject, EntityManager> _entityCache;

        public SpellTargetSelector(SpritePositioning spritePositioning, Dictionary<GameObject, EntityManager> entityCache)
        {
            _spritePositioning = spritePositioning;
            _entityCache = entityCache;
        }

        public EntityManager GetBestSpellTarget(CardData cardType)
        {
            if (cardType?.EffectTypes == null || !cardType.EffectTypes.Any())
                return null;

            // For Draw/Bloodprice only cards, return a dummy target
            if (ContainsOnlyDrawAndBloodpriceEffects(cardType))
            {
                return GetDummyTarget();
            }

            // For other spells, find the most appropriate target
            var effect = cardType.EffectTypes.FirstOrDefault(e =>
                e != SpellEffect.Draw && e != SpellEffect.Bloodprice);

            // If no targetable effect is found, use the first effect
            if (effect == 0)
                effect = cardType.EffectTypes.First();

            bool isDamagingEffect = effect == SpellEffect.Damage || effect == SpellEffect.Burn;

            var potentialTargets = GetAllValidTargets(effect);
            if (potentialTargets.Count == 0)
                return null;

            // Filter targets using AIUtilities to ensure proper targeting
            var validTargets = potentialTargets
                .Where(target => AIUtilities.IsValidTargetForEffect(target, effect, isDamagingEffect))
                .ToList();

            if (validTargets.Count == 0)
            {
                Debug.LogWarning($"[SpellTargetSelector] Found {potentialTargets.Count} targets but none were valid for {effect}");
                return null;
            }

            return validTargets
                .OrderByDescending(t => CalculateThreatScore(t, cardType))
                .FirstOrDefault();
        }

        public bool ContainsOnlyDrawAndBloodpriceEffects(CardData cardData)
        {
            if (cardData?.EffectTypes == null || !cardData.EffectTypes.Any())
                return false;

            foreach (var effect in cardData.EffectTypes)
            {
                if (effect != SpellEffect.Draw && effect != SpellEffect.Bloodprice)
                    return false;
            }

            return true;
        }

        public EntityManager GetDummyTarget()
        {
            // For cards with only Draw/Bloodprice effects, we can use any entity including empty placeholders

            // First try to find the enemy health icon as it's a safe target for utility spells
            var enemyHealth = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
            if (enemyHealth != null)
                return enemyHealth;

            // If enemy health icon not available, try to find any enemy entity - including empty placeholders
            if (_spritePositioning != null && _entityCache.Count > 0)
            {
                // For Draw/Bloodprice only effects, we can use empty placeholders as well
                foreach (var entity in _spritePositioning.EnemyEntities)
                {
                    if (entity != null && _entityCache.TryGetValue(entity, out var entityManager) && entityManager != null)
                    {
                        // Return the entity whether it's placed or not, since Draw/Bloodprice don't need actual targets
                        return entityManager;
                    }
                }
            }

            // As a fallback, try to find any valid entity in the cache
            var anyEntity = _entityCache.Values.FirstOrDefault(e => e != null);
            if (anyEntity != null)
                return anyEntity;

            // Ultimate fallback - create a temporary entity if needed
            Debug.LogWarning("[SpellTargetSelector] No valid entities found for dummy target, creating a temporary one");

            return CreateTemporaryEntityForDummyTarget();
        }

        private EntityManager CreateTemporaryEntityForDummyTarget()
        {
            // Create a temporary, invisible entity that can be used as a target
            // This entity won't be displayed or affect gameplay
            var tempEntity = new GameObject("TempDummyTarget").AddComponent<EntityManager>();
            tempEntity.gameObject.SetActive(false);

            // Destroy it after a short delay
            Object.Destroy(tempEntity.gameObject, 0.5f);

            return tempEntity;
        }

        public List<EntityManager> GetAllValidTargets(SpellEffect effect)
        {
            var targets = new List<EntityManager>();

            // For damage effects, add player entities and player health icon
            if (effect == SpellEffect.Damage || effect == SpellEffect.Burn)
            {
                // Get all placed player entities first
                var playerEntities = new List<EntityManager>();
                if (_spritePositioning != null)
                {
                    playerEntities = _spritePositioning.PlayerEntities
                        .Where(e => e != null)
                        .Select(e => _entityCache.TryGetValue(e, out var em) ? em : null)
                        .Where(e => e != null && e.placed)
                        .ToList();
                }

                // Check for taunt units
                var tauntUnits = playerEntities.Where(e => e.HasKeyword(Keywords.MonsterKeyword.Taunt)).ToList();

                // If there are taunt units, only they can be targeted
                if (tauntUnits.Any())
                {
                    Debug.Log("[SpellTargetSelector] Restricting targeting to taunt units only");
                    targets.AddRange(tauntUnits);
                }
                else
                {
                    // Add all player entities if no taunts
                    targets.AddRange(playerEntities);

                    // Add player health icon ONLY when no player entities on the field
                    if (playerEntities.Count == 0)
                    {
                        var playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
                        if (playerHealth != null) targets.Add(playerHealth);
                    }
                }
            }
            // For healing effects, add enemy entities and enemy health icon
            else if (effect == SpellEffect.Heal)
            {
                // For healing, we don't need to check for taunt since it only affects
                // targeting of opposing units, and healing targets allied units

                // Add enemy entities
                if (_spritePositioning != null)
                {
                    targets.AddRange(
                        _spritePositioning.EnemyEntities
                            .Where(e => e != null)
                            .Select(e => _entityCache.TryGetValue(e, out var em) ? em : null)
                            .Where(e => e != null && e.placed)
                    );
                }

                // Add enemy health icon as a potential target for healing
                var enemyHealth = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
                if (enemyHealth != null && enemyHealth.GetHealth() < enemyHealth.MaxHealth)
                {
                    targets.Add(enemyHealth);
                }
            }

            return targets;
        }

        public float CalculateThreatScore(EntityManager target, CardData cardType)
        {
            float score = 0f;

            // Check if this is a healing effect
            bool isHealingEffect = cardType.EffectTypes != null &&
                                   cardType.EffectTypes.Contains(SpellEffect.Heal);

            // Calculate score based on target type
            if (target is HealthIconManager healthIcon)
            {
                if (isHealingEffect)
                {
                    // Cast to float to ensure proper division
                    float healthPercentage = (float)healthIcon.GetHealth() / (float)healthIcon.MaxHealth;

                    // Higher score for more damaged health pools (scaled down)
                    score = (1f - healthPercentage) * 80f;

                    // Critical priority if health is below 25% (reduced slightly)
                    if (healthPercentage < 0.25f)
                        score += 60f;
                }
                else
                {
                    // Damage targeting logic (adjusted to be less aggressive)
                    score = healthIcon.GetHealth() * 0.6f;

                    // Critical priority for low health (reduced to balance with other priorities)
                    if (healthIcon.GetHealth() < 10)
                        score += 70f;
                }
            }
            else
            {
                // Regular entity
                if (isHealingEffect)
                {
                    float missingHealth = target.GetMaxHealth() - target.GetHealth();
                    float healthPercentage = target.GetHealth() / (float)target.GetMaxHealth();
                    float entityValue = target.GetAttack() * 1.2f + target.GetMaxHealth();

                    
                    score = missingHealth * 8f + entityValue * (1f - healthPercentage) * 1.5f;

                    // Bonus for high-value minions (slightly reduced)
                    if (target.GetAttack() >= 5 || target.GetMaxHealth() >= 5)
                        score += 25f;

                    // Consistent keyword bonus values
                    if (target.HasKeyword(Keywords.MonsterKeyword.Taunt))
                        score += 30f;

                    if (target.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
                        score += 25f;
                }
                else
                {
                    // Damage targeting logic (adjusted weights)
                    score = target.GetAttack() * 1.5f + target.GetHealth() * 0.7f;

                    // Keyword modifiers (more balanced values)
                    if (target.HasKeyword(Keywords.MonsterKeyword.Taunt))
                        score += 35f;

                    if (target.HasKeyword(Keywords.MonsterKeyword.Tough))
                    {
                        score += 20f;
                        if (cardType.EffectTypes != null && cardType.EffectTypes.Contains(SpellEffect.Damage))
                        {
                            score -= 10f; // Reduced penalty for damage spells against Tough
                        }
                    }

                    if (target.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
                    {
                        score += 30f; // Slightly reduced
                    }

                    if (target.HasKeyword(Keywords.MonsterKeyword.Ranged))
                    {
                        score += 25f; // Slightly reduced

                        if (cardType.EffectTypes != null && cardType.EffectTypes.Contains(SpellEffect.Damage))
                        {
                            score += 15f; // Increased to make spell targeting ranged units more valuable
                        }
                    }
                }
            }

            // Add a small variance to prevent ties and make targeting less predictable
            score += Random.Range(-2f, 2f);

            return score;
        }
    }
}
