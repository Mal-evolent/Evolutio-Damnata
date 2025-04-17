using UnityEngine;
using System.Collections.Generic;

public class AttackHandler : IAttackHandler
{
    private readonly AttackLimiter _attackLimiter;
    private readonly ICombatRulesEngine _rulesEngine;
    private SpritePositioning _spritePositioning;

    public AttackHandler(AttackLimiter attackLimiter, ICombatRulesEngine rulesEngine)
    {
        _attackLimiter = attackLimiter;
        _rulesEngine = rulesEngine ?? new CombatRulesEngine();
        _spritePositioning = GameObject.FindObjectOfType<SpritePositioning>();
    }

    public void HandleAttack(EntityManager attacker, EntityManager target)
    {
        if (attacker == null || target == null)
        {
            Debug.LogError("One of the entities is null!");
            return;
        }

        // Check if either entity is dead or fading out
        if (attacker.dead || attacker.IsFadingOut)
        {
            Debug.LogWarning($"Cannot attack with {attacker.name}: entity is dead or fading out.");
            return;
        }

        if (target.dead || target.IsFadingOut)
        {
            Debug.LogWarning($"Cannot attack {target.name}: entity is dead or fading out.");
            return;
        }

        if (!CanAttack(attacker))
        {
            Debug.LogWarning($"{attacker.name} cannot attack anymore this turn.");
            return;
        }

        // Calculate base damage values
        float attackerDamage = attacker.GetAttackDamage();
        float targetDamage = target.GetAttackDamage();

        // Apply rules that might modify damage or attack behavior
        AttackRuleResult ruleResult = _rulesEngine.ApplyRules(attacker, target, attackerDamage, targetDamage);

        // Set potential killers before taking damage
        attacker.SetKilledBy(target);
        target.SetKilledBy(attacker);

        // Apply modified damage based on rules
        target.TakeDamage(ruleResult.ModifiedAttackerDamage);

        // Handle counter damage based on rules
        if (ruleResult.ShouldTakeCounterDamage)
        {
            // Normal attack - take full counter damage
            attacker.TakeDamage(ruleResult.ModifiedTargetDamage);
        }
        else
        {
            // Ranged attack - show 0 damage but don't actually take damage
            attacker.ShowDamageNumber(0);
        }

        // Apply splash damage if the attack has the Overwhelm keyword
        if (ruleResult.HasSplashDamage)
        {
            // Get the appropriate entities list based on target type
            List<GameObject> entitiesOnTargetSide = GetEntitiesOnSameSide(target);
            if (entitiesOnTargetSide != null && entitiesOnTargetSide.Count > 0)
            {
                // Apply splash damage to all other entities on target's side
                _rulesEngine.ApplySplashDamage(ruleResult, entitiesOnTargetSide);
            }
            else
            {
                Debug.LogWarning($"[AttackHandler] No entities found on the same side as {target.name} for splash damage");
            }
        }

        // Record the attack in the CardHistory
        if (CardHistory.Instance != null)
        {
            // Find the current turn number from any available CombatManager
            int turnNumber = 0;
            var combatManager = GameObject.FindObjectOfType<CombatManager>();
            if (combatManager != null)
            {
                turnNumber = combatManager.TurnCount;
            }

            CardHistory.Instance.RecordAttack(
                attacker,
                target,
                turnNumber,
                ruleResult.ModifiedAttackerDamage,
                ruleResult.ShouldTakeCounterDamage ? ruleResult.ModifiedTargetDamage : 0,
                !ruleResult.ShouldTakeCounterDamage
            );
        }
        else
        {
            Debug.LogWarning("[AttackHandler] CardHistory.Instance is null, cannot record attack");
        }

        _attackLimiter.RegisterAttack(attacker);

        // Log the attack results
        string attackerDamageText = ruleResult.ShouldTakeCounterDamage ?
            $"{attacker.name} took {ruleResult.ModifiedTargetDamage} damage." :
            $"{attacker.name} avoided counter damage ({ruleResult.RuleDescription}).";

        Debug.Log($"{attacker.name} attacked {target.name}. {attackerDamageText} {target.name} took {ruleResult.ModifiedAttackerDamage} damage.");
    }

    private List<GameObject> GetEntitiesOnSameSide(EntityManager target)
    {
        if (_spritePositioning == null)
        {
            _spritePositioning = GameObject.FindObjectOfType<SpritePositioning>();
            if (_spritePositioning == null)
            {
                Debug.LogError("[AttackHandler] Could not find SpritePositioning component");
                return null;
            }
        }

        // Determine which list to use based on the target's type
        EntityManager.MonsterType targetType = target.GetMonsterType();
        return targetType == EntityManager.MonsterType.Enemy ?
               _spritePositioning.EnemyEntities :
               _spritePositioning.PlayerEntities;
    }

    public bool CanAttack(EntityManager entity)
    {
        return _attackLimiter.CanAttack(entity);
    }
}
