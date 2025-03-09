using System.Collections;
using UnityEngine;

public class GameStateManager
{
    private CombatManager combatManager;

    public GameStateManager(CombatManager combatManager)
    {
        this.combatManager = combatManager;
    }

    public void InitializeGame()
    {
        Debug.Log("Initializing Game");
        combatManager.uiManager.SetButtonState(combatManager.endPhaseButton, true);
        combatManager.uiManager.SetButtonState(combatManager.endTurnButton, false);

        combatManager.playerTurn = combatManager.playerGoesFirst;

        combatManager.StartCoroutine(RoundStart());
    }

    public IEnumerator RoundStart()
    {
        Debug.Log("Starting New Round");
        combatManager.combatStage.currentMana++;
        combatManager.turnCount++;
        combatManager.turnUI.text = "turn: " + combatManager.turnCount;
        combatManager.playerMana = combatManager.combatStage.currentMana;
        combatManager.enemyMana = combatManager.combatStage.currentMana;

        combatManager.combatStage.updateManaUI();

        combatManager.playerTurn = combatManager.playerGoesFirst;
        combatManager.playerGoesFirst = !combatManager.playerGoesFirst;

        yield return combatManager.StartCoroutine(combatManager.phaseManager.PrepPhase());
    }
}
