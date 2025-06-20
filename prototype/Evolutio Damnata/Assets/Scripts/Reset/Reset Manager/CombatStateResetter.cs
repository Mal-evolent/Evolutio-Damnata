using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Combat.Reset
{
    /// <summary>
    /// Handles resetting the CombatManager to its initial state
    /// </summary>
    [RequireComponent(typeof(CombatManager))]
    public class CombatManagerResetter : MonoBehaviour, IResettable
    {
        [SerializeField] private int _resetPriority = 10;
        
        private CombatManager _combatManager;

        public int ResetPriority => _resetPriority;

        private void Awake()
        {
            _combatManager = GetComponent<CombatManager>();
            
            // Register with the reset manager if it exists
            var resetManager = FindObjectOfType<CombatResetManager>();
            if (resetManager != null)
            {
                resetManager.RegisterResettable(this);
            }
            else
            {
                Debug.LogWarning("[CombatManagerResetter] No CombatResetManager found in scene");
            }
        }

        private void OnDestroy()
        {
            // Unregister when destroyed
            var resetManager = FindObjectOfType<CombatResetManager>();
            if (resetManager != null)
            {
                resetManager.UnregisterResettable(this);
            }
        }

        public async Task ResetAsync()
        {
            Debug.Log("[CombatManagerResetter] Resetting combat manager");

            // Reset game state variables
            ResetHealthValues();
            ResetManaValues();
            ResetTurnState();
            
            // Reset UI elements
            UpdateUIElements();

            await Task.CompletedTask; // For async compatibility
        }

        private void ResetHealthValues()
        {
            // Reset health to max values
            _combatManager.PlayerHealth = _combatManager.PlayerMaxHealth;
            _combatManager.EnemyHealth = _combatManager.EnemyMaxHealth;
        }

        private void ResetManaValues()
        {
            // Reset mana values
            _combatManager.MaxMana = 1; // Starting max mana
            _combatManager.PlayerMana = 1;
            _combatManager.EnemyMana = 1;
        }

        private void ResetTurnState()
        {
            // Reset turn counter and phase
            _combatManager.TurnCount = 0;
            _combatManager.ResetPhaseState();

            // Always make sure player goes first
            _combatManager.PlayerGoesFirst = true;
            _combatManager.PlayerTurn = true;


            Debug.Log("[CombatStateResetter] Reset player turn order. Player will go first.");
        }

        private void UpdateUIElements()
        {
            // Update UI elements
            _combatManager.InitializeManaUI();
            _combatManager.UpdateManaUI();
        }

        /// <summary>
        /// Manual reset that can be called directly (for testing or UI events)
        /// </summary>
        public void ManualReset()
        {
            _ = ResetAsync();
        }
    }
}