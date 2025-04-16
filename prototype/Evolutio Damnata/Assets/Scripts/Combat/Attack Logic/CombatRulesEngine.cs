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

        // Apply target's defensive keywords - target takes less damage when Tough
        ApplyToughRuleForDefender(target, result);

        // Apply attacker's defensive keywords - attacker takes less counter damage when Tough
        ApplyToughRuleForAttacker(attacker, result);

        // Add future rules here
        // ApplyTauntRule(target, result);
        // ApplyLifestealRule(attacker, result);

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
}
