using UnityEngine;
using UnityEngine.UI;

/**
 * The UIManager class is responsible for managing the UI elements of the game.
 * It handles the visibility of the buttons and other UI elements.
 */

public class UIManager
{
    private CombatManager combatManager;

    public UIManager(CombatManager combatManager)
    {
        this.combatManager = combatManager;
    }

    public void SetButtonState(Button button, bool state)
    {
        if (button != null)
        {
            button.gameObject.SetActive(state);
        }
    }
}
