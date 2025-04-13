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
            bool hasEntities = false;
            bool hasTargets = false;
            bool errorOccurred = false;
            
            try
            {
                // Get all enemy entities that can attack
                enemyEntities = _spritePositioning.EnemyEntities
                    .Where(entity => entity != null)
                    .Select(entity => entity.GetComponent<EntityManager>())
                    .Where(entity => entity != null && entity.placed && !entity.dead && 
                          (_attackLimiter != null ? _attackLimiter.CanAttack(entity) : !entity.HasAttacked))
                    .ToList();
                    
                hasEntities = enemyEntities != null && enemyEntities.Count > 0;
                
                // Get all potential targets
                playerEntities = _spritePositioning.PlayerEntities
                    .Where(entity => entity != null)
                    .Select(entity => entity.GetComponent<EntityManager>())
                    .Where(entity => entity != null && entity.placed && !entity.dead)
                    .ToList();
                
                // Get player health icon as a potential target
                playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
                
                hasTargets = (playerEntities != null && playerEntities.Count > 0) || playerHealthIcon != null;
                
                if (!hasEntities)
                {
                    Debug.Log("[AttackManager] No enemy entities available to attack");
                }
                else if (!hasTargets)
                {
                    Debug.Log("[AttackManager] No valid targets available for enemy attacks");
                }
                else
                {
                    Debug.Log($"[AttackManager] Enemy has {enemyEntities.Count} entities that can attack, against {playerEntities.Count} player entities");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AttackManager] Error in Attack: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }
            
            // If we encountered an error or have no entities or no targets, just exit with a small delay
            if (errorOccurred || !hasEntities || !hasTargets)
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
            
            // Process each entity's attack
            foreach (var attacker in enemyEntities)
            {
                yield return new WaitForSeconds(0.3f); // Add delay for visual effect
                
                // Check for taunt units - must attack these first
                bool hasTaunts = playerEntities.Any(e => e.HasKeyword(Keywords.MonsterKeyword.Taunt));
                
                // Select target based on combat rules and AI strategy
                EntityManager targetEntity = null;
                
                if (hasTaunts)
                {
                    // Must attack taunt units first
                    var tauntTargets = playerEntities.Where(e => e.HasKeyword(Keywords.MonsterKeyword.Taunt)).ToList();
                    targetEntity = SelectBestTarget(attacker, tauntTargets, boardState);
                    Debug.Log($"[AttackManager] {attacker.name} must attack taunt unit {targetEntity?.name ?? "unknown"}");
                }
                else
                {
                    // Decide between attacking a player entity or going for the health icon
                    bool attackHealthIcon = ShouldAttackHealthIcon(attacker, playerEntities, playerHealthIcon, boardState);
                    
                    if (attackHealthIcon && playerHealthIcon != null)
                    {
                        Debug.Log($"[AttackManager] {attacker.name} attacking player health icon");
                        // Perform attack against health icon
                        if (_combatStage != null)
                        {
                            _combatStage.HandleMonsterAttack(attacker, playerHealthIcon);
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
                        }
                        else
                        {
                            Debug.LogError("[AttackManager] Cannot attack health icon - CombatStage is null");
                        }
                        
                        continue; // Skip to next attacker
                    }
                    else
                    {
                        // Attack a player entity
                        targetEntity = SelectBestTarget(attacker, playerEntities, boardState);
                        Debug.Log($"[AttackManager] {attacker.name} targeting {targetEntity?.name ?? "unknown"}");
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
                        
                        // Refresh our player entities list (remove dead entities)
                        playerEntities = playerEntities.Where(e => !e.dead).ToList();
                        hasTargets = (playerEntities != null && playerEntities.Count > 0) || playerHealthIcon != null;
                        
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
        
        private bool ShouldAttackHealthIcon(EntityManager attacker, List<EntityManager> playerEntities, HealthIconManager playerHealthIcon, BoardState boardState)
        {
            // Logic to decide whether to attack player health directly or target a monster
            
            // Always go for the kill if possible
            if (playerHealthIcon != null && attacker.GetAttackPower() >= playerHealthIcon.GetHealth())
            {
                return true;
            }
            
            // If player has no entities, always go for health
            if (playerEntities == null || playerEntities.Count == 0)
            {
                return true;
            }
            
            // Check if attacker is ranged (doesn't take counter-attack damage)
            bool isRanged = attacker.HasKeyword(Keywords.MonsterKeyword.Ranged);
            
            // If not ranged, consider if attacking creatures would result in death
            if (!isRanged)
            {
                // Check if any attack would lead to trading our creature
                bool wouldDieToAny = playerEntities.Any(entity => 
                    entity.GetAttackPower() >= attacker.GetHealth() && 
                    attacker.GetAttackPower() < entity.GetHealth());
                
                // If attacking creatures would kill our creature with no gain, prefer health icon
                if (wouldDieToAny)
                {
                    return Random.value < 0.7f; // 70% chance to go for health instead
                }
            }
            
            // If we have a significant health advantage, be more aggressive
            if (boardState != null && boardState.EnemyHealth > boardState.PlayerHealth * 1.5f)
            {
                // 70% chance to attack health icon directly when we have a big lead
                return Random.value < 0.7f;
            }
            
            // If player health is low, be aggressive
            if (playerHealthIcon != null && playerHealthIcon.GetHealth() < 10)
            {
                // 80% chance to go for the kill when player health is low
                return Random.value < 0.8f;
            }
            
            // In early game (turns 1-3), prefer to attack entities to control the board
            if (boardState != null && boardState.TurnCount <= 3)
            {
                // Only 20% chance to attack health icon in early game
                return Random.value < 0.2f;
            }
            
            // By default, 40% chance to attack health icon directly
            return Random.value < 0.4f;
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
            if (attacker == null || targets == null || targets.Count == 0)
                return null;

            EntityManager bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var target in targets.Where(t => t != null))
            {
                float score = EvaluateTarget(attacker, target, boardState);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
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
    }
} 