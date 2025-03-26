using UnityEngine;
using UnityEngine.UI;

public class UIManager : IUIManager
{
    private readonly ICombatManager combatManager;

    public UIManager(ICombatManager combatManager)
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