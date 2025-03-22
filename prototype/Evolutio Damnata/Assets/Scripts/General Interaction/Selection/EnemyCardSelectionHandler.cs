using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCardSelectionHandler
{
    private CardManager cardManager;
    private CombatManager combatManager;
    private CardOutlineManager cardOutlineManager;
    private SpritePositioning spritePositioning;
    private CombatStage combatStage;
    private ManaChecker manaChecker;
    private SpellEffectApplier spellEffectApplier;

    public EnemyCardSelectionHandler(CardManager cardManager, CombatManager combatManager, CardOutlineManager cardOutlineManager, SpritePositioning spritePositioning, CombatStage combatStage)
    {
        this.cardManager = cardManager;
        this.combatManager = combatManager;
        this.cardOutlineManager = cardOutlineManager;
        this.spritePositioning = spritePositioning;
        this.combatStage = combatStage;
        this.manaChecker = new ManaChecker(combatStage, cardOutlineManager, cardManager);
        this.spellEffectApplier = new SpellEffectApplier(cardManager);
    }

    public void HandleEnemyCardSelection(int index, EntityManager entityManager)
    {
        CardUI cardUI = cardManager.currentSelectedCard.GetComponent<CardUI>();
        if (cardUI == null && !entityManager.placed)
        {
            Debug.LogError("CardUI component not found on current selected card!");
            return;
        }

        Card cardComponent = cardUI?.card;
        if (cardComponent == null && !entityManager.placed)
        {
            Debug.LogError("Card component not found on current selected card!");
            return;
        }

        CardData selectedCardData = cardComponent?.CardType;
        if (selectedCardData == null && !entityManager.placed)
        {
            Debug.LogError("CardType is null on current selected card!");
            return;
        }

        if (selectedCardData != null && selectedCardData.IsSpellCard)
        {
            if (!manaChecker.HasEnoughMana(selectedCardData))
            {
                return; // Bail if there isn't enough mana
            }

            spellEffectApplier.ApplySpellEffect(entityManager, selectedCardData, index);

            // Deduct mana
            manaChecker.DeductMana(selectedCardData);

            // Remove card from hand
            List<GameObject> handCardObjects = cardManager.getHandCardObjects();
            foreach (GameObject cardObject in handCardObjects)
            {
                if (cardObject == cardManager.currentSelectedCard)
                {
                    handCardObjects.Remove(cardObject);
                    UnityEngine.Object.Destroy(cardObject);
                    Debug.Log("Removed card from hand.");
                    break;
                }
            }

            cardManager.currentSelectedCard = null;
            cardOutlineManager.RemoveHighlight();
        }
        else if (!entityManager.placed)
        {
            Debug.LogError("Card type not found!");
        }
    }
}
