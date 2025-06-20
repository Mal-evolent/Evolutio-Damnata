using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles entity cleanup when combat ends by listening to OnEnemyDefeated events
/// </summary>
public class EntityCombatResetHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool killAllEntitiesOnEnemyDefeated = true;
    [SerializeField] private bool includeEnemyEntities = true;
    [SerializeField] private bool includeFriendlyEntities = true;
    
    [Header("References")]
    [SerializeField] private CombatManager combatManager;
    
    private List<EntityManager> _activeEntities = new List<EntityManager>();
    
    private void Awake()
    {
        // Find combat manager if not assigned
        if (combatManager == null)
        {
            combatManager = FindObjectOfType<CombatManager>();
            if (combatManager == null)
            {
                Debug.LogWarning("[EntityCombatResetHandler] CombatManager not found in scene. Handler will not function.");
                enabled = false;
                return;
            }
        }
    }
    
    private void OnEnable()
    {
        if (combatManager != null)
        {
            combatManager.OnEnemyDefeated += HandleEnemyDefeated;
            Debug.Log("[EntityCombatResetHandler] Subscribed to OnEnemyDefeated event");
        }
    }
    
    private void OnDisable()
    {
        if (combatManager != null)
        {
            combatManager.OnEnemyDefeated -= HandleEnemyDefeated;
            Debug.Log("[EntityCombatResetHandler] Unsubscribed from OnEnemyDefeated event");
        }
    }
    
    /// <summary>
    /// Register an entity to be tracked for cleanup
    /// </summary>
    public void RegisterEntity(EntityManager entity)
    {
        if (entity != null && !_activeEntities.Contains(entity))
        {
            _activeEntities.Add(entity);
        }
    }
    
    /// <summary>
    /// Unregister an entity from tracking
    /// </summary>
    public void UnregisterEntity(EntityManager entity)
    {
        if (entity != null)
        {
            _activeEntities.Remove(entity);
        }
    }
    
    /// <summary>
    /// Handles the enemy defeated event by fading out all registered entities
    /// </summary>
    private void HandleEnemyDefeated()
    {
        if (!killAllEntitiesOnEnemyDefeated)
            return;
            
        Debug.Log("[EntityCombatResetHandler] Enemy defeated, cleaning up entities");
        
        // Create a copy of the list to avoid modification issues during iteration
        var entitiesToProcess = new List<EntityManager>(_activeEntities);
        
        foreach (var entity in entitiesToProcess)
        {
            if (entity == null || entity.dead || entity.IsFadingOut)
                continue;
                
            if (!ShouldProcessEntity(entity))
                continue;
                
            // Kill the entity if it's not already dead or fading
            KillEntity(entity);
        }
        
        // Clear the list after processing
        _activeEntities.Clear();
    }
    
    private bool ShouldProcessEntity(EntityManager entity)
    {
        if (entity == null)
            return false;
            
        var monsterType = entity.GetMonsterType();
        
        if (monsterType == EntityManager.MonsterType.Enemy && !includeEnemyEntities)
            return false;
            
        if (monsterType == EntityManager.MonsterType.Friendly && !includeFriendlyEntities)
            return false;
            
        return true;
    }
    
    private void KillEntity(EntityManager entity)
    {
        if (entity == null || entity.dead || entity.IsFadingOut)
            return;
            
        Debug.Log($"[EntityCombatResetHandler] Killing entity: {entity.name}");
        
        // Use silent kill instead of TakeDamage to avoid damage numbers
        entity.SilentKill();
    }
    
    /// <summary>
    /// Manually trigger cleanup of all registered entities
    /// </summary>
    public void CleanupAllEntities()
    {
        HandleEnemyDefeated();
    }
}