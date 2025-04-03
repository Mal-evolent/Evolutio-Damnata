using UnityEngine;
using UnityEngine.UI;

public class UIManager : IUIManager
{
    private readonly ICombatManager combatManager;
    private Image buttonShadow;

    public UIManager(ICombatManager combatManager)
    {
        this.combatManager = combatManager;
    }

    public void SetButtonState(Button button, bool state)
    {
        if (button != null)
        {
            button.gameObject.SetActive(state);
            buttonShadow = button.gameObject.GetComponentInParent<Image>();
            buttonShadow.gameObject.SetActive(state);
            Debug.LogError($"==UI MANAGER== Button Name {buttonShadow.gameObject.name}");
        }
    }
}