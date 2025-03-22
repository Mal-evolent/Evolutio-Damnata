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

    public EnemyCardSelectionHandler(CardManager cardManager, CombatManager combatManager, CardOutlineManager cardOutlineManager, SpritePositioning spritePositioning, CombatStage combatStage)
    {
        this.cardManager = cardManager;
        this.combatManager = combatManager;
        this.cardOutlineManager = cardOutlineManager;
        this.spritePositioning = spritePositioning;
        this.combatStage = combatStage;
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
            if (combatStage.currentMana < selectedCardData.ManaCost)
            {
                Debug.LogError($"Not enough mana. Card costs {selectedCardData.ManaCost}, player has {combatStage.currentMana}");
                cardOutlineManager.RemoveHighlight();
                cardManager.currentSelectedCard = null;
                return;
            }

            if (entityManager != null && entityManager.placed)
            {
                // Apply spell effect to the enemy monster
                Debug.Log($"Applying spell {cardManager.currentSelectedCard.name} to enemy monster {index}");
                SpellCard spellCard = cardManager.currentSelectedCard.GetComponent<SpellCard>();
                if (spellCard == null)
                {
                    Debug.LogWarning("SpellCard component not found on current selected card! Adding SpellCard component.");
                    spellCard = cardManager.currentSelectedCard.AddComponent<SpellCard>();

                    // Copy properties from CardData to SpellCard
                    spellCard.CardName = selectedCardData.CardName;
                    spellCard.CardImage = selectedCardData.CardImage;
                    spellCard.Description = selectedCardData.Description;
                    spellCard.ManaCost = selectedCardData.ManaCost;
                    spellCard.EffectTypes = selectedCardData.EffectTypes;
                    spellCard.EffectValue = selectedCardData.EffectValue;
                    spellCard.Duration = selectedCardData.Duration;
                }
                spellCard.targetEntity = entityManager;
                spellCard.Play();

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
            else
            {
                Debug.Log("Spells cannot be placed on the field.");
                cardManager.currentSelectedCard = null;
                cardOutlineManager.RemoveHighlight();
            }
        }
        else if (!entityManager.placed)
        {
            Debug.LogError("Card type not found!");
        }
    }
}
