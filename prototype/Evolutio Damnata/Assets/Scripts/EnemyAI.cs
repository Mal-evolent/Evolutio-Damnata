using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField]
    private Deck enemyDeck;

    [SerializeField]
    private CardLibrary cardLibrary;

    [SerializeField]
    private CombatManager combatManager;

    [SerializeField]
    private combatStage combatStage;

    [SerializeField]
    private SpritePositioning spritePositioning;

    // Start is called before the first frame update
    void Start()
    {
        InitializeDeck();
        DrawInitialHand();
    }

    // Update is called once per frame
    void Update()
    {
        // Update logic here
    }

    private void InitializeDeck()
    {
        if (enemyDeck == null)
        {
            Debug.LogError("Enemy deck is not assigned!");
            return;
        }

        enemyDeck.cardLibrary = cardLibrary; // Assign the card library to the deck
        enemyDeck.PopulateDeck();
        Debug.Log("Enemy deck initialized and shuffled.");
    }

    private void DrawInitialHand()
    {
        enemyDeck.DrawCard();
    }
}
