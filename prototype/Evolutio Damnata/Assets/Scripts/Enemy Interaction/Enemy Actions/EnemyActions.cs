using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers;
using EnemyInteraction.Models;
using EnemyInteraction.Services;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Utilities;

namespace EnemyInteraction
{
    public class EnemyActions : MonoBehaviour, IEnemyActions
    {
        [SerializeField] private EnemyDependencyInitializer _dependencyInitializer;

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        private CombatPhase _currentPhase = CombatPhase.None;
        private bool _canTakeAction = false;

        private void Awake()
        {
            if (_dependencyInitializer == null)
            {
                _dependencyInitializer = gameObject.AddComponent<EnemyDependencyInitializer>();
            }

            StartCoroutine(Initialize());
        }

        private void Start()
        {
            if (_dependencyInitializer?.CombatManager is CombatManager combatManagerImpl)
            {
                combatManagerImpl.SubscribeToPhaseChanges(OnPhaseChanged);
            }
        }

        private void OnDestroy()
        {
            if (_dependencyInitializer?.CombatManager is CombatManager combatManagerImpl)
            {
                combatManagerImpl.UnsubscribeFromPhaseChanges(OnPhaseChanged);
            }
        }

        public void ResetActionState()
        {
            Debug.Log("[EnemyActions] Externally requested action state reset");
            // Use the existing private OnPhaseChanged method to reset state
            OnPhaseChanged(CombatPhase.None);
        }

        private void OnPhaseChanged(CombatPhase newPhase)
        {
            _currentPhase = newPhase;

            // Determine if enemy can act in this phase
            var combatManager = _dependencyInitializer?.CombatManager;
            if (combatManager != null)
            {
                bool isEnemyPhase = combatManager.IsEnemyPrepPhase() || combatManager.IsEnemyCombatPhase();
                bool isEnemyTurn = !combatManager.PlayerTurn;
                _canTakeAction = isEnemyPhase && isEnemyTurn;

                Debug.Log($"[EnemyActions] Phase changed to {newPhase} - " +
                          $"PlayerTurn: {combatManager.PlayerTurn}, " +
                          $"Can take action: {_canTakeAction}");

                // Additional validation when room transitions happen
                if (newPhase == CombatPhase.None)
                {
                    _canTakeAction = false;
                    Debug.Log("[EnemyActions] Combat reset detected, disabling enemy actions");
                }
            }
            else
            {
                _canTakeAction = false;
                Debug.LogWarning("[EnemyActions] Combat manager not available, disabling enemy actions");
            }
        }

        private IEnumerator Initialize()
        {
            yield return StartCoroutine(_dependencyInitializer.Initialize());
            _isInitialized = _dependencyInitializer.IsInitialized;
        }

        public IEnumerator PlayCards()
        {
            Debug.Log("[EnemyActions] Starting PlayCards");

            // Wait for initialization if needed
            yield return StartCoroutine(InitializationUtility.WaitForInitialization(_isInitialized));

            // If still not initialized, abort
            if (!_isInitialized)
            {
                Debug.LogError("[EnemyActions] Failed to initialize, cannot play cards");
                yield break;
            }

            // IMPROVED: Check if enemy can take action based on current phase
            var combatManager = _dependencyInitializer?.CombatManager;
            if (combatManager != null)
            {
                bool isEnemyPhase = combatManager.IsEnemyPrepPhase() || combatManager.IsEnemyCombatPhase();
                bool isEnemyTurn = !combatManager.PlayerTurn;

                if (!isEnemyPhase || !isEnemyTurn)
                {
                    Debug.LogWarning($"[EnemyActions] Cannot play cards - Phase: {combatManager.CurrentPhase}, PlayerTurn: {combatManager.PlayerTurn}");
                    yield break;
                }

                // Log state to help debugging
                AttackValidator.LogCombatStateTransition(combatManager, "PlayCards validation");
            }
            else if (!_canTakeAction)
            {
                Debug.LogWarning("[EnemyActions] Cannot play cards - not in a valid enemy action phase");
                yield break;
            }

            // Wait for AIServices
            yield return StartCoroutine(InitializationUtility.WaitForAIServicesInitialization());

            // Check if AIServices instance exists and is initialized
            if (AIServices.Instance == null || !AIServices.Instance.IsInitialized)
            {
                Debug.LogError("[EnemyActions] AIServices initialization timed out or instance is null");
                yield break;
            }

            var cardPlayManager = _dependencyInitializer.CardPlayManager;

            // Ensure CardPlayManager is available
            if (cardPlayManager == null)
            {
                Debug.LogError("[EnemyActions] CardPlayManager is null, cannot play cards");
                yield break;
            }

            // Refresh entity cache before playing cards
            var entityCacheManager = AIServices.Instance.EntityCacheManager as EntityCacheManager;
            entityCacheManager?.RefreshAfterAction();

            // Execute card playing
            yield return StartCoroutine(ExecuteCardPlaying(cardPlayManager));
        }

        private IEnumerator ExecuteCardPlaying(ICardPlayManager cardPlayManager)
        {
            IEnumerator playCardsCoroutine = null;
            bool errorOccurred = false;

            try
            {
                playCardsCoroutine = cardPlayManager.PlayCards();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemyActions] Error in CardPlayManager.PlayCards: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }

            if (playCardsCoroutine != null && !errorOccurred)
            {
                yield return playCardsCoroutine;
            }
            else if (errorOccurred)
            {
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[EnemyActions] PlayCards completed");
        }

        public IEnumerator Attack()
        {
            Debug.Log("[EnemyActions] Starting Attack");

            // Wait for initialization if needed
            yield return StartCoroutine(InitializationUtility.WaitForInitialization(_isInitialized));

            // If still not initialized, abort
            if (!_isInitialized)
            {
                Debug.LogError("[EnemyActions] Failed to initialize, cannot perform attack");
                yield break;
            }

            // IMPROVED: Check if enemy can take action based on current phase
            var combatManager = _dependencyInitializer?.CombatManager;
            if (combatManager != null)
            {
                bool isEnemyCombatPhase = combatManager.IsEnemyCombatPhase();
                bool isEnemyTurn = !combatManager.PlayerTurn;

                if (!isEnemyCombatPhase || !isEnemyTurn)
                {
                    Debug.LogWarning($"[EnemyActions] Cannot attack - Phase: {combatManager.CurrentPhase}, PlayerTurn: {combatManager.PlayerTurn}");
                    yield break;
                }

                // Log state to help debugging
                AttackValidator.LogCombatStateTransition(combatManager, "Attack validation");
            }
            else if (!_canTakeAction)
            {
                Debug.LogWarning("[EnemyActions] Cannot attack - not in a valid enemy action phase");
                yield break;
            }

            // Wait for AIServices
            yield return StartCoroutine(InitializationUtility.WaitForAIServicesInitialization());

            // Check if AIServices instance exists and is initialized
            if (AIServices.Instance == null || !AIServices.Instance.IsInitialized)
            {
                Debug.LogError("[EnemyActions] AIServices initialization timed out or instance is null");
                yield break;
            }

            var attackManager = _dependencyInitializer.AttackManager;

            // Ensure AttackManager is available
            if (attackManager == null)
            {
                Debug.LogError("[EnemyActions] AttackManager is null, cannot perform attack");
                yield break;
            }

            // Execute attack
            yield return StartCoroutine(ExecuteAttack(attackManager));
        }

        private IEnumerator ExecuteAttack(IAttackManager attackManager)
        {
            IEnumerator attackCoroutine = null;
            bool errorOccurred = false;

            try
            {
                attackCoroutine = attackManager.Attack();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemyActions] Error in AttackManager.Attack: {e.Message}\n{e.StackTrace}");
                errorOccurred = true;
            }

            if (attackCoroutine != null && !errorOccurred)
            {
                yield return attackCoroutine;
            }
            else if (errorOccurred)
            {
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[EnemyActions] Attack completed");
        }

        public void LogCardsInHand()
        {
            var combatManager = _dependencyInitializer.CombatManager;

            if (combatManager?.EnemyDeck?.Hand == null)
            {
                Debug.LogWarning("Enemy deck or hand is null");
                return;
            }

            Debug.Log("Enemy Cards in Hand:");
            foreach (var card in combatManager.EnemyDeck.Hand)
            {
                if (card != null)
                {
                    Debug.Log($"- {card.CardName} (Mana Cost: {card.CardType.ManaCost})");
                }
            }
        }
    }
}

