using UnityEngine;

/*
 * The PlayerActions class is responsible for managing the player's actions during the combat phase.
 * It keeps track of the player's turn state and provides methods for the player to end their turn.
 */

public class PlayerActions
{
    private CombatManager combatManager;
    public bool playerTurnEnded = false;

    public PlayerActions(CombatManager combatManager)
    {
        this.combatManager = combatManager;
    }

    public void EndTurn()
    {
        Debug.Log("Ending Turn");
        playerTurnEnded = true;
    }
}
