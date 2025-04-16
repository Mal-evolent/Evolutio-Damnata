using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Interface for combat rules engines
/// </summary>
public interface ICombatRulesEngine
{
    AttackRuleResult ApplyRules(EntityManager attacker, EntityManager target, float baseAttackerDamage, float baseTargetDamage);
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

        // Apply target's defensive keywords - target takes less damage when Tough
        ApplyToughRuleForDefender(target, result);

        // Apply attacker's defensive keywords - attacker takes less counter damage when Tough
        ApplyToughRuleForAttacker(attacker, result);

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
    /// Tough defenders take half damage from attacks
    /// </summary>
    private void ApplyToughRuleForDefender(EntityManager target, AttackRuleResult result)
    {
        if (target != null && target.HasKeyword(Keywords.MonsterKeyword.Tough))
        {
            result.ModifiedAttackerDamage = Mathf.Floor(result.ModifiedAttackerDamage / 2f);

            string currentDescription = result.RuleDescription;
            result.RuleDescription = string.IsNullOrEmpty(currentDescription) || currentDescription == "Normal Attack" ?
                "Defender Tough" : $"{currentDescription}, Defender Tough";

            Debug.Log($"[CombatRulesEngine] {target.name} is tough and reduces incoming damage by half!");
        }
    }

    /// <summary>
    /// Tough attackers take half damage from counter-attacks
    /// </summary>
    private void ApplyToughRuleForAttacker(EntityManager attacker, AttackRuleResult result)
    {
        if (attacker != null && attacker.HasKeyword(Keywords.MonsterKeyword.Tough) && result.ShouldTakeCounterDamage)
        {
            result.ModifiedTargetDamage = Mathf.Floor(result.ModifiedTargetDamage / 2f);

            string currentDescription = result.RuleDescription;
            result.RuleDescription = string.IsNullOrEmpty(currentDescription) || currentDescription == "Normal Attack" ?
                "Attacker Tough" : $"{currentDescription}, Attacker Tough";

            Debug.Log($"[CombatRulesEngine] {attacker.name} is tough and reduces incoming counter damage by half!");
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

            string currentDescription = result.RuleDescription;
            result.RuleDescription = string.IsNullOrEmpty(currentDescription) || currentDescription == "Normal Attack" ?
                "Overwhelm" : $"{currentDescription}, Overwhelm";

            Debug.Log($"[CombatRulesEngine] {attacker.name} attacks with Overwhelm, causing {splashDamage} splash damage to all other units!");
        }
    }

    /// <summary>
    /// Applies splash damage to all active entities from an Overwhelm attack
    /// </summary>
    public static void ApplySplashDamage(AttackRuleResult result)
    {
        if (!result.HasSplashDamage || result.SplashDamageAmount <= 0 || result.TargetEntity == null)
            return;

        // Get the appropriate SpritePositioning reference
        var spritePositioning = Object.FindObjectOfType<SpritePositioning>();
        if (spritePositioning == null)
        {
            var combatStage = Object.FindObjectOfType<CombatStage>();
            if (combatStage != null)
            {
                spritePositioning = combatStage.SpritePositioning as SpritePositioning;
            }
        }

        if (spritePositioning == null)
        {
            Debug.LogError("[CombatRulesEngine] Cannot apply splash damage - SpritePositioning not found");
            return;
        }

        // Get the appropriate entity list based on target side
        var entityList = result.IsTargetEnemy ? spritePositioning.EnemyEntities : spritePositioning.PlayerEntities;
        if (entityList == null || entityList.Count == 0)
            return;

        // Find all valid entities that are NOT the target
        List<EntityManager> otherEntities = new List<EntityManager>();

        foreach (var entity in entityList)
        {
            if (entity == null) continue;

            var entityManager = entity.GetComponent<EntityManager>();
            if (entityManager == null) continue;

            // Skip the original target
            if (entityManager == result.TargetEntity) continue;

            // Only include active entities
            if (entityManager.placed && !entityManager.dead && !entityManager.IsFadingOut)
            {
                otherEntities.Add(entityManager);
            }
        }

        // Apply splash damage to all other active entities
        foreach (var entity in otherEntities)
        {
            entity.TakeDamage(result.SplashDamageAmount);
            Debug.Log($"[CombatRulesEngine] Overwhelm splash damage: {entity.name} takes {result.SplashDamageAmount} damage!");
        }
    }

    /// <summary>
    /// Checks if there are any taunt units on the field that should be targeted first
    /// </summary>
    public static bool HasTauntUnits(System.Collections.Generic.List<GameObject> entities)
    {
        return entities.Any(entity =>
            entity != null &&
            entity.GetComponent<EntityManager>()?.HasKeyword(Keywords.MonsterKeyword.Taunt) == true &&
            !entity.GetComponent<EntityManager>().dead &&
            !entity.GetComponent<EntityManager>().IsFadingOut);
    }

    /// <summary>
    /// Gets all taunt units from a list of entities
    /// </summary>
    public static System.Collections.Generic.List<EntityManager> GetAllTauntUnits(System.Collections.Generic.List<GameObject> entities)
    {
        return entities
            .Where(entity => entity != null)
            .Select(entity => entity.GetComponent<EntityManager>())
            .Where(entity => entity != null &&
                  entity.HasKeyword(Keywords.MonsterKeyword.Taunt) &&
                  !entity.dead &&
                  !entity.IsFadingOut)
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
}
