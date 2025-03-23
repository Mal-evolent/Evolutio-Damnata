using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


/*
 * The EnemyActions class is responsible for handling the enemy's actions during combat.
 * It contains methods for playing cards and attacking the player.
 */

public class EnemyActions
{
    private CombatManager combatManager;
    private SpritePositioning spritePositioning;
    private Deck enemyDeck;
    private CardLibrary cardLibrary;
    private CombatStage combatStage;

    public EnemyActions(CombatManager combatManager, SpritePositioning spritePositioning, Deck enemyDeck, CardLibrary cardLibrary, CombatStage combatStage)
    {
        this.combatManager = combatManager;
        this.spritePositioning = spritePositioning;
        this.enemyDeck = enemyDeck;
        this.cardLibrary = cardLibrary;
        this.combatStage = combatStage;
    }

    public void InitializeDeck()
    {
        if (enemyDeck == null)
        {
            Debug.LogError("Enemy deck is not assigned!");
            return;
        }

        enemyDeck.cardLibrary = cardLibrary;
        enemyDeck.PopulateDeck();
        Debug.Log("Enemy deck initialized and shuffled.");
    }


    public IEnumerator PlayCards()
    {
        Debug.Log("Enemy Playing Cards");

        // Simulate AI playing cards
        for (int i = 0; i < combatManager.enemyDeck.Hand.Count; i++)
        {
            Card card = combatManager.enemyDeck.Hand[i];
            if (card != null && card.CardType.IsMonsterCard && combatManager.enemyMana >= card.CardType.ManaCost)
            {
                // Play the card
                combatManager.combatStage.spawnEnemy(card.CardName, i);
                Debug.Log($"Enemy played card: {card.CardName}");

                // Check if an enemy card was played
                if (combatManager.combatStage.enemyCardSpawner.enemyCardPlayed)
                {
                    combatManager.enemyDeck.Hand.Remove(card);
                }
                break;
            }
        }

        // Check if no enemy card was played
        if (!combatManager.combatStage.enemyCardSpawner.enemyCardPlayed)
        {
            Debug.Log("Enemy did not play any cards.");
        }

        yield return new WaitForSeconds(2);
    }

    public IEnumerator Attack()
    {
        Debug.Log("Enemy Attacks");
        yield return new WaitForSeconds(2);
    }
}
