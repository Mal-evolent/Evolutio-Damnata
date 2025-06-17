using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the rounds and turn order within the combat system.
/// Handles initialization, turn transitions, and coordinating with the PhaseManager.
/// </summary>
public class RoundManager : IRoundManager
{
    private readonly ICombatManager _combatManager;
    private readonly IEnemyActions _enemyActions;
    private readonly IUIManager _uiManager;
    private IPhaseManager _phaseManager;

    // Add flag to prevent duplicate rounds
    private bool _isProcessingRound = false;

    /// <summary>
    /// Initializes a new instance of the RoundManager class.
    /// </summary>
    /// <param name="combatManager">The combat manager responsible for overall combat state</param>
    /// <param name="enemyActions">The enemy actions manager handling enemy behavior</param>
    /// <param name="uiManager">The UI manager for controlling UI elements</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
    public RoundManager(
        ICombatManager combatManager,
        IEnemyActions enemyActions,
        IUIManager uiManager)
    {
        _combatManager = combatManager ?? throw new ArgumentNullException(nameof(combatManager),
            "CombatManager cannot be null. Ensure CombatManager is properly initialized.");

        _enemyActions = enemyActions ?? throw new ArgumentNullException(nameof(enemyActions),
            "EnemyActions cannot be null. Ensure EnemyActions is properly initialized.");

        _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager),
            "UIManager cannot be null. Ensure UIManager is properly initialized.");

        Debug.Log("[RoundManager] Core dependencies initialized (PhaseManager will be set later)");
    }

    /// <summary>
    /// Sets the phase manager dependency for the RoundManager.
    /// This is required before initializing the game.
    /// </summary>
    /// <param name="phaseManager">The phase manager responsible for phase transitions</param>
    /// <exception cref="ArgumentNullException">Thrown when phaseManager is null</exception>
    public void SetPhaseManager(IPhaseManager phaseManager)
    {
        _phaseManager = phaseManager ?? throw new ArgumentNullException(nameof(phaseManager));
        Debug.Log("[RoundManager] PhaseManager dependency set");
    }

    /// <summary>
    /// Initializes the game by setting up the initial turn order and starting the first round.
    /// Sets player to go first and enables appropriate UI elements.
    /// </summary>
    public void InitializeGame()
    {
        Debug.Log("[RoundManager] Initializing game...");
        try
        {
            if (_phaseManager == null)
            {
                Debug.LogError("[RoundManager] PhaseManager not set - call SetPhaseManager first");
                return;
            }

            _uiManager.SetButtonState(_combatManager.EndPhaseButton, true);
            _uiManager.SetButtonState(_combatManager.EndTurnButton, false);

            // Set player to go first when initializing a game and mark it as first turn
            _combatManager.PlayerGoesFirst = true;
            _combatManager.PlayerTurn = true;

            // Reset the processing flag
            _isProcessingRound = false;

            // Start the first round
            StartNextRound();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RoundManager] InitializeGame failed: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Starts the next round of combat.
    /// This method is called by the PhaseManager at the end of a round.
    /// Prevents starting multiple rounds simultaneously using the processing flag.
    /// </summary>
    public void StartNextRound()
    {
        // If already processing a round, don't start another one
        if (_isProcessingRound)
        {
            Debug.LogWarning("[RoundManager] Already processing a round, ignoring StartNextRound request");
            return;
        }

        Debug.Log("[RoundManager] Starting next round...");
        ((MonoBehaviour)_combatManager).StartCoroutine(RoundStart());
    }

    /// <summary>
    /// Coroutine that handles the round start sequence.
    /// Validates references, updates turn count and mana, and initiates the phase cycle.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    public IEnumerator RoundStart()
    {
        // Check if we're already processing a round
        if (_isProcessingRound)
        {
            Debug.LogWarning("[RoundManager] Already processing a round, ignoring RoundStart request");
            yield break;
        }

        _isProcessingRound = true;

        Debug.Log($"[RoundManager] ===== ROUND START - Current Turn: {_combatManager.TurnCount} =====");

        // Phase 1: Validate Managers
        if (!ValidateEssentialReferences())
        {
            Debug.LogError("[RoundManager] Critical references missing - aborting round");
            _isProcessingRound = false;
            yield break;
        }

        // Phase 2: Turn Update
        yield return HandlePhaseSafely(HandleTurnUpdate(), "Turn update failed");

        // Phase 3: Mana Update
        yield return HandlePhaseSafely(HandleManaUpdate(), "Mana update failed");

        // Debug turn state for diagnosis
        Debug.Log($"[RoundManager] Turn {_combatManager.TurnCount} setup complete - " +
                 $"PlayerGoesFirst: {_combatManager.PlayerGoesFirst}, PlayerTurn: {_combatManager.PlayerTurn}");

        // Phase 4: Start the phase cycle from the Prep Phase
        if (_phaseManager != null)
        {
            // Start the phase cycle but don't wait for completion
            ((MonoBehaviour)_combatManager).StartCoroutine(_phaseManager.PrepPhase());
        }
        else
        {
            Debug.LogError("[RoundManager] PhaseManager is null, cannot start phase cycle");
        }

        Debug.Log("[RoundManager] ===== ROUND INITIALIZATION COMPLETE =====");

        // Reset the processing flag after the round has been started
        // We do this here since PrepPhase runs asynchronously
        _isProcessingRound = false;
    }

    /// <summary>
    /// Safely executes a phase coroutine, handling any errors that occur during execution.
    /// </summary>
    /// <param name="phase">The phase coroutine to execute</param>
    /// <param name="errorMessage">Error message to display if execution fails</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator HandlePhaseSafely(IEnumerator phase, string errorMessage)
    {
        if (phase == null)
        {
            Debug.LogError($"[RoundManager] {errorMessage}: phase enumerator is null");
            yield break;
        }

        while (true)
        {
            bool moveNext;
            try
            {
                moveNext = phase.MoveNext();
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoundManager] {errorMessage}: {e.Message}");
                yield break;
            }

            if (!moveNext)
                break;

            yield return phase.Current;
        }
    }

    /// <summary>
    /// Validates that all essential references are properly set.
    /// </summary>
    /// <returns>True if all required dependencies are valid, false otherwise</returns>
    private bool ValidateEssentialReferences()
    {
        bool isValid = true;

        if (_combatManager == null)
        {
            Debug.LogError("[RoundManager] CRITICAL: CombatManager reference missing");
            isValid = false;
        }

        if (_phaseManager == null)
        {
            Debug.LogError("[RoundManager] CRITICAL: PhaseManager reference missing");
            isValid = false;
        }

        if (_enemyActions == null)
        {
            Debug.LogError("[RoundManager] CRITICAL: EnemyActions reference missing");
            isValid = false;
        }

        if (!(_combatManager is MonoBehaviour))
        {
            Debug.LogError("[RoundManager] CRITICAL: CombatManager is not a MonoBehaviour");
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Updates the turn counter and UI elements for the new turn.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator HandleTurnUpdate()
    {
        Debug.Log("[RoundManager] -- Updating Turn --");

        _combatManager.TurnCount++;
        Debug.Log($"[RoundManager] Turn {_combatManager.TurnCount} started");

        if (_combatManager.TurnUI != null)
        {
            _combatManager.TurnUI.text = $"Turn: {_combatManager.TurnCount}";
            _combatManager.TurnUIShadow.text = $"Turn: {_combatManager.TurnCount}";
        }
        else
        {
            Debug.LogWarning("[RoundManager] TurnUI reference missing");
        }

        yield return null;
    }

    /// <summary>
    /// Updates mana values for both players based on the turn count.
    /// Also handles turn alternation between player and enemy.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator HandleManaUpdate()
    {
        Debug.Log("[RoundManager] -- Updating Mana --");

        _combatManager.MaxMana = _combatManager.TurnCount;
        _combatManager.PlayerMana = _combatManager.MaxMana;
        _combatManager.EnemyMana = _combatManager.MaxMana;
        Debug.Log($"[RoundManager] Mana set to {_combatManager.MaxMana}");

        _combatManager.PlayerTurn = _combatManager.PlayerGoesFirst;

        // Normal alternation for third turn onwards
        _combatManager.PlayerGoesFirst = !_combatManager.PlayerGoesFirst;
        Debug.Log($"[RoundManager] Turn order updated - Next turn, player first: {_combatManager.PlayerGoesFirst}");

        yield return null;
    }

    /// <summary>
    /// Resets the turn order to have the player go first.
    /// Used when restarting combat or after specific game events.
    /// </summary>
    public void ResetTurnOrder()
    {
        // When resetting the turn order, ensure player goes first
        _combatManager.PlayerGoesFirst = true;
        _combatManager.PlayerTurn = true;
        Debug.Log("[RoundManager] Turn order reset: Player will go first next.");
    }
}