using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundManager : IRoundManager
{
    private readonly ICombatManager _combatManager;
    private readonly IEnemyActions _enemyActions;
    private readonly IUIManager _uiManager;
    private IPhaseManager _phaseManager;
    
    // Add flag to prevent duplicate rounds
    private bool _isProcessingRound = false;

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

    public void SetPhaseManager(IPhaseManager phaseManager)
    {
        _phaseManager = phaseManager ?? throw new ArgumentNullException(nameof(phaseManager));
        Debug.Log("[RoundManager] PhaseManager dependency set");
    }

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
            // _isFirstTurnAfterReset = true;
            // _isSecondTurnAfterReset = false;
            
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
    
    // New method for PhaseManager to call at the end of a round
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

    public void ResetTurnOrder()
    {
        // When resetting the turn order, ensure player goes first
        _combatManager.PlayerGoesFirst = true;
        _combatManager.PlayerTurn = true;
        Debug.Log("[RoundManager] Turn order reset: Player will go first next.");
    }
}