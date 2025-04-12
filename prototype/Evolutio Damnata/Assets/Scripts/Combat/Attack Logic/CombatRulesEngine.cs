using System.Collections.Generic;
using UnityEngine;

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
