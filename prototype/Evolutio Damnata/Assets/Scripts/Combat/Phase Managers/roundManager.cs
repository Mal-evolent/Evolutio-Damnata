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

            _combatManager.PlayerTurn = _combatManager.PlayerGoesFirst;
            ((MonoBehaviour)_combatManager).StartCoroutine(RoundStart());
        }
        catch (Exception e)
        {
            Debug.LogError($"[RoundManager] InitializeGame failed: {e.Message}");
            Debug.LogException(e);
        }
    }

    public IEnumerator RoundStart()
    {
        Debug.Log("[RoundManager] ===== ROUND START =====");

        // Phase 1: Validate Managers
        if (!ValidateEssentialReferences())
        {
            Debug.LogError("[RoundManager] Critical references missing - aborting round");
            yield break;
        }

        // Phase 2: Turn Update
        yield return HandlePhaseSafely(HandleTurnUpdate(), "Turn update failed");

        // Phase 3: Mana Update
        yield return HandlePhaseSafely(HandleManaUpdate(), "Mana update failed");

        // Phase 5: Prep Phase
        yield return HandlePhaseSafely(HandlePrepPhase(), "Prep phase failed");

        Debug.Log("[RoundManager] ===== ROUND COMPLETE =====");
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
        _combatManager.PlayerGoesFirst = !_combatManager.PlayerGoesFirst;
        Debug.Log($"[RoundManager] Turn order updated");

        yield return null;
    }

    private IEnumerator HandlePrepPhase()
    {
        Debug.Log("[RoundManager] -- Starting Prep Phase --");

        if (_phaseManager == null)
        {
            Debug.LogError("[RoundManager] PhaseManager is null in HandlePrepPhase");
            yield break;
        }

        yield return _phaseManager.PrepPhase();
    }
}