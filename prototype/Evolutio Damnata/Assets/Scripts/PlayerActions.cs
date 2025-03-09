using System.Collections;
using UnityEngine;

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
