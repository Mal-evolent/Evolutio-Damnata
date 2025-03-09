using UnityEngine;
using UnityEngine.UI;

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
