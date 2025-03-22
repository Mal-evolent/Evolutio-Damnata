using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManaChecker
{
    private CombatStage combatStage;
    private CardOutlineManager cardOutlineManager;
    private CardManager cardManager;

    public ManaChecker(CombatStage combatStage, CardOutlineManager cardOutlineManager, CardManager cardManager)
    {
        this.combatStage = combatStage;
        this.cardOutlineManager = cardOutlineManager;
        this.cardManager = cardManager;
    }

    public bool HasEnoughMana(CardData cardData)
    {
        if (combatStage.currentMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough mana. Card costs {cardData.ManaCost}, player has {combatStage.currentMana}");
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
            return false;
        }
        return true;
    }

    public void DeductMana(CardData cardData)
    {
        combatStage.currentMana -= cardData.ManaCost;
        UpdateManaUI();
    }

    private void UpdateManaUI()
    {
        combatStage.updateManaUI();
    }
}
