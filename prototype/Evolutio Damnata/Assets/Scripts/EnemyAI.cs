using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        StartCoroutine(InitializeDeckWhenReady());
    }

    // Update is called once per frame
    void Update()
    {
        // Update logic here
    }

    private IEnumerator InitializeDeckWhenReady()
    {
        // Wait until the room is ready
        while (!spritePositioning.roomReady)
        {
            yield return null; // Wait for the next frame
        }

        InitializeDeck();
        DrawInitialHand();
        StartCoroutine(PrepPhase());
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

    private IEnumerator PrepPhase()
    {
        yield return new WaitUntil(() => combatManager.isEnemyPrepPhase == true);
        SpawnRandomMonsterCards();
    }

    private void SpawnRandomMonsterCards()
    {
        List<PositionData> enemyPositions = spritePositioning.GetEnemyPositionsForCurrentRoom();
        for (int i = 0; i < enemyPositions.Count; i++)
        {
            bool cardPlayed = false;

            if (enemyDeck.Hand.Count > 0)
            {
                // Filter out spell cards
                List<Card> monsterCards = enemyDeck.Hand.FindAll(card => !(card is SpellCard));
                if (monsterCards.Count > 0)
                {
                    int randomIndex = Random.Range(0, monsterCards.Count);
                    Card randomCard = monsterCards[randomIndex];
                    enemyDeck.Hand.Remove(randomCard);
                    combatStage.spawnEnemy(randomCard.CardName, i);
                    cardPlayed = true;
                }
                else
                {
                    Debug.LogWarning("No monster cards available to spawn!");
                }
            }
            else
            {
                Debug.LogWarning("Enemy hand is empty!");
            }

            // If no card was played, deactivate the parent button
            if (!cardPlayed)
            {
                GameObject enemyPlaceholder = spritePositioning.enemyEntities[i];
                if (enemyPlaceholder != null)
                {
                    Button parentButton = enemyPlaceholder.GetComponentInParent<Button>();
                    if (parentButton != null)
                    {
                        parentButton.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
