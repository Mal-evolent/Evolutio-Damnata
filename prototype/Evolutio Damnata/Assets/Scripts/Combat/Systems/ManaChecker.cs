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

    // Check if the player has enough mana
    public bool HasEnoughPlayerMana(CardData cardData)
    {
        if (combatStage.currentMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough player mana. Card costs {cardData.ManaCost}, player has {combatStage.currentMana}");
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
            return false;
        }
        return true;
    }

    // Deduct mana from the player
    public void DeductPlayerMana(CardData cardData)
    {
        combatStage.currentMana -= cardData.ManaCost;
        combatStage.combatManager.playerMana = combatStage.currentMana;
        UpdateManaUI();
    }

    // Check if the enemy has enough mana
    public bool HasEnoughEnemyMana(CardData cardData)
    {
        if (combatStage.combatManager.enemyMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough enemy mana. Card costs {cardData.ManaCost}, enemy has {combatStage.combatManager.enemyMana}");
            return false;
        }
        return true;
    }

    // Deduct mana from the enemy
    public void DeductEnemyMana(CardData cardData)
    {
        combatStage.combatManager.enemyMana -= cardData.ManaCost;
    }

    private void UpdateManaUI()
    {
        combatStage.updateManaUI();
    }
}
