using UnityEngine;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;
using System.Collections;

namespace EnemyInteraction.Managers
{
    public class BoardStateManager : MonoBehaviour, IBoardStateManager
    {
        [SerializeField] private SpritePositioning _spritePositioning;
        private ICombatManager _combatManager;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[BoardStateManager] Starting initialization...");
            
            // Get dependencies
            _combatManager = FindObjectOfType<CombatManager>();
            while (_combatManager == null)
            {
                Debug.Log("[BoardStateManager] Waiting for CombatManager...");
                yield return null;
                _combatManager = FindObjectOfType<CombatManager>();
            }

            var combatStage = FindObjectOfType<CombatStage>();
            while (combatStage == null)
            {
                Debug.Log("[BoardStateManager] Waiting for CombatStage...");
                yield return null;
                combatStage = FindObjectOfType<CombatStage>();
            }
            
            // Wait for CombatStage to be ready
            while (combatStage.SpritePositioning == null)
            {
                Debug.Log("[BoardStateManager] Waiting for CombatStage.SpritePositioning...");
                yield return null;
            }
            
            // Try to get SpritePositioning from CombatStage if not set in inspector
            if (_spritePositioning == null)
            {
                _spritePositioning = combatStage.SpritePositioning as SpritePositioning;
            }

            ValidateReferences();
            if (_spritePositioning == null || _combatManager == null)
            {
                Debug.LogError($"[{nameof(BoardStateManager)}] Failed to initialize properly!");
                enabled = false;
                yield break;
            }

            _isInitialized = true;
            Debug.Log("[BoardStateManager] Initialization completed successfully");
        }

        private void ValidateReferences()
        {
            if (_spritePositioning == null)
                Debug.LogError($"[{nameof(BoardStateManager)}] SpritePositioning is null during initialization!");
            if (_combatManager == null)
                Debug.LogError($"[{nameof(BoardStateManager)}] CombatManager is null during initialization!");
        }

        public BoardState EvaluateBoardState()
        {
            if (!_isInitialized)
            {
                Debug.LogError($"[{nameof(BoardStateManager)}] Attempting to evaluate board state before initialization!");
                return null;
            }

            if (_spritePositioning == null || _combatManager == null)
            {
                Debug.LogError($"[{nameof(BoardStateManager)}] Critical dependencies are null!");
                return null;
            }

            var state = new BoardState
            {
                EnemyMonsters = _spritePositioning.EnemyEntities
                    ?.Where(e => e != null && e.GetComponent<EntityManager>() != null && 
                           !e.GetComponent<EntityManager>().dead && 
                           e.GetComponent<EntityManager>().placed)
                    ?.Select(e => e.GetComponent<EntityManager>())
                    ?.ToList(),

                PlayerMonsters = _spritePositioning.PlayerEntities
                    ?.Where(e => e != null && e.GetComponent<EntityManager>() != null && 
                           !e.GetComponent<EntityManager>().dead && 
                           e.GetComponent<EntityManager>().placed)
                    ?.Select(e => e.GetComponent<EntityManager>())
                    ?.ToList(),

                EnemyHealth = _combatManager.EnemyHealth,
                PlayerHealth = _combatManager.PlayerHealth,
                TurnCount = _combatManager.TurnCount,
                EnemyMana = _combatManager.EnemyMana
            };

            // Calculate board control
            state.EnemyBoardControl = CalculateBoardControl(state.EnemyMonsters);
            state.PlayerBoardControl = CalculateBoardControl(state.PlayerMonsters);
            
            // Add health icon considerations to board control
            state.EnemyBoardControl += state.EnemyHealth * 0.2f; // Health is worth 20% of its value in board control
            state.PlayerBoardControl += state.PlayerHealth * 0.2f;
            
            state.BoardControlDifference = state.EnemyBoardControl - state.PlayerBoardControl;

            // Calculate health metrics
            state.HealthAdvantage = state.EnemyHealth - state.PlayerHealth;
            state.HealthRatio = state.EnemyHealth / (float)_combatManager.MaxHealth;

            return state;
        }

        private float CalculateBoardControl(System.Collections.Generic.List<EntityManager> entities)
        {
            if (entities == null || entities.Count == 0)
                return 0f;

            return entities.Sum(e => 
            {
                if (e == null) return 0f;
                return e.GetAttack() + e.GetHealth();
            });
        }
    }
} 