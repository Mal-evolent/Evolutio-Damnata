using UnityEngine;
using TMPro;
using UnityEngine.UI;
using EnemyInteraction;

namespace Combat.UI
{
    /// <summary>
    /// Class responsible for managing the visibility of combat UI elements.
    /// </summary>
    public class CombatUIVisibilityManager : MonoBehaviour
    {
        [Header("Combat Manager Reference")]
        [SerializeField] private CombatManager _combatManager;

        /// <summary>
        /// Initialize with a CombatManager reference.
        /// Can be called from code instead of using the inspector assignment.
        /// </summary>
        /// <param name="combatManager">The combat manager to use</param>
        public void Initialize(CombatManager combatManager)
        {
            _combatManager = combatManager;
        }

        /// <summary>
        /// Sets the visibility of all combat UI elements
        /// </summary>
        /// <param name="isVisible">Whether the combat UI should be visible</param>
        public void SetCombatUIVisibility(bool isVisible)
        {
            // Early return if combat manager is null to prevent NullReferenceException
            if (_combatManager == null)
            {
                Debug.LogWarning("[CombatUIVisibilityManager] Cannot set UI visibility: CombatManager is null.");
                return;
            }

            // Use the UIContainerObject to control all UI elements at once
            if (_combatManager.UIContainerObject != null)
            {
                _combatManager.UIContainerObject.SetActive(isVisible);
                Debug.Log($"[CombatUIVisibilityManager] Combat UI visibility set to: {isVisible} using UIContainerObject");
            }
            else
            {
                Debug.LogWarning("[CombatUIVisibilityManager] UIContainerObject is null in CombatManager.");
            }
        }
    }
}
