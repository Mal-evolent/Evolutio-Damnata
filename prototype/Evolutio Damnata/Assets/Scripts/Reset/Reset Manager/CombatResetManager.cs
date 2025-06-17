using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Combat.Reset
{
    /// <summary>
    /// Manages the reset process for all combat-related components
    /// </summary>
    public class CombatResetManager : MonoBehaviour
    {
        private readonly List<IResettable> _resettableComponents = new List<IResettable>();
        private bool _isResetting = false;

        /// <summary>
        /// Register a component to be reset when ResetCombat is called
        /// </summary>
        public void RegisterResettable(IResettable resettable)
        {
            if (!_resettableComponents.Contains(resettable))
            {
                _resettableComponents.Add(resettable);
                // Sort by priority each time we add a new component
                _resettableComponents.Sort((a, b) => a.ResetPriority.CompareTo(b.ResetPriority));
            }
        }

        /// <summary>
        /// Unregister a component from the reset system
        /// </summary>
        public void UnregisterResettable(IResettable resettable)
        {
            _resettableComponents.Remove(resettable);
        }

        /// <summary>
        /// Reset all registered components in priority order
        /// </summary>
        public async Task ResetCombatAsync()
        {
            if (_isResetting)
                return;

            _isResetting = true;
            Debug.Log("[CombatResetManager] Beginning combat reset sequence");

            try
            {
                foreach (var resettable in _resettableComponents)
                {
                    await resettable.ResetAsync();
                }
                Debug.Log("[CombatResetManager] Combat reset completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CombatResetManager] Error during reset: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Non-async version that can be called from Unity events
        /// </summary>
        public void ResetCombat()
        {
            _ = ResetCombatAsync();
        }
    }
}