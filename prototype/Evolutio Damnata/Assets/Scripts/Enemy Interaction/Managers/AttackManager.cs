using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Models;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;

namespace EnemyInteraction.Managers
{
    public class AttackManager : MonoBehaviour, IAttackManager
    {
        [SerializeField] private SpritePositioning _spritePositioning;
        private ICombatManager _combatManager;
        private CombatStage _combatStage;
        private IKeywordEvaluator _keywordEvaluator;
        private IBoardStateManager _boardStateManager;
        private AttackLimiter _attackLimiter;

        private void Awake()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[AttackManager] Starting initialization...");
            
            // First, wait for scene essentials
            int maxAttempts = 30;
            int attempts = 0;
            
            // Find critical scene components first
            while (attempts < maxAttempts)
            {
                _combatManager = _combatManager ?? FindObjectOfType<CombatManager>();
                _combatStage = _combatStage ?? FindObjectOfType<CombatStage>();
                
                if (_combatManager != null && _combatStage != null)
                    break;
                    
                Debug.Log("[AttackManager] Searching for scene components...");
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
            
            if (_combatManager == null)
            {
                Debug.LogError("[AttackManager] Failed to find CombatManager in scene!");
            }
            
            if (_combatStage == null)
            {
                Debug.LogError("[AttackManager] Failed to find CombatStage in scene!");
            }
            
            // If we've found CombatStage, wait for its initialization
            if (_combatStage != null)
            {
                attempts = 0;
                while (_combatStage.SpritePositioning == null && attempts < maxAttempts)
                {
                    Debug.Log("[AttackManager] Waiting for CombatStage.SpritePositioning...");
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }
                
                // Try to get SpritePositioning from CombatStage if not set in inspector
                if (_spritePositioning == null && _combatStage.SpritePositioning != null)
                {
                    _spritePositioning = _combatStage.SpritePositioning as SpritePositioning;
                    Debug.Log("[AttackManager] Got SpritePositioning from CombatStage");
                }
                
                // Get AttackLimiter from CombatStage
                if (_attackLimiter == null && _combatStage != null)
                {
                    _attackLimiter = _combatStage.GetAttackLimiter();
                    if (_attackLimiter != null)
                    {
                        Debug.Log("[AttackManager] Got AttackLimiter from CombatStage");
                    }
                }
            }
            
            // Wait for AIServices to be ready (optional)
            attempts = 0;
            while (AIServices.Instance == null && attempts < maxAttempts)
            {
                Debug.Log("[AttackManager] Waiting for AIServices to be initialized...");
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
            
            // Get dependencies from AIServices if possible
            if (AIServices.Instance != null)
            {
                var services = AIServices.Instance;
                
                if (_keywordEvaluator == null)
                    _keywordEvaluator = services.KeywordEvaluator;
                    
                if (_boardStateManager == null)
                    _boardStateManager = services.BoardStateManager;
                    
                Debug.Log("[AttackManager] Tried to get services from AIServices");
            }
            
            // Create any missing services locally if needed
            if (_keywordEvaluator == null)
            {
                var keywordEvaluatorObj = new GameObject("KeywordEvaluator_Local");
                keywordEvaluatorObj.transform.SetParent(transform);
                _keywordEvaluator = keywordEvaluatorObj.AddComponent<KeywordEvaluator>();
                Debug.Log("[AttackManager] Created local KeywordEvaluator");
            }
            
            if (_boardStateManager == null)
            {
                var boardStateManagerObj = new GameObject("BoardStateManager_Local");
                boardStateManagerObj.transform.SetParent(transform);
                _boardStateManager = boardStateManagerObj.AddComponent<BoardStateManager>();
                Debug.Log("[AttackManager] Created local BoardStateManager");
            }
            
            // If we still don't have SpritePositioning, try to create a minimal one
            if (_spritePositioning == null)
            {
                Debug.LogWarning("[AttackManager] Unable to get SpritePositioning from scene, functionality will be limited");
            }

            Debug.Log("[AttackManager] Initialization completed - some dependencies may be missing but we'll handle it gracefully");
        }

        private bool ValidateReferences()
        {
            bool valid = true;
            
            if (_keywordEvaluator == null)
            {
                Debug.LogError("[AttackManager] KeywordEvaluator is null!");
                valid = false;
            }
            
            if (_boardStateManager == null)
            {
                Debug.LogError("[AttackManager] BoardStateManager is null!");
                valid = false;
            }
            
            if (_combatManager == null)
            {
                Debug.LogError("[AttackManager] CombatManager is null!");
                valid = false;
            }
            
            if (_combatStage == null)
            {
                Debug.LogError("[AttackManager] CombatStage is null!");
                valid = false;
            }
            
            if (_spritePositioning == null)
            {
                Debug.LogError("[AttackManager] SpritePositioning is null!");
                valid = false;
            }
            
            return valid;
        }

        public IEnumerator Attack()
        {
            Debug.Log("[AttackManager] Starting Attack");
            
            // Validate that the required dependencies are available
            if (_combatManager == null || _spritePositioning == null)
            {
                Debug.LogWarning("[AttackManager] Required components are null in Attack! Using a placeholder implementation.");
                
                // Simple placeholder implementation that doesn't depend on any components
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[AttackManager] Simulating enemy attacks (placeholder)");
                yield return new WaitForSeconds(0.5f);
                
                Debug.Log("[AttackManager] Attack completed (placeholder)");
                yield break;
            }
            
            // Make sure we have an AttackLimiter
            if (_attackLimiter == null && _combatStage != null)
            {
                _attackLimiter = _combatStage.GetAttackLimiter();
                if (_attackLimiter == null)
                {
                    Debug.LogError("[AttackManager] Cannot find AttackLimiter, creating a new one");
                    _attackLimiter = new AttackLimiter();
                }
            }
            
            // Validate that we're in the correct phase
            if (!ValidateCombatState())
            {
                Debug.LogWarning("[AttackManager] Not in enemy combat phase, skipping attacks");
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[AttackManager] Attack skipped - not in combat phase");
                yield break;
            }
            
            // Declare variables we'll use inside and outside the try block
            List<EntityManager> enemyEntities = null;
            List<EntityManager> playerEntities = null;
            HealthIconManager playerHealthIcon = null;
            bool hasEnemyEntities = false;
            bool hasPlayerEntities = false;
            bool hasTargets = false;
            bool errorOccurred = false;
            
            try
            {
                // Check if enemy entities are present using our helper method
                hasEnemyEntities = HasEntitiesOnField(false);
                
                if (hasEnemyEntities)
                {
                    // Get all enemy entities that can attack
                    enemyEntities = _spritePositioning.EnemyEntities
                        .Where(entity => entity != null)
                        .Select(entity => entity.GetComponent<EntityManager>())
                        .Where(entity => entity != null && entity.placed && !entity.dead && !entity.IsFadingOut && 
                              (_attackLimiter != null ? _attackLimiter.CanAttack(entity) : !entity.HasAttacked))
                        .ToList();
                        
                    hasEnemyEntities = enemyEntities != null && enemyEntities.Count > 0;
                }
                
                // Check if player entities are present using our helper method
                hasPlayerEntities = HasEntitiesOnField(true);
                
                if (hasPlayerEntities)
                {
                    // Get all potential player entity targets and filter out any that are dead or null
                    playerEntities = _spritePositioning.PlayerEntities
                        .Where(entity => entity != null)
                        .Select(entity => entity.GetComponent<EntityManager>())
                        .Where(entity => entity != null && entity.placed && !entity.dead && !entity.IsFadingOut)
                        .ToList();
                    
                    // Update hasPlayerEntities based on the actual filtered list
                    hasPlayerEntities = playerEntities != null && playerEntities.Count > 0;
                }
                
                // If after filtering we have no player entities, check if the health icon is available
                if (!hasPlayerEntities)
                {
                    // Get player health icon as a potential target
                    playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
                    hasTargets = playerHealthIcon != null;
                    
                    if (hasTargets)
                    {
                        Debug.Log("[AttackManager] No valid player entities, targeting health icon");
                    }
                }
                else
                {
                    hasTargets = true;
                    
                    // Also get the health icon reference even though we'll attack entities first
                    playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
                }
                
                if (!hasEnemyEntities)
                {
                    Debug.Log("[AttackManager] No enemy entities available to attack");
                }
                else if (!hasTargets)
                {
                    Debug.Log("[AttackManager] No valid targets available for enemy attacks");
                }
                else
                {
                    int playerEntityCount = playerEntities?.Count ?? 0;
                    Debug.Log($"[AttackManager] Enemy has {enemyEntities.Count} entities that can attack, against {playerEntityCount} player entities");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AttackManager] Error in Attack: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }
            
            // If we encountered an error or have no entities or no targets, just exit with a small delay
            if (errorOccurred || !hasEnemyEntities || !hasTargets)
            {
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[AttackManager] Attack completed - no action taken");
                yield break;
            }
            
            // Get current board state for evaluation
            BoardState boardState = null;
            if (_boardStateManager != null)
            {
                boardState = _boardStateManager.EvaluateBoardState();
            }
            else
            {
                // Create a simple board state if no manager available
                boardState = new BoardState
                {
                    EnemyHealth = _combatManager.EnemyHealth,
                    PlayerHealth = _combatManager.PlayerHealth,
                    TurnCount = _combatManager.TurnCount
                };
            }
            
            // Final validation before proceeding with attacks
            if (enemyEntities == null || enemyEntities.Count == 0)
            {
                Debug.LogWarning("[AttackManager] No enemy entities available for attacking after initialization");
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[AttackManager] Attack completed - no action taken");
                yield break;
            }
            
            // Process each entity's attack
            foreach (var attacker in enemyEntities)
            {
                yield return new WaitForSeconds(0.3f); // Add delay for visual effect
                
                // Check for taunt units - must attack these first
                bool hasTaunts = false;
                if (playerEntities != null && playerEntities.Count > 0)
                {
                    hasTaunts = playerEntities.Any(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt));
                }
                
                // Select target based on combat rules and AI strategy
                EntityManager targetEntity = null;
                
                if (hasTaunts)
                {
                    // Must attack taunt units first
                    var tauntTargets = playerEntities.Where(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt)).ToList();
                    targetEntity = SelectBestTarget(attacker, tauntTargets, boardState);
                    Debug.Log($"[AttackManager] {attacker.name} must attack taunt unit {targetEntity?.name ?? "unknown"}");
                }
                else
                {
                    // Decide between attacking a player entity or going for the health icon
                    bool attackHealthIcon = ShouldAttackHealthIcon(attacker, playerEntities, playerHealthIcon, boardState);
                    
                    if (attackHealthIcon && playerHealthIcon != null)
                    {
                        // Use our dedicated method for health icon attacks
                        AttackPlayerHealthIcon(attacker, playerHealthIcon);
                        continue; // Skip to next attacker
                    }
                    else if (playerEntities != null && playerEntities.Count > 0)
                    {
                        // Attack a player entity
                        targetEntity = SelectBestTarget(attacker, playerEntities, boardState);
                        Debug.Log($"[AttackManager] {attacker.name} targeting {targetEntity?.name ?? "unknown"}");
                    }
                    else
                    {
                        Debug.LogWarning($"[AttackManager] {attacker.name} has no valid targets to attack");
                        continue; // Skip to next attacker
                    }
                }
                
                // Perform the attack if we have a valid target
                if (targetEntity != null && _combatStage != null)
                {
                    _combatStage.HandleMonsterAttack(attacker, targetEntity);
                    // Register the attack with the AttackLimiter
                    if (_attackLimiter != null)
                    {
                        _attackLimiter.RegisterAttack(attacker);
                    }
                    else
                    {
                        attacker.HasAttacked = true;
                    }
                    Debug.Log($"[AttackManager] {attacker.name} successfully attacked {targetEntity.name}");
                    
                    // Re-evaluate board state after significant changes
                    if (targetEntity.dead && _boardStateManager != null)
                    {
                        boardState = _boardStateManager.EvaluateBoardState();
                        
                        // Refresh our player entities list to remove dead or dying entities
                        playerEntities = RefreshPlayerEntities(playerEntities);
                        
                        // Check if we have any valid targets left
                        hasPlayerEntities = playerEntities.Count > 0;
                        hasTargets = hasPlayerEntities || playerHealthIcon != null;
                        
                        // If no more targets, exit early
                        if (!hasTargets)
                        {
                            Debug.Log("[AttackManager] No more targets available, ending attack phase");
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[AttackManager] {attacker.name} could not attack - no valid target or CombatStage is null");
                }
            }
            
            // Additional delay at the end
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[AttackManager] Attack completed successfully");
        }
        
        /// <summary>
        /// Checks if there are any active entities on the specified side
        /// </summary>
        /// <param name="isPlayerSide">True to check player side, false to check enemy side</param>
        /// <returns>True if entities are present on the field</returns>
        private bool HasEntitiesOnField(bool isPlayerSide)
        {
            if (_spritePositioning == null)
                return false;
                
            var entities = isPlayerSide ? _spritePositioning.PlayerEntities : _spritePositioning.EnemyEntities;
            
            foreach (var entity in entities)
            {
                var entityManager = entity?.GetComponent<EntityManager>();
                if (entityManager != null && entityManager.placed && !entityManager.dead && !entityManager.IsFadingOut)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private bool ShouldAttackHealthIcon(EntityManager attacker, List<EntityManager> playerEntities, HealthIconManager playerHealthIcon, BoardState boardState)
        {
            if (attacker == null)
            {
                Debug.LogError("[AttackManager] Cannot decide attack target: attacker is null");
                return false;
            }
            
            if (playerHealthIcon == null)
            {
                Debug.LogWarning("[AttackManager] Cannot attack health icon: health icon is null");
                return false;
            }
            
            // Check if player has any active entities on the field using the helper method
            bool playerEntitiesPresent = HasEntitiesOnField(true);
            
            // Alternative check if the helper method failed or returned inconsistent results
            if (!playerEntitiesPresent && playerEntities != null && playerEntities.Count > 0)
            {
                // Double-check with the actual list if there are any valid entities
                playerEntitiesPresent = playerEntities.Any(e => e != null && !e.dead && !e.IsFadingOut);
            }
            
            // Always prioritize attacking player monsters if they exist
            if (playerEntitiesPresent)
            {
                // Never attack the health icon directly if player has monsters on the field
                return false;
            }
            
            // If no player entities exist, we must attack the health icon
            Debug.Log("[AttackManager] No player entities on field, attacking health icon directly");
            return true;
        }

        private bool ValidateCombatState()
        {
            if (_combatManager == null)
            {
                Debug.LogError("[AttackManager] CombatManager reference is missing!");
                return false;
            }

            if (!_combatManager.IsEnemyCombatPhase())
            {
                Debug.LogWarning("[AttackManager] Cannot attack in current combat state - Not in EnemyCombat phase");
                return false;
            }

            return true;
        }

        private EntityManager SelectBestTarget(EntityManager attacker, List<EntityManager> targets, BoardState boardState)
        {
            if (attacker == null)
            {
                Debug.LogError("[AttackManager] Cannot select target: attacker is null");
                return null;
            }
            
            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning($"[AttackManager] No valid targets available for {attacker.name}");
                return null;
            }

            // Filter out any null, dead, or fading entities
            var validTargets = targets.Where(t => t != null && !t.dead && !t.IsFadingOut).ToList();
            
            if (validTargets.Count == 0)
            {
                Debug.LogWarning($"[AttackManager] All targets were null or dead for {attacker.name}");
                return null;
            }
            
            // If only one target, return it immediately
            if (validTargets.Count == 1)
            {
                return validTargets[0];
            }

            EntityManager bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var target in validTargets)
            {
                try
                {
                    // Double-check the entity is still valid right before evaluation
                    if (target == null || target.dead || target.IsFadingOut)
                    {
                        continue;
                    }
                    
                    float score = EvaluateTarget(attacker, target, boardState);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = target;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AttackManager] Error evaluating target {target?.name ?? "unknown"}: {e.Message}");
                    // Continue with next target
                }
            }

            // Final validation of the selected target
            if (bestTarget != null && (bestTarget.dead || bestTarget.IsFadingOut))
            {
                Debug.LogWarning($"[AttackManager] Best target for {attacker.name} is dead or dying, returning null");
                return null;
            }

            return bestTarget;
        }

        private float EvaluateTarget(EntityManager attacker, EntityManager target, BoardState boardState)
        {
            if (attacker == null || target == null)
                return float.MinValue;

            float score = 0;

            // Base score from attack vs health
            score += attacker.GetAttack() - target.GetHealth();

            // Consider keywords
            if (_keywordEvaluator != null)
            {
                score += _keywordEvaluator.EvaluateKeywords(attacker, target, boardState);
            }
            
            // Consider counter-attack damage and survival chance
            bool willTakeCounterAttack = !attacker.HasKeyword(Keywords.MonsterKeyword.Ranged); // Ranged units don't take counter-attack damage
            
            if (willTakeCounterAttack)
            {
                // Calculate expected counter damage
                float counterDamage = target.GetAttack();
                
                // Will our attacker survive the counter-attack?
                bool willSurvive = attacker.GetHealth() > counterDamage;
                
                if (!willSurvive)
                {
                    // Big penalty if our unit would die from counter-attack
                    score -= 100f;
                    
                    // However, if we can kill a high-value target, it might be worth the sacrifice
                    if (target.GetHealth() <= attacker.GetAttack() && 
                        (target.HasKeyword(Keywords.MonsterKeyword.Taunt) || target.HasKeyword(Keywords.MonsterKeyword.Ranged)))
                    {
                        // Trade might be worth it for important targets
                        score += 50f;
                    }
                }
                else
                {
                    // Smaller penalty based on counter damage relative to attacker's health
                    float counterDamageRatio = counterDamage / attacker.GetHealth();
                    score -= counterDamageRatio * 50f;
                }
            }
            else
            {
                // Bonus for ranged attackers who won't take counter damage
                score += 30f;
            }
            
            // Consider if we can kill the target
            if (target.GetHealth() <= attacker.GetAttack())
            {
                // Big bonus for killing targets
                score += 75f;
                
                // Extra bonus for killing high-value targets
                if (target.HasKeyword(Keywords.MonsterKeyword.Taunt) || target.HasKeyword(Keywords.MonsterKeyword.Ranged))
                {
                    score += 50f;
                }
            }

            // Consider board state
            if (boardState != null)
            {
                if (boardState.PlayerBoardControl > boardState.EnemyBoardControl)
                {
                    // Prioritize removing threats when behind on board
                    score += target.GetAttack() * 1.5f;
                }
            }

            return score;
        }

        /// <summary>
        /// Handles an enemy entity attacking the player's health icon
        /// </summary>
        /// <param name="attacker">The enemy entity performing the attack</param>
        /// <param name="healthIcon">The player's health icon</param>
        /// <returns>True if attack was successful</returns>
        private bool AttackPlayerHealthIcon(EntityManager attacker, HealthIconManager healthIcon)
        {
            if (attacker == null)
            {
                Debug.LogError("[AttackManager] Cannot attack health icon: attacker is null");
                return false;
            }
            
            if (healthIcon == null)
            {
                Debug.LogError("[AttackManager] Cannot attack health icon: health icon is null");
                return false;
            }
            
            if (_combatStage == null)
            {
                Debug.LogError("[AttackManager] Cannot attack health icon: CombatStage is null");
                return false;
            }
            
            Debug.Log($"[AttackManager] {attacker.name} attacking player health icon");
            
            try
            {
                // Perform attack against health icon
                _combatStage.HandleMonsterAttack(attacker, healthIcon);
                
                // Register the attack with the AttackLimiter
                if (_attackLimiter != null)
                {
                    _attackLimiter.RegisterAttack(attacker);
                }
                else
                {
                    attacker.HasAttacked = true;
                }
                
                Debug.Log($"[AttackManager] {attacker.name} successfully attacked player health for {attacker.GetAttackPower()} damage");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AttackManager] Error attacking health icon: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Safely refreshes the list of player entities by filtering out any that are dead or dying
        /// </summary>
        /// <param name="currentEntities">The current list of player entities</param>
        /// <returns>A filtered list of valid player entities</returns>
        private List<EntityManager> RefreshPlayerEntities(List<EntityManager> currentEntities)
        {
            if (currentEntities == null || currentEntities.Count == 0)
                return new List<EntityManager>();

            try
            {
                // Filter out any entities that are null, dead, or fading out
                return currentEntities
                    .Where(e => e != null && !e.dead && !e.IsFadingOut)
                    .ToList();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AttackManager] Error refreshing player entities: {ex.Message}");
                
                // Manual fallback in case LINQ fails
                var result = new List<EntityManager>();
                foreach (var entity in currentEntities)
                {
                    if (entity != null && !entity.dead && !entity.IsFadingOut)
                    {
                        result.Add(entity);
                    }
                }
                return result;
            }
        }
    }
} 