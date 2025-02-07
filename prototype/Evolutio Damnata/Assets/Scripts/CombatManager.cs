using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public int playerMana;
    public int maxPlayerMana;
    public int enemyMana;
    public int maxEnemyMana;
    public int playerHealth;
    public int enemyHealth;
    public int storedMana;
    public int maxStoredMana = 2;
    public bool playerTurn = true;

    public Deck playerDeck;
    public Deck enemyDeck;
    public CardManager cardManager;
    public CardLibrary cardLibrary;
    public SpritePositioning spritePositioning;
    public DamageVisualizer damageVisualizer;

    [SerializeField]
    public GameObject manaBar;
    [SerializeField]
    public GameObject manaText;
    public TMP_Text turnText;
    public GameObject damageNumberPrefab;

    void Start()
    {
        // Initialize game state
        InitializeGame();
    }

    void Update()
    {
        // Additional update logic if needed
    }

    void InitializeGame()
    {
        playerMana = 0;
        maxPlayerMana = 1;
        enemyMana = 0;
        maxEnemyMana = 1;
        playerHealth = 30; // Example starting health
        enemyHealth = 30; // Example starting health
        storedMana = 0;

        // Populate decks
        playerDeck.PopulateDeck();
        enemyDeck.PopulateDeck();

        // Initialize DamageVisualizer
        damageVisualizer = new DamageVisualizer();

        // Start the first turn
        StartCoroutine(PrepPhase());
    }

    IEnumerator PrepPhase()
    {
        // Increase max mana and gain mana
        maxPlayerMana++;
        playerMana = maxPlayerMana;
        maxEnemyMana++;
        enemyMana = maxEnemyMana;

        // Update mana UI
        UpdateManaUI();

        // Allow players to play cards
        // Example: player plays cards
        yield return StartCoroutine(PlayerPlayCards());

        // Example: enemy plays cards
        yield return StartCoroutine(EnemyPlayCards());

        // Proceed to Combat Phase
        StartCoroutine(CombatPhase());
    }

    IEnumerator PlayerPlayCards()
    {
        // Logic for player to play cards
        // Example: wait for player input
        yield return new WaitForSeconds(2); // Placeholder for player input
    }

    IEnumerator EnemyPlayCards()
    {
        // Logic for enemy to play cards
        // Example: AI plays cards
        yield return new WaitForSeconds(2); // Placeholder for enemy AI
    }

    IEnumerator CombatPhase()
    {
        // Determine attack order
        if (playerTurn)
        {
            // Player attacks first
            yield return StartCoroutine(PlayerAttack());
            yield return StartCoroutine(EnemyAttack());
        }
        else
        {
            // Enemy attacks first
            yield return StartCoroutine(EnemyAttack());
            yield return StartCoroutine(PlayerAttack());
        }

        // Toggle turn order
        playerTurn = !playerTurn;

        // Proceed to Clean-Up Phase
        StartCoroutine(CleanUpPhase());
    }

    IEnumerator PlayerAttack()
    {
        // Logic for player attacks
        for (int i = 0; i < spritePositioning.playerEntities.Count; i++)
        {
            if (spritePositioning.playerEntities[i] != null)
            {
                EntityManager playerEntity = spritePositioning.playerEntities[i].GetComponent<EntityManager>();
                if (playerEntity != null && playerEntity.placed)
                {
                    // Find a corresponding enemy entity to attack
                    if (i < spritePositioning.enemyEntities.Count && spritePositioning.enemyEntities[i] != null)
                    {
                        EntityManager enemyEntity = spritePositioning.enemyEntities[i].GetComponent<EntityManager>();
                        if (enemyEntity != null && enemyEntity.placed)
                        {
                            // Perform attack
                            float damage = playerEntity.getAttackDamage();
                            enemyEntity.takeDamage(damage);
                            damageVisualizer.createDamageNumber(this, damage, enemyEntity.transform.position, damageNumberPrefab);
                        }
                    }
                    else
                    {
                        // No enemy entity to attack, deal damage to enemy health
                        float damage = playerEntity.getAttackDamage();
                        enemyHealth -= (int)damage; // Explicit cast to int
                        damageVisualizer.createDamageNumber(this, damage, new Vector3(0, 0, 0), damageNumberPrefab); // Adjust position as needed
                    }
                }
            }
        }
        yield return new WaitForSeconds(1); // Placeholder for attack resolution
    }

    IEnumerator EnemyAttack()
    {
        // Logic for enemy attacks
        for (int i = 0; i < spritePositioning.enemyEntities.Count; i++)
        {
            if (spritePositioning.enemyEntities[i] != null)
            {
                EntityManager enemyEntity = spritePositioning.enemyEntities[i].GetComponent<EntityManager>();
                if (enemyEntity != null && enemyEntity.placed)
                {
                    // Find a corresponding player entity to attack
                    if (i < spritePositioning.playerEntities.Count && spritePositioning.playerEntities[i] != null)
                    {
                        EntityManager playerEntity = spritePositioning.playerEntities[i].GetComponent<EntityManager>();
                        if (playerEntity != null && playerEntity.placed)
                        {
                            // Perform attack
                            float damage = enemyEntity.getAttackDamage();
                            playerEntity.takeDamage(damage);
                            damageVisualizer.createDamageNumber(this, damage, playerEntity.transform.position, damageNumberPrefab);
                        }
                    }
                    else
                    {
                        // No player entity to attack, deal damage to player health
                        float damage = enemyEntity.getAttackDamage();
                        playerHealth -= (int)damage; // Explicit cast to int
                        damageVisualizer.createDamageNumber(this, damage, new Vector3(0, 0, 0), damageNumberPrefab); // Adjust position as needed
                    }
                }
            }
        }
        yield return new WaitForSeconds(1); // Placeholder for attack resolution
    }

    IEnumerator CleanUpPhase()
    {
        // Resolve lingering effects
        yield return StartCoroutine(ResolveLingeringEffects());

        // Check for Last Stand
        if (playerDeck.Hand.Count == 0)
        {
            // Player draws an extra card at the cost of increased future card costs
            yield return StartCoroutine(LastStand());
        }

        // Update game state
        UpdateGameState();

        // Start the next turn
        StartCoroutine(PrepPhase());
    }

    IEnumerator ResolveLingeringEffects()
    {
        // Logic to resolve burn, poison, curses, etc.
        yield return new WaitForSeconds(1); // Placeholder for effect resolution
    }

    IEnumerator LastStand()
    {
        // Logic for Last Stand mechanic
        yield return new WaitForSeconds(1); // Placeholder for Last Stand resolution
    }

    void UpdateGameState()
    {
        // Update game state, check for game over conditions, etc.
    }

    void UpdateManaUI()
    {
        manaBar.GetComponent<Slider>().value = playerMana;
        manaText.GetComponent<TMP_Text>().text = playerMana.ToString();
    }
}
