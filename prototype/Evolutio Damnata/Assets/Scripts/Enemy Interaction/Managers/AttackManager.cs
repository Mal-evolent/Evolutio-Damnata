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
            
            // Declare variables we'll use inside and outside the try block
            List<EntityManager> enemyEntities = null;
            bool hasEntities = false;
            bool errorOccurred = false;
            
            try
            {
                // Basic implementation that just logs what would happen
                enemyEntities = _spritePositioning.EnemyEntities
                    .Where(entity => entity != null)
                    .Select(entity => entity.GetComponent<EntityManager>())
                    .Where(entity => entity != null && entity.placed && !entity.dead)
                    .ToList();
                    
                hasEntities = enemyEntities != null && enemyEntities.Count > 0;
                
                if (!hasEntities)
                {
                    Debug.Log("[AttackManager] No enemy entities available to attack");
                }
                else
                {
                    Debug.Log($"[AttackManager] Enemy has {enemyEntities.Count} entities that can attack");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AttackManager] Error in Attack: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }
            
            // If we encountered an error or have no entities, just exit with a small delay
            if (errorOccurred || !hasEntities)
            {
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[AttackManager] Attack completed");
                yield break;
            }
            
            // Process each entity's attack
            foreach (var entity in enemyEntities)
            {
                Debug.Log($"[AttackManager] Entity {entity.name} would attack here");
                yield return new WaitForSeconds(0.3f);
            }
            
            // Additional delay at the end
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[AttackManager] Attack completed");
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
                score += _keywordEvaluator.EvaluateKeywords(attacker, target);
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