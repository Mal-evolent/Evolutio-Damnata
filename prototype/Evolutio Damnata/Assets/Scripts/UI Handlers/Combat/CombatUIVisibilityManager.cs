using UnityEngine;
using TMPro;
using UnityEngine.UI;
using EnemyInteraction;
using System.Collections;

namespace Combat.UI
{
    /// <summary>
    /// Class responsible for managing the visibility of combat UI elements.
    /// </summary>
    public class CombatUIVisibilityManager : MonoBehaviour
    {
        [Header("Combat Manager Reference")]
        [SerializeField] private CombatManager _combatManager;

        [Header("Fade Settings")]
        [SerializeField] private float _fadeOutDuration = 3.5f; // Duration of the fade-out effect in seconds
        [SerializeField] private float _delayBeforeFadeOut = 0.5f; // Delay before starting the fade-out

        private CanvasGroup _canvasGroup;

        /// <summary>
        /// Initialize with a CombatManager reference.
        /// Can be called from code instead of using the inspector assignment.
        /// </summary>
        /// <param name="combatManager">The combat manager to use</param>
        public void Initialize(CombatManager combatManager)
        {
            _combatManager = combatManager;

            // Add CanvasGroup component if it doesn't exist
            if (_combatManager.UIContainerObject != null)
            {
                _canvasGroup = _combatManager.UIContainerObject.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = _combatManager.UIContainerObject.AddComponent<CanvasGroup>();
                }
            }

            // Subscribe to enemy defeated event
            if (_combatManager != null)
            {
                _combatManager.OnEnemyDefeated += OnCombatConcluded;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (_combatManager != null)
            {
                _combatManager.OnEnemyDefeated -= OnCombatConcluded;
            }
        }

        private void OnCombatConcluded()
        {
            StartCoroutine(FadeOutCombatUI());
            Debug.Log("[CombatUIVisibilityManager] Combat concluded, fading out UI");
        }

        /// <summary>
        /// Coroutine that slowly fades out the combat UI
        /// </summary>
        private IEnumerator FadeOutCombatUI()
        {
            // Wait for a short delay before starting the fade
            yield return new WaitForSeconds(_delayBeforeFadeOut);

            if (_canvasGroup == null || _combatManager.UIContainerObject == null)
            {
                Debug.LogWarning("[CombatUIVisibilityManager] Cannot fade out: CanvasGroup or UIContainer is null");
                SetCombatUIVisibility(false); // Fall back to immediate hide
                yield break;
            }

            float elapsedTime = 0;
            _canvasGroup.alpha = 1f;

            // Gradually decrease alpha over time
            while (elapsedTime < _fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / _fadeOutDuration);
                yield return null;
            }

            // Ensure alpha is exactly 0 at the end
            _canvasGroup.alpha = 0f;
            _combatManager.UIContainerObject.SetActive(false);

            Debug.Log("[CombatUIVisibilityManager] Combat UI fade-out complete");
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
                if (isVisible)
                {
                    _combatManager.UIContainerObject.SetActive(true);

                    // Reset alpha if we have a CanvasGroup
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = 1f;
                    }
                }
                else
                {
                    _combatManager.UIContainerObject.SetActive(false);
                }

                Debug.Log($"[CombatUIVisibilityManager] Combat UI visibility set to: {isVisible} using UIContainerObject");
            }
            else
            {
                Debug.LogWarning("[CombatUIVisibilityManager] UIContainerObject is null in CombatManager.");
            }
        }
    }
}
