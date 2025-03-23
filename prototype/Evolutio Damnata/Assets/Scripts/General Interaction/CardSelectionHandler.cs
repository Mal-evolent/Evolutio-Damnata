using System.Collections.Generic;
using UnityEngine;

public class CardSelectionHandler : MonoBehaviour
{
    private CardManager cardManager;
    private CombatManager combatManager;
    private CardOutlineManager cardOutlineManager;
    private SpritePositioning spritePositioning;
    private CombatStage combatStage;
    private GeneralEntities playerCardSpawner;
    private PlayerCardSelectionHandler playerCardSelectionHandler;
    private EnemyCardSelectionHandler enemyCardSelectionHandler;

    public void Initialize(CardManager cardManager, CombatManager combatManager, CardOutlineManager cardOutlineManager, SpritePositioning spritePositioning, CombatStage combatStage, GeneralEntities playerCardSpawner)
    {
        this.cardManager = cardManager;
        this.combatManager = combatManager;
        this.cardOutlineManager = cardOutlineManager;
        this.spritePositioning = spritePositioning;
        this.combatStage = combatStage;
        this.playerCardSpawner = playerCardSpawner;
        this.playerCardSelectionHandler = new PlayerCardSelectionHandler(cardManager, combatManager, cardOutlineManager, spritePositioning, combatStage, playerCardSpawner);
        this.enemyCardSelectionHandler = new EnemyCardSelectionHandler(cardManager, combatManager, cardOutlineManager, spritePositioning, combatStage);
    }

    public void OnPlayerButtonClick(int index)
    {
        Debug.Log($"Button inside Player Placeholder {index} clicked!");
        EntityManager entityManager = spritePositioning.playerEntities[index].GetComponent<EntityManager>();

        if (cardManager.currentSelectedCard != null && combatManager.playerTurn)
        {
            playerCardSelectionHandler.HandlePlayerCardSelection(index, entityManager);
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

    public void OnEnemyButtonClick(int index)
    {
        Debug.Log($"Button inside Enemy Placeholder {index} clicked!");
        EntityManager entityManager = spritePositioning.enemyEntities[index].GetComponent<EntityManager>();

        if (cardManager.currentSelectedCard != null && combatManager.playerTurn)
        {
            enemyCardSelectionHandler.HandleEnemyCardSelection(index, entityManager);
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
}

