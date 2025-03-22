using UnityEngine;
using System.Collections.Generic;

/*
 * Limits the attacks of an entity based on the number of allowed attacks.
 */

public class AttackLimiter
{
    private Dictionary<EntityManager, int> attackCounts = new Dictionary<EntityManager, int>();
    private Dictionary<EntityManager, int> maxAttacks = new Dictionary<EntityManager, int>();

    public void RegisterEntity(EntityManager entity, int allowedAttacks)
    {
        if (!attackCounts.ContainsKey(entity))
        {
            attackCounts[entity] = 0;
            maxAttacks[entity] = allowedAttacks;
            Debug.Log($"Registered entity {entity.name} with {allowedAttacks} allowed attacks.");
        }
    }

    public bool CanAttack(EntityManager entity)
    {
        if (attackCounts.ContainsKey(entity))
        {
            return attackCounts[entity] < maxAttacks[entity];
        }
        return false;
    }

    public void RegisterAttack(EntityManager entity)
    {
        if (attackCounts.ContainsKey(entity))
        {
            attackCounts[entity]++;
        }
    }

    public void ModifyAllowedAttacks(EntityManager entity, int newAllowedAttacks)
    {
        if (maxAttacks.ContainsKey(entity))
        {
            maxAttacks[entity] = newAllowedAttacks;
        }
    }

    public void ResetAttacks(EntityManager entity)
    {
        Debug.LogWarning("function reached");
        if (attackCounts.ContainsKey(entity))
        {
            Debug.LogWarning("if statement reached");
            attackCounts[entity] = 0;
            Debug.Log($"Reset attacks for {entity.name}. Attack count is now {attackCounts[entity]}.");
        }
        else
        {
            Debug.LogWarning($"Entity {entity.name} not found in attackCounts.");
        }
    }
}
