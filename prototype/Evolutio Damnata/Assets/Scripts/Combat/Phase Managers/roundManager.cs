using System.Collections;
using UnityEngine;


/*
 * The roundManager class is responsible for managing the game state.
 * It keeps track of the combat manager and initializes the game.
 */

public class roundManager
{
    private CombatManager combatManager;

    public roundManager(CombatManager combatManager)
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
        combatManager.turnCount++;
        combatManager.turnUI.text = "turn: " + combatManager.turnCount;

        // Set maxMana to the current turn count
        combatManager.combatStage.maxMana = combatManager.turnCount;

        // Set currentMana to maxMana
        combatManager.combatStage.currentMana = combatManager.combatStage.maxMana;

        // Update player and enemy mana
        combatManager.playerMana = combatManager.combatStage.currentMana;
        combatManager.enemyMana = combatManager.combatStage.currentMana;

        // Update the mana UI
        combatManager.combatStage.updateManaUI();

        combatManager.playerTurn = combatManager.playerGoesFirst;
        combatManager.playerGoesFirst = !combatManager.playerGoesFirst;

        // Initialize the enemy deck
        combatManager.enemyActions.InitializeDeck();

        yield return combatManager.StartCoroutine(combatManager.phaseManager.PrepPhase());
    }
}
