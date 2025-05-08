using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Managers;
using EnemyInteraction.Models;

public class AttackStrategyExecutor
{
    private readonly IAttackExecutor _attackExecutor;
    private readonly IEntityCacheManager _entityCacheManager;
    private readonly IAttackStrategyManager _attackStrategyManager;

    // Timing and delay parameters
    private readonly float _evaluationDelay;

    // Human error simulation parameters
    private readonly float _missedAttackChance;
    private readonly float _strategyChangeChance;
    private readonly float _targetReconsiderationChance;
    private readonly float _reconsiderationDelay;
    private readonly float _attackOrderRandomizationChance;

    public AttackStrategyExecutor(
        IAttackExecutor attackExecutor,
        IEntityCacheManager entityCacheManager,
        IAttackStrategyManager attackStrategyManager,
        float evaluationDelay,
        float missedAttackChance,
        float strategyChangeChance,
        float targetReconsiderationChance,
        float reconsiderationDelay,
        float attackOrderRandomizationChance)
    {
        _attackExecutor = attackExecutor;
        _entityCacheManager = entityCacheManager;
        _attackStrategyManager = attackStrategyManager;
        _evaluationDelay = evaluationDelay;
        _missedAttackChance = missedAttackChance;
        _strategyChangeChance = strategyChangeChance;
        _targetReconsiderationChance = targetReconsiderationChance;
        _reconsiderationDelay = reconsiderationDelay;
        _attackOrderRandomizationChance = attackOrderRandomizationChance;
    }

    public IEnumerator ExecuteAttackSequence(AttackContext context)
    {
        // Delay to simulate "thinking" about attack strategy
        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay));

        // Get attack order
        var attackOrder = GetOptimizedAttackOrder(context);

        // Determine initial strategy
        StrategicMode mode = _attackStrategyManager.DetermineStrategicMode(context.BoardState);
        Debug.Log($"[AttackExecutor] Current strategy: {mode}");

        // Brief pause after determining strategy before first attack
        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.5f));

        // Process each attack with appropriate delays
        foreach (var attacker in attackOrder)
        {
            yield return ProcessSingleAttack(attacker, mode, context);

            // Update entity lists after attack in case targets were destroyed
            context.PlayerEntities = _entityCacheManager.CachedPlayerEntities;

            // Check if we should continue
            if (context.PlayerEntities.Count == 0 && context.PlayerHealthIcon == null)
                break;
        }

        // Final delay after attack sequence completes
        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay));
    }

    private List<EntityManager> GetOptimizedAttackOrder(AttackContext context)
    {
        var attackOrder = _attackStrategyManager.GetAttackOrder(
            context.EnemyEntities,
            context.PlayerEntities,
            context.PlayerHealthIcon,
            context.BoardState);

        // Apply attack order randomization based on chance (human error simulation)
        if (Random.value < _attackOrderRandomizationChance && attackOrder.Count > 1)
        {
            Debug.Log("[AttackExecutor] Applying attack order randomization (simulating human error)");
            attackOrder = RandomizeAttackOrder(attackOrder);
        }

        return attackOrder;
    }

    private IEnumerator ProcessSingleAttack(EntityManager attacker, StrategicMode initialMode, AttackContext context)
    {
        // Simulate "forgetting" to attack with this entity
        if (Random.value < _missedAttackChance)
        {
            Debug.Log($"[AttackExecutor] 'Forgot' to attack with {attacker.name} (simulating human error)");
            yield break;
        }

        // Determine if we should change strategy mid-turn
        StrategicMode currentMode = DetermineCurrentStrategy(initialMode);

        // Delay before selecting target - simulates AI "thinking"
        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.7f));

        // Select target and perform attack
        yield return SelectAndAttackTarget(attacker, currentMode, context);
    }

    private StrategicMode DetermineCurrentStrategy(StrategicMode initialMode)
    {
        // Check if we should change strategy mid-turn
        if (Random.value < _strategyChangeChance)
        {
            StrategicMode newMode = initialMode == StrategicMode.Aggro ? StrategicMode.Defensive : StrategicMode.Aggro;
            Debug.Log($"[AttackExecutor] Changed strategy mid-turn to: {newMode} (simulating human error)");
            return newMode;
        }

        return initialMode;
    }

    private IEnumerator SelectAndAttackTarget(EntityManager attacker, StrategicMode mode, AttackContext context)
    {
        // Select target
        EntityManager targetEntity = _attackStrategyManager.SelectTarget(
            attacker,
            context.PlayerEntities,
            context.PlayerHealthIcon,
            context.BoardState,
            mode);

        // Handle target reconsideration
        if (targetEntity != null && Random.value < _targetReconsiderationChance)
        {
            yield return SimulateTargetReconsideration();
            targetEntity = ReconsiderTarget(targetEntity, context);
        }

        // Execute appropriate attack based on target
        if (targetEntity != null)
        {
            yield return ExecuteEntityAttack(attacker, targetEntity);
        }
        else if (context.PlayerHealthIcon != null &&
                _attackStrategyManager.ShouldAttackHealthIcon(attacker, context.PlayerEntities, context.PlayerHealthIcon, context.BoardState))
        {
            yield return ExecuteHealthIconAttack(attacker, context.PlayerHealthIcon);
        }
    }

    private IEnumerator SimulateTargetReconsideration()
    {
        Debug.Log($"[AttackExecutor] Reconsidering target selection (simulating human error)");
        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_reconsiderationDelay));
    }

    private EntityManager ReconsiderTarget(EntityManager targetEntity, AttackContext context)
    {
        // 50% chance to actually change the target
        if (Random.value < 0.5f && context.PlayerEntities.Count > 1)
        {
            int currentIndex = context.PlayerEntities.IndexOf(targetEntity);
            int newIndex = (currentIndex + 1) % context.PlayerEntities.Count;
            EntityManager newTarget = context.PlayerEntities[newIndex];
            Debug.Log($"[AttackExecutor] Changed target to {newTarget.name}");
            return newTarget;
        }

        return targetEntity;
    }

    private IEnumerator ExecuteEntityAttack(EntityManager attacker, EntityManager target)
    {
        // Small delay between target selection and attack execution
        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.3f));

        yield return _attackExecutor.ExecuteAttack(attacker, target);

        // Handle entity cache refresh without using yield return
        RefreshEntityCacheAfterAttack();

        if (target.dead)
        {
            // Add longer pause after killing a unit to emphasize the moment
            yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 1.2f));

            _entityCacheManager.RefreshEntityCaches();
        }
        else
        {
            // Normal post-attack delay
            yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay));
        }
    }

    private IEnumerator ExecuteHealthIconAttack(EntityManager attacker, HealthIconManager playerHealthIcon)
    {
        // Dramatic pause before attacking health icon directly
        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 0.5f));

        _attackExecutor.AttackPlayerHealthIcon(attacker, playerHealthIcon);

        // Handle entity cache refresh without using yield return
        RefreshEntityCacheAfterAttack();

        // Longer pause after attacking health icon to emphasize importance
        yield return new WaitForSeconds(_attackExecutor.GetRandomizedDelay(_evaluationDelay * 1.5f));
    }

    // Non-iterator helper method to handle entity cache refresh
    private void RefreshEntityCacheAfterAttack()
    {
        // Get the actual EntityCacheManager implementation
        var entityCacheManager = _entityCacheManager as EntityCacheManager;

        // Call RefreshAfterAction directly if available
        if (entityCacheManager != null)
        {
            entityCacheManager.RefreshAfterAction();
        }
        else
        {
            // Fallback to just refreshing caches if we can't cast to concrete implementation
            _entityCacheManager.RefreshEntityCaches();
        }
    }

    public List<EntityManager> RandomizeAttackOrder(List<EntityManager> originalOrder)
    {
        List<EntityManager> randomizedOrder = new List<EntityManager>(originalOrder);

        // Simple shuffling algorithm
        int n = randomizedOrder.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            EntityManager temp = randomizedOrder[k];
            randomizedOrder[k] = randomizedOrder[n];
            randomizedOrder[n] = temp;
        }

        return randomizedOrder;
    }
}
