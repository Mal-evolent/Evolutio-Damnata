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
            Transform parentTransform = button.transform.parent;

            while (parentTransform != null)
            {
                buttonShadow = parentTransform.GetComponent<Image>();
                if (buttonShadow != null)
                {
                    buttonShadow.gameObject.SetActive(state);
                    break;
                }
                parentTransform = parentTransform.parent;
            }

            if (buttonShadow == null)
            {
                Debug.LogError("==UI MANAGER== Button shadow not found.");
            }
        }
        else
        {
            Debug.LogError("==UI MANAGER== Button is null.");
        }
    }
}