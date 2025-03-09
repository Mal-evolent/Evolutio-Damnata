using System.Collections;
using UnityEngine;


/*
 * The EnemyActions class is responsible for handling the enemy's actions during combat.
 * It contains methods for playing cards and attacking the player.
 */

public class EnemyActions
{
    private CombatManager combatManager;

    public EnemyActions(CombatManager combatManager)
    {
        this.combatManager = combatManager;
    }

    public IEnumerator PlayCards()
    {
        Debug.Log("Enemy Playing Cards");

        // Simulate AI playing cards
        bool cardPlayed = false;
        for (int i = 0; i < combatManager.enemyDeck.Hand.Count; i++)
        {
            Card card = combatManager.enemyDeck.Hand[i];
            if (card != null && card.CardType.IsMonsterCard && combatManager.enemyMana >= card.CardType.ManaCost)
            {
                // Play the card
                combatManager.combatStage.spawnEnemy(card.CardName, i);
                combatManager.enemyDeck.Hand.Remove(card);
                combatManager.enemyMana -= card.CardType.ManaCost;
                cardPlayed = true;
                Debug.Log($"Enemy played card: {card.CardName}");
                break;
            }
        }

        if (!cardPlayed)
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
