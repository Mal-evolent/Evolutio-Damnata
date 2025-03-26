using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundManager : IRoundManager
{
    private readonly ICombatManager _combatManager;
    private readonly IPhaseManager _phaseManager;
    private readonly IEnemyActions _enemyActions;
    private readonly IUIManager _uiManager;

    public RoundManager(
        ICombatManager combatManager,
        IPhaseManager phaseManager,
        IEnemyActions enemyActions,
        IUIManager uiManager)
    {
        _combatManager = combatManager;
        _phaseManager = phaseManager;
        _enemyActions = enemyActions;
        _uiManager = uiManager;
    }

    public void InitializeGame()
    {
        Debug.Log("Initializing Game");
        _uiManager.SetButtonState(_combatManager.EndPhaseButton, true);
        _uiManager.SetButtonState(_combatManager.EndTurnButton, false);

        _combatManager.PlayerTurn = _combatManager.PlayerGoesFirst;
        ((MonoBehaviour)_combatManager).StartCoroutine(RoundStart());
    }

    public IEnumerator RoundStart()
    {
        Debug.Log("Starting New Round");
        _combatManager.TurnCount++;
        _combatManager.TurnUI.text = "turn: " + _combatManager.TurnCount;

        // Set mana values
        _combatManager.CombatStage.maxMana = _combatManager.TurnCount;
        _combatManager.CombatStage.currentMana = _combatManager.CombatStage.maxMana;
        _combatManager.PlayerMana = _combatManager.CombatStage.currentMana;
        _combatManager.EnemyMana = _combatManager.CombatStage.currentMana;

        _combatManager.CombatStage.updateManaUI();

        _combatManager.PlayerTurn = _combatManager.PlayerGoesFirst;
        _combatManager.PlayerGoesFirst = !_combatManager.PlayerGoesFirst;

        _enemyActions.InitializeDeck();

        yield return ((MonoBehaviour)_combatManager).StartCoroutine(_phaseManager.PrepPhase());
    }
}