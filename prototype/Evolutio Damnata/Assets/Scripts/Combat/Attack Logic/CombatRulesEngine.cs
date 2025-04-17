using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Interface for combat rules engines
/// </summary>
public interface ICombatRulesEngine
{
    AttackRuleResult ApplyRules(EntityManager attacker, EntityManager target, float baseAttackerDamage, float baseTargetDamage);
    /// <summary>
    /// Applies splash damage to all entities on the same side as the target
    /// </summary>
    /// <param name="result">The attack result containing splash damage information</param>
    /// <param name="entitiesOnSameSide">List of GameObjects on the same side as the target</param>
    public void ApplySplashDamage(AttackRuleResult result, List<GameObject> entitiesOnSameSide)
    {
    }
}

public class CombatRulesEngine : ICombatRulesEngine
{
    /// <summary>
    /// Applies all combat rules and returns the modified attack values and behaviors
    /// </summary>
    public AttackRuleResult ApplyRules(EntityManager attacker, EntityManager target, float baseAttackerDamage, float baseTargetDamage)
    {
        AttackRuleResult result = new AttackRuleResult
        {
            ModifiedAttackerDamage = baseAttackerDamage,
            ModifiedTargetDamage = baseTargetDamage,
            ShouldTakeCounterDamage = true,
            RuleDescription = "Normal Attack"
        };

        // Apply attacker's offensive keywords
        ApplyRangedRule(attacker, result);
        ApplyOverwhelmRule(attacker, target, result);

        // NOTE: We no longer reduce damage here since it's handled in EntityManager.TakeDamage
        // We just update the description for clarity
        ApplyToughRuleDescription(target, result, "Defender");

        // NOTE: We no longer reduce damage here since it's handled in EntityManager.TakeDamage
        // We just update the description for clarity
        ApplyToughRuleDescription(attacker, result, "Attacker");

        return result;
    }

    /// <summary>
    /// Ranged attackers don't take counter damage
    /// </summary>
    private void ApplyRangedRule(EntityManager attacker, AttackRuleResult result)
    {
        if (attacker != null && attacker.HasKeyword(Keywords.MonsterKeyword.Ranged))
        {
            result.ShouldTakeCounterDamage = false;
            result.RuleDescription = "Ranged";
            Debug.Log($"[CombatRulesEngine] {attacker.name} attacks from range and avoids counter-damage!");
        }
    }

    /// <summary>
    /// Add Tough description to the result (damage reduction now handled in EntityManager)
    /// </summary>
    private void ApplyToughRuleDescription(EntityManager entity, AttackRuleResult result, string role)
    {
        if (entity != null && entity.HasKeyword(Keywords.MonsterKeyword.Tough))
        {
            string currentDescription = result.RuleDescription;
            result.RuleDescription = string.IsNullOrEmpty(currentDescription) || currentDescription == "Normal Attack" ?
                $"{role} Tough" : $"{currentDescription}, {role} Tough";

            Debug.Log($"[CombatRulesEngine] {entity.name} is tough and will reduce all incoming damage by half!");
        }
    }

    /// <summary>
    /// Overwhelm attackers deal splash damage to all other active entities on the same side
    /// </summary>
    private void ApplyOverwhelmRule(EntityManager attacker, EntityManager target, AttackRuleResult result)
    {
        if (attacker != null && attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
        {
            // Calculate splash damage (half of the regular damage)
            float splashDamage = Mathf.Floor(result.ModifiedAttackerDamage * 0.5f);

            // Store the target's side (enemy or player) to find other entities later
            result.IsTargetEnemy = target.GetMonsterType() == EntityManager.MonsterType.Enemy;
            result.HasSplashDamage = true;
            result.SplashDamageAmount = splashDamage;
            result.TargetEntity = target;
            result.SourceAttacker = attacker;

            string currentDescription = result.RuleDescription;
            result.RuleDescription = string.IsNullOrEmpty(currentDescription) || currentDescription == "Normal Attack" ?
                "Overwhelm" : $"{currentDescription}, Overwhelm";

            Debug.Log($"[CombatRulesEngine] {attacker.name} attacks with Overwhelm, causing {splashDamage} splash damage to all other units!");
        }
    }

    /// <summary>
    /// Applies splash damage to all entities on the same side as the target
    /// </summary>
    /// <param name="result">The attack result containing splash damage information</param>
    /// <param name="entitiesOnSameSide">List of GameObjects on the same side as the target</param>
    public void ApplySplashDamage(AttackRuleResult result, List<GameObject> entitiesOnSameSide)
    {
        if (!result.HasSplashDamage || entitiesOnSameSide == null || entitiesOnSameSide.Count == 0 || result.SourceAttacker == null)
            return;

        Debug.Log($"[CombatRulesEngine] Applying splash damage of {result.SplashDamageAmount} from {result.SourceAttacker.name} to entities on same side");

        foreach (var entityObj in entitiesOnSameSide)
        {
            // Skip null objects
            if (entityObj == null) continue;

            EntityManager entity = entityObj.GetComponent<EntityManager>();

            // Skip if entity is null, is the primary target, is dead, is not placed, or is fading out
            if (entity == null ||
                entity == result.TargetEntity ||
                entity.dead ||
                !entity.placed ||
                entity.IsFadingOut)
            {
                continue;
            }

            // Set the entity's killer to the original attacker before applying damage
            entity.SetKilledBy(result.SourceAttacker);

            // Apply splash damage to valid entity
            entity.TakeDamage(result.SplashDamageAmount);
            Debug.Log($"[CombatRulesEngine] {entity.name} took {result.SplashDamageAmount} splash damage from {result.SourceAttacker.name}'s Overwhelm effect");
        }
    }


    /// <summary>
    /// Checks if any entities in the provided list have the Taunt keyword
    /// </summary>
    public static bool HasTauntUnits(List<GameObject> entities)
    {
        if (entities == null) return false;

        return entities.Any(e =>
        {
            var entityManager = e?.GetComponent<EntityManager>();
            return entityManager != null &&
                   !entityManager.dead &&
                   entityManager.placed &&
                   !entityManager.IsFadingOut &&
                   entityManager.HasKeyword(Keywords.MonsterKeyword.Taunt);
        });
    }

    /// <summary>
    /// Returns all EntityManagers in the provided list that have the Taunt keyword
    /// </summary>
    public static List<EntityManager> GetAllTauntUnits(List<GameObject> entities)
    {
        if (entities == null) return new List<EntityManager>();

        return entities
            .Select(e => e?.GetComponent<EntityManager>())
            .Where(e => e != null &&
                       !e.dead &&
                       e.placed &&
                       !e.IsFadingOut &&
                       e.HasKeyword(Keywords.MonsterKeyword.Taunt))
            .ToList();
    }
}

/// <summary>
/// Class to store the result of applying attack rules
/// </summary>
public class AttackRuleResult
{
    public float ModifiedAttackerDamage { get; set; }
    public float ModifiedTargetDamage { get; set; }
    public bool ShouldTakeCounterDamage { get; set; }
    public string RuleDescription { get; set; }

    // Properties for Overwhelm splash damage
    public bool HasSplashDamage { get; set; } = false;
    public float SplashDamageAmount { get; set; } = 0f;
    public bool IsTargetEnemy { get; set; } = false;
    public EntityManager TargetEntity { get; set; } = null;
    public EntityManager SourceAttacker { get; set; } = null;
}
