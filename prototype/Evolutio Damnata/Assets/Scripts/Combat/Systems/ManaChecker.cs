using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManaChecker
{
    private IManaProvider manaProvider;
    private CardOutlineManager cardOutlineManager;
    private CardManager cardManager;

    public ManaChecker(IManaProvider manaProvider, CardOutlineManager cardOutlineManager, CardManager cardManager)
    {
        this.manaProvider = manaProvider;
        this.cardOutlineManager = cardOutlineManager;
        this.cardManager = cardManager;
    }

    public bool HasEnoughPlayerMana(CardData cardData)
    {
        if (manaProvider.PlayerMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough player mana. Card costs {cardData.ManaCost}, player has {manaProvider.PlayerMana}");
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
            return false;
        }
        return true;
    }

    public void DeductPlayerMana(CardData cardData)
    {
        manaProvider.PlayerMana -= cardData.ManaCost;
    }

    public bool HasEnoughEnemyMana(CardData cardData)
    {
        if (manaProvider.EnemyMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough enemy mana. Card costs {cardData.ManaCost}, enemy has {manaProvider.EnemyMana}");
            return false;
        }
        return true;
    }

    public void DeductEnemyMana(CardData cardData)
    {
        manaProvider.EnemyMana -= cardData.ManaCost;
    }
}