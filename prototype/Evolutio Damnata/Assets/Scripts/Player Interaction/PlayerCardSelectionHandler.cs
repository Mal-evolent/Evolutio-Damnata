using System.Collections.Generic;
using UnityEngine;

public class PlayerCardSelectionHandler
{
    private CardManager cardManager;
    private CombatManager combatManager;
    private CardOutlineManager cardOutlineManager;
    private SpritePositioning spritePositioning;
    private CombatStage combatStage;
    private GeneralEntities playerCardSpawner;
    private ManaChecker manaChecker;
    private SpellEffectApplier spellEffectApplier;

    public PlayerCardSelectionHandler(CardManager cardManager, CombatManager combatManager, CardOutlineManager cardOutlineManager, SpritePositioning spritePositioning, CombatStage combatStage, GeneralEntities playerCardSpawner)
    {
        this.cardManager = cardManager;
        this.combatManager = combatManager;
        this.cardOutlineManager = cardOutlineManager;
        this.spritePositioning = spritePositioning;
        this.combatStage = combatStage;
        this.playerCardSpawner = playerCardSpawner;
        this.manaChecker = new ManaChecker(combatStage, cardOutlineManager, cardManager);
        this.spellEffectApplier = new SpellEffectApplier(cardManager);
    }

    public void HandlePlayerCardSelection(int index, EntityManager entityManager)
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

        CardData cardData = cardComponent?.CardType;
        if (cardData == null && !entityManager.placed)
        {
            Debug.LogError("CardType is null on current selected card!");
            return;
        }

        if (cardData != null && cardData.IsMonsterCard)
        {
            HandleMonsterCardSelection(index);
        }
        else if (cardData != null && cardData.IsSpellCard)
        {
            HandleSpellCardSelection(index, entityManager);
        }
        else if (!entityManager.placed)
        {
            Debug.LogError("Card type not found!");
        }
    }

    private void HandleMonsterCardSelection(int index)
    {
        if (!combatManager.isPlayerPrepPhase)
        {
            Debug.LogError("Cannot spawn monster card outside of the preparation phase.");
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
            return;
        }

        CardUI cardUI = cardManager.currentSelectedCard.GetComponent<CardUI>();
        if (cardUI == null)
        {
            Debug.LogError("CardUI component not found on current selected card!");
            return;
        }

        Card cardComponent = cardUI.card;
        if (cardComponent == null)
        {
            Debug.LogError("Card component not found on current selected card!");
            return;
        }

        CardData cardData = cardComponent.CardType;
        if (cardData == null)
        {
            Debug.LogError("CardType is null on current selected card!");
            return;
        }

        if (!manaChecker.HasEnoughMana(cardData))
        {
            return;
        }

        if (cardData.IsMonsterCard)
        {
            playerCardSpawner.SpawnCards(cardManager.currentSelectedCard.name, index);

            // Deduct mana
            manaChecker.DeductMana(cardData);

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

            // Also remove the card from the player's deck hand
            cardManager.playerDeck.Hand.Remove(cardComponent);

            cardManager.currentSelectedCard = null;

            // Deactivate placeholders
            combatStage.placeHolderActiveState(false);
        }
    }

    private void HandleSpellCardSelection(int index, EntityManager entityManager)
    {
        if (combatManager.isCleanUpPhase)
        {
            Debug.LogError("Cannot play spell cards during the Clean Up phase.");
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
            return;
        }

        CardUI cardUI = cardManager.currentSelectedCard.GetComponent<CardUI>();
        if (cardUI == null)
        {
            Debug.LogError("CardUI component not found on current selected card!");
            return;
        }

        Card cardComponent = cardUI.card;
        if (cardComponent == null)
        {
            Debug.LogError("Card component not found on current selected card!");
            return;
        }

        CardData cardData = cardComponent.CardType;
        if (cardData == null)
        {
            Debug.LogError("CardType is null on current selected card!");
            return;
        }

        if (!manaChecker.HasEnoughMana(cardData))
        {
            return; // Bail if there isn't enough mana
        }

        spellEffectApplier.ApplySpellEffect(entityManager, cardData, index);

        manaChecker.DeductMana(cardData);

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

        // Also remove the card from the player's deck hand
        cardManager.playerDeck.Hand.Remove(cardComponent);

        cardManager.currentSelectedCard = null;
        cardOutlineManager.RemoveHighlight();
    }
}

