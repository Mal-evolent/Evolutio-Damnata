using System.Collections.Generic;
using UnityEngine;

public class CardSelectionHandler : MonoBehaviour
{
    private CardManager cardManager;
    private CombatManager combatManager;
    private CardOutlineManager cardOutlineManager;
    private SpritePositioning spritePositioning;
    private CombatStage combatStage;

    public void Initialize(CardManager cardManager, CombatManager combatManager, CardOutlineManager cardOutlineManager, SpritePositioning spritePositioning, CombatStage combatStage)
    {
        this.cardManager = cardManager;
        this.combatManager = combatManager;
        this.cardOutlineManager = cardOutlineManager;
        this.spritePositioning = spritePositioning;
        this.combatStage = combatStage;
    }

    public void OnPlayerButtonClick(int index)
    {
        Debug.Log($"Button inside Player Placeholder {index} clicked!");
        EntityManager entityManager = spritePositioning.playerEntities[index].GetComponent<EntityManager>();

        if (cardManager.currentSelectedCard != null && combatManager.playerTurn)
        {
            HandlePlayerCardSelection(index, entityManager);
        }
        else if (cardManager.currentSelectedCard == null)
        {
            if (entityManager != null && entityManager.placed)
            {
                cardManager.currentSelectedCard = spritePositioning.playerEntities[index];
            }
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            cardOutlineManager.RemoveHighlight();
        }
    }

    private void HandlePlayerCardSelection(int index, EntityManager entityManager)
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

        if (combatStage.currentMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough mana. Card costs {cardData.ManaCost}, player has {combatStage.currentMana}");
            cardOutlineManager.RemoveHighlight();
            return; // Bail if there isn't enough mana
        }

        if (cardData.IsMonsterCard)
        {
            combatStage.spawnPlayerCard(cardManager.currentSelectedCard.name, index);

            // Remove card from hand
            List<GameObject> handCardObjects = cardManager.getHandCardObjects();
            foreach (GameObject cardObject in handCardObjects)
            {
                if (cardObject == cardManager.currentSelectedCard)
                {
                    handCardObjects.Remove(cardObject);
                    Destroy(cardObject);
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

        if (combatStage.currentMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough mana. Card costs {cardData.ManaCost}, player has {combatStage.currentMana}");
            cardOutlineManager.RemoveHighlight();
            return; // Bail if there isn't enough mana
        }

        if (entityManager != null && entityManager.placed)
        {
            // Apply spell effect to the placed monster
            Debug.Log($"Applying spell {cardManager.currentSelectedCard.name} to monster {index}");
            SpellCard spellCard = cardManager.currentSelectedCard.GetComponent<SpellCard>();
            if (spellCard == null)
            {
                Debug.LogWarning("SpellCard component not found on current selected card! Adding SpellCard component.");
                spellCard = cardManager.currentSelectedCard.AddComponent<SpellCard>();

                spellCard.CardType = cardData;
                spellCard.CardName = cardData.CardName;
                spellCard.CardImage = cardData.CardImage;
                spellCard.Description = cardData.Description;
                spellCard.ManaCost = cardData.ManaCost;
                spellCard.EffectTypes = cardData.EffectTypes;
                spellCard.EffectValue = cardData.EffectValue;
                spellCard.Duration = cardData.Duration;
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
                    Destroy(cardObject);
                    Debug.Log("Removed card from hand.");
                    break;
                }
            }

            // Also remove the card from the player's deck hand
            cardManager.playerDeck.Hand.Remove(cardComponent);

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

    public void OnEnemyButtonClick(int index)
    {
        Debug.Log($"Button inside Enemy Placeholder {index} clicked!");
        EntityManager entityManager = spritePositioning.enemyEntities[index].GetComponent<EntityManager>();

        if (cardManager.currentSelectedCard != null && combatManager.playerTurn)
        {
            HandleEnemyCardSelection(index, entityManager);
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            cardOutlineManager.RemoveHighlight();
        }

        // Check if a player monster is selected and handle the attack
        if (cardManager.currentSelectedCard != null)
        {
            EntityManager playerEntityManager = cardManager.currentSelectedCard.GetComponent<EntityManager>();
            if (playerEntityManager != null && playerEntityManager.placed)
            {
                if (combatManager.isPlayerCombatPhase)
                {
                    combatStage.HandleMonsterAttack(playerEntityManager, entityManager);
                    cardManager.currentSelectedCard = null;
                }
                else
                {
                    Debug.Log("Attacks are not allowed at this stage!");
                }
            }
            else
            {
                Debug.Log("Player monster not selected or not placed.");
            }
        }
    }

    private void HandleEnemyCardSelection(int index, EntityManager entityManager)
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
                        Destroy(cardObject);
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
