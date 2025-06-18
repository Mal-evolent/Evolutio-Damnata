using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using EnemyInteraction;

/// <summary>
/// Manages the phases of combat including preparation, combat execution, and cleanup.
/// Implements the IPhaseManager interface to control the flow of combat turns.
/// </summary>
public class PhaseManager : IPhaseManager
{
    private readonly ICombatManager _combatManager;
    private readonly AttackLimiter _attackLimiter;
    private readonly IUIManager _uiManager;
    private IEnemyActions _enemyActions;
    private readonly IPlayerActions _playerActions;
    private readonly IRoundManager _roundManager;

    private readonly Image _prepPhaseImage;
    private readonly Image _combatPhaseImage;
    private readonly Image _cleanupPhaseImage;

    /// <summary>
    /// Initializes a new instance of the PhaseManager class.
    /// </summary>
    /// <param name="combatManager">Manager handling combat state and interactions</param>
    /// <param name="attackLimiter">Component that manages attack limitations</param>
    /// <param name="uiManager">Manager handling UI interactions</param>
    /// <param name="enemyActions">Component for enemy AI decisions and actions</param>
    /// <param name="playerActions">Component handling player interactions</param>
    /// <param name="roundManager">Manager for round progression</param>
    /// <param name="prepPhaseImage">UI image shown during preparation phase</param>
    /// <param name="combatPhaseImage">UI image shown during combat phase</param>
    /// <param name="cleanupPhaseImage">UI image shown during cleanup phase</param>
    public PhaseManager(
        ICombatManager combatManager,
        AttackLimiter attackLimiter,
        IUIManager uiManager,
        IEnemyActions enemyActions,
        IPlayerActions playerActions,
        IRoundManager roundManager,
        Image prepPhaseImage,
        Image combatPhaseImage,
        Image cleanupPhaseImage)
    {
        _combatManager = combatManager;
        _attackLimiter = attackLimiter;
        _uiManager = uiManager;
        _enemyActions = enemyActions;
        _playerActions = playerActions;
        _roundManager = roundManager;
        _prepPhaseImage = prepPhaseImage;
        _combatPhaseImage = combatPhaseImage;
        _cleanupPhaseImage = cleanupPhaseImage;
    }

    /// <summary>
    /// Updates the UI to show which phase is currently active.
    /// </summary>
    /// <param name="phase">The current combat phase</param>
    private void SetPhaseImage(CombatPhase phase)
    {
        _prepPhaseImage.enabled = (phase == CombatPhase.PlayerPrep || phase == CombatPhase.EnemyPrep);
        _combatPhaseImage.enabled = (phase == CombatPhase.PlayerCombat || phase == CombatPhase.EnemyCombat);
        _cleanupPhaseImage.enabled = (phase == CombatPhase.CleanUp);
    }

    /// <summary>
    /// Initiates the preparation phase of combat.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    public IEnumerator PrepPhase()
    {
        Debug.Log($"[PhaseManager] ===== ENTERING PREP PHASE - Turn {_combatManager.TurnCount} =====");
        Debug.Log($"[PhaseManager] PlayerTurn: {_combatManager.PlayerTurn}, PlayerGoesFirst: {_combatManager.PlayerGoesFirst}");

        _combatManager.ResetPhaseState();
        SetPhaseImage(_combatManager.PlayerTurn ? CombatPhase.PlayerPrep : CombatPhase.EnemyPrep);

        if (_combatManager.PlayerTurn)
        {
            Debug.Log("[PhaseManager] Running Player Prep Phase");
            yield return RunSafely(PlayerPrepPhase(), "Player Prep Phase");
        }
        else
        {
            Debug.Log("[PhaseManager] Running Enemy Prep Phase");
            yield return RunSafely(EnemyPrepPhase(), "Enemy Prep Phase");
        }

        _combatManager.CurrentPhase = CombatPhase.None;
        yield return RunSafely(ExecuteCombatPhase(), "Execute Combat Phase");
    }

    /// <summary>
    /// Executes the combat phase where players and enemies can attack.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    public IEnumerator ExecuteCombatPhase()
    {
        Debug.Log("[PhaseManager] ===== ENTERING COMBAT PHASE =====");
        _combatManager.ResetPhaseState();
        SetPhaseImage(CombatPhase.PlayerCombat);

        if (_combatManager.PlayerTurn)
        {
            // Execute the other prep phase before combat begins
            Debug.Log("[PhaseManager] Running Enemy Prep Phase (after Player Prep)");
            yield return RunSafely(EnemyPrepPhase(), "Enemy Prep Phase");

            // Combat phase for the current turn
            Debug.Log("[PhaseManager] Running Enemy Combat Phase (Player started turn)");
            yield return RunSafely(EnemyCombatPhase(), "Enemy Combat Phase");
            Debug.Log("[PhaseManager] Running Player Combat Phase (Player started turn)");
            yield return RunSafely(PlayerCombatPhase(), "Player Combat Phase");
        }
        else
        {
            // Execute the other prep phase before combat begins
            Debug.Log("[PhaseManager] Running Player Prep Phase (after Enemy Prep)");
            yield return RunSafely(PlayerPrepPhase(), "Player Prep Phase");

            // Combat phase for the current turn
            Debug.Log("[PhaseManager] Running Player Combat Phase (Enemy started turn)");
            yield return RunSafely(PlayerCombatPhase(), "Player Combat Phase");
            Debug.Log("[PhaseManager] Running Enemy Combat Phase (Enemy started turn)");
            yield return RunSafely(EnemyCombatPhase(), "Enemy Combat Phase");
        }

        _combatManager.CurrentPhase = CombatPhase.None;
        yield return RunSafely(CleanUpPhase(), "Clean Up Phase");
    }

    /// <summary>
    /// Executes the cleanup phase at the end of a combat round.
    /// Processes entities, stack effects, and draws cards.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator CleanUpPhase()
    {
        Debug.Log("[PhaseManager] ===== ENTERING CLEAN-UP PHASE =====");
        _combatManager.CurrentPhase = CombatPhase.CleanUp;
        SetPhaseImage(CombatPhase.CleanUp);

        // Process all entities once, handling both attack resets and effects
        Debug.Log("[PhaseManager] Processing entities for cleanup");
        yield return ProcessEntities(_combatManager.CombatStage.SpritePositioning.PlayerEntities, "Player", false);
        yield return ProcessEntities(_combatManager.CombatStage.SpritePositioning.EnemyEntities, "Enemy", false);

        // Process stack effects once for all entities
        Debug.Log("[PhaseManager] Processing stack effects");
        StackManager.Instance.ProcessStack();

        // Continue with normal cleanup
        yield return DrawCards();
        yield return new WaitForSeconds(1);

        // Use the new StartNextRound method instead of directly starting RoundStart coroutine
        Debug.Log("[PhaseManager] Clean-up phase complete, starting next round");
        _roundManager.StartNextRound();
    }

    /// <summary>
    /// Ends the current phase of combat.
    /// </summary>
    public void EndPhase()
    {
        Debug.Log("[PhaseManager] Ending current phase");
    }

    #region Phase Handlers
    /// <summary>
    /// Handles the player's preparation phase where they can play cards.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator PlayerPrepPhase()
    {
        _combatManager.CurrentPhase = CombatPhase.PlayerPrep;
        Debug.Log("[PhaseManager] Player's Prep Phase");
        _uiManager.SetButtonState(_combatManager.EndPhaseButton, true);
        yield return new WaitUntil(() => !_combatManager.EndPhaseButton.gameObject.activeSelf);
        _combatManager.PlayerTurn = true;
    }

    /// <summary>
    /// Handles the enemy's preparation phase where the AI plays cards.
    /// Includes additional error checking and timeout handling.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator EnemyPrepPhase()
    {
        if (_enemyActions == null)
        {
            Debug.LogError("[PhaseManager] EnemyActions is null in EnemyPrepPhase!");

            // Try to find it in the scene
            var enemyActionsObj = GameObject.FindObjectOfType<EnemyInteraction.EnemyActions>();
            if (enemyActionsObj != null)
            {
                _enemyActions = enemyActionsObj;
                Debug.Log("[PhaseManager] Found EnemyActions in scene");
            }
            else
            {
                // No other option but to skip
                Debug.LogError("[PhaseManager] Cannot find EnemyActions in scene, skipping EnemyPrepPhase");
                yield break;
            }
        }

        // Wait for EnemyActions to be fully initialized
        float timeout = 5f; // 5 seconds timeout
        float timer = 0f;

        // First check if the component is enabled
        while (!(_enemyActions as MonoBehaviour)?.enabled ?? false)
        {
            Debug.Log("[PhaseManager] Waiting for EnemyActions to be enabled...");
            yield return null;

            timer += Time.deltaTime;
            if (timer > timeout)
            {
                Debug.LogError("[PhaseManager] Timeout waiting for EnemyActions to be enabled!");
                yield break;
            }
        }

        // Then check for initialization if possible
        if (_enemyActions is EnemyInteraction.EnemyActions enemyActionsImpl)
        {
            // Check if we can access IsInitialized property
            var isInitializedProperty = enemyActionsImpl.GetType().GetProperty("IsInitialized");
            if (isInitializedProperty != null)
            {
                timer = 0f;
                while (!(bool)isInitializedProperty.GetValue(enemyActionsImpl) && timer < timeout)
                {
                    Debug.Log("[PhaseManager] Waiting for EnemyActions to be fully initialized...");
                    yield return null;
                    timer += Time.deltaTime;
                }

                if (timer >= timeout)
                {
                    Debug.LogWarning("[PhaseManager] Timeout waiting for EnemyActions to be fully initialized, proceeding anyway...");
                }
            }
        }

        _combatManager.PlayerTurn = false;
        _combatManager.CurrentPhase = CombatPhase.EnemyPrep;
        Debug.Log("[PhaseManager] Enemy's Prep Phase");

        // Extra safety for PlayCards
        IEnumerator playCardsCoroutine = null;
        bool errorOccurred = false;

        try
        {
            playCardsCoroutine = _enemyActions.PlayCards();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhaseManager] Error getting PlayCards coroutine: {e.Message}\n{e.StackTrace}");
            errorOccurred = true;
        }

        if (playCardsCoroutine == null || errorOccurred)
        {
            Debug.LogError("[PhaseManager] _enemyActions.PlayCards() returned null or error occurred!");
            yield return new WaitForSeconds(0.5f); // Short delay to prevent freeze
            yield break;
        }

        yield return RunSafely(playCardsCoroutine, "Enemy PlayCards");
    }

    /// <summary>
    /// Handles the player's combat phase where they can initiate attacks.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator PlayerCombatPhase()
    {
        _combatManager.PlayerTurn = true;
        _combatManager.CurrentPhase = CombatPhase.PlayerCombat;
        Debug.Log("[PhaseManager] Player's Combat Phase");
        _uiManager.SetButtonState(_combatManager.EndTurnButton, true);
        _playerActions.PlayerTurnEnded = false;
        yield return new WaitUntil(() => _playerActions.PlayerTurnEnded);
    }

    /// <summary>
    /// Handles the enemy's combat phase where the AI initiates attacks.
    /// Includes additional error checking and timeout handling.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator EnemyCombatPhase()
    {
        if (_enemyActions == null)
        {
            Debug.LogError("[PhaseManager] EnemyActions is null in EnemyCombatPhase!");

            // Try to find it in the scene
            var enemyActionsObj = GameObject.FindObjectOfType<EnemyInteraction.EnemyActions>();
            if (enemyActionsObj != null)
            {
                _enemyActions = enemyActionsObj;
                Debug.Log("[PhaseManager] Found EnemyActions in scene");
            }
            else
            {
                // No other option but to skip
                Debug.LogError("[PhaseManager] Cannot find EnemyActions in scene, skipping EnemyCombatPhase");
                yield break;
            }
        }

        // Wait for EnemyActions to be fully initialized - similar to EnemyPrepPhase
        float timeout = 5f; // 5 seconds timeout
        float timer = 0f;

        // First check if the component is enabled
        while (!(_enemyActions as MonoBehaviour)?.enabled ?? false)
        {
            Debug.Log("[PhaseManager] Waiting for EnemyActions to be enabled...");
            yield return null;

            timer += Time.deltaTime;
            if (timer > timeout)
            {
                Debug.LogError("[PhaseManager] Timeout waiting for EnemyActions to be enabled!");
                yield break;
            }
        }

        // Then check for initialization if possible
        if (_enemyActions is EnemyInteraction.EnemyActions enemyActionsImpl)
        {
            // Check if we can access IsInitialized property
            var isInitializedProperty = enemyActionsImpl.GetType().GetProperty("IsInitialized");
            if (isInitializedProperty != null)
            {
                timer = 0f;
                while (!(bool)isInitializedProperty.GetValue(enemyActionsImpl) && timer < timeout)
                {
                    Debug.Log("[PhaseManager] Waiting for EnemyActions to be fully initialized...");
                    yield return null;
                    timer += Time.deltaTime;
                }

                if (timer >= timeout)
                {
                    Debug.LogWarning("[PhaseManager] Timeout waiting for EnemyActions to be fully initialized, proceeding anyway...");
                }
            }
        }

        _combatManager.PlayerTurn = false;
        _combatManager.CurrentPhase = CombatPhase.EnemyCombat;
        Debug.Log("[PhaseManager] Enemy's Combat Phase");

        // First, let the AI play spell cards during combat phase
        Debug.Log("[PhaseManager] Enemy playing spell cards during combat phase");
        IEnumerator playCardsCoroutine = null;
        bool playCardsErrorOccurred = false;

        try
        {
            playCardsCoroutine = _enemyActions.PlayCards();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhaseManager] Error getting PlayCards coroutine in combat phase: {e.Message}\n{e.StackTrace}");
            playCardsErrorOccurred = true;
        }

        if (playCardsCoroutine != null && !playCardsErrorOccurred)
        {
            yield return RunSafely(playCardsCoroutine, "Enemy PlayCards during Combat");
        }
        else if (playCardsErrorOccurred)
        {
            Debug.LogError("[PhaseManager] _enemyActions.PlayCards() in combat phase returned null or error occurred!");
            yield return new WaitForSeconds(0.5f); // Short delay to prevent freeze
        }

        // Now proceed with attacks
        // Extra safety for Attack
        IEnumerator attackCoroutine = null;
        bool attackErrorOccurred = false;

        try
        {
            attackCoroutine = _enemyActions.Attack();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhaseManager] Error getting Attack coroutine: {e.Message}\n{e.StackTrace}");
            attackErrorOccurred = true;
        }

        if (attackCoroutine == null || attackErrorOccurred)
        {
            Debug.LogError("[PhaseManager] _enemyActions.Attack() returned null or error occurred!");
            yield return new WaitForSeconds(0.5f); // Short delay to prevent freeze
            yield break;
        }

        yield return RunSafely(attackCoroutine, "Enemy Attack");
    }
    #endregion

    #region Cleanup Processing
    /// <summary>
    /// Processes all entities in a collection, resetting attacks and optionally applying effects.
    /// </summary>
    /// <param name="entities">The collection of entities to process</param>
    /// <param name="entityType">Type identifier for logging (e.g., "Player", "Enemy")</param>
    /// <param name="applyEffects">Whether to apply ongoing effects to entities</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator ProcessEntities(List<GameObject> entities, string entityType, bool applyEffects)
    {
        if (entities == null) yield break;

        Debug.Log($"[PhaseManager] Processing {entityType} entities (Count: {entities.Count})");

        var entitiesToProcess = new List<GameObject>(entities);

        for (int i = 0; i < entitiesToProcess.Count; i++)
        {
            var entity = entitiesToProcess[i];
            if (entity == null)
            {
                Debug.LogWarning($"[PhaseManager] {entityType} entity {i} is null - removing from list");
                entities.Remove(entity);
                continue;
            }

            if (!entity.activeInHierarchy)
            {
                Debug.Log($"[PhaseManager] {entityType} entity {i} is inactive - skipping");
                continue;
            }

            var entityManager = entity.GetComponent<EntityManager>();
            if (entityManager == null)
            {
                Debug.LogWarning($"[PhaseManager] {entityType} entity {i} has no EntityManager");
                continue;
            }

            if (entityManager.dead)
            {
                Debug.Log($"[PhaseManager] Skipping dead {entityType} entity: {entity.name}");
                continue;
            }

            try
            {
                Debug.Log($"[PhaseManager] Processing {entityType} entity: {entity.name}");

                // Always reset attacks first
                _attackLimiter.ResetAttacks(entityManager);

                // Only apply effects if explicitly requested
                if (applyEffects)
                {
                    entityManager.ApplyOngoingEffect();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PhaseManager] Error processing {entityType} entity {entity.name}: {e.Message}");
            }

            yield return null;
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Safely executes a coroutine with exception handling.
    /// </summary>
    /// <param name="coroutine">The coroutine to execute safely</param>
    /// <param name="context">Context description for logging if errors occur</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator RunSafely(IEnumerator coroutine, string context)
    {
        if (coroutine == null)
        {
            Debug.LogError($"[PhaseManager] Null coroutine passed to RunSafely for context: {context}");
            yield break;
        }

        while (true)
        {
            bool moveNext;
            try
            {
                moveNext = coroutine.MoveNext();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PhaseManager] Error in {context}: {e.Message}\n{e.StackTrace}");
                yield break;
            }

            if (!moveNext) break;
            yield return coroutine.Current;
        }
    }

    /// <summary>
    /// Draws cards for both player and enemy decks.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator DrawCards()
    {
        Debug.Log("[PhaseManager] Drawing cards");
        _combatManager.PlayerDeck?.DrawOneCard();
        _combatManager.EnemyDeck?.DrawOneCard();
        yield return null;
    }
    #endregion
}