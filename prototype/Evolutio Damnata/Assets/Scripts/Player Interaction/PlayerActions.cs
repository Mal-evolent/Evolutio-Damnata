using UnityEngine;

public class PlayerActions : IPlayerActions
{
    private readonly ICombatManager _combatManager;

    public bool PlayerTurnEnded { get; set; } = false;

    public PlayerActions(ICombatManager combatManager)
    {
        _combatManager = combatManager;
    }

    public void EndTurn()
    {
        Debug.Log("Ending Turn");
        PlayerTurnEnded = true;
    }
}