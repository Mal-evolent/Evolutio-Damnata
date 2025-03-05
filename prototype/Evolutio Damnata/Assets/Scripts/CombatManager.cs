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
    public Button endTurnButton;

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
        //TESTING ONLY

        //playerTurn = true;
    }

    void InitializeGame()
    {
        playerMana = 0;
        maxPlayerMana = 1;
        enemyMana = 0;
        maxEnemyMana = 1;
        playerHealth = 30;
        enemyHealth = 30; 
        storedMana = 0;

        // Populate decks
        playerDeck.PopulateDeck();
        enemyDeck.PopulateDeck();

        // Reference the DamageVisualizer component
        damageVisualizer = FindObjectOfType<DamageVisualizer>();
        if (damageVisualizer == null)
        {
            Debug.LogError("DamageVisualizer not found in the scene.");
        }

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

        // Proceed to Clean-Up Phase
        StartCoroutine(CleanUpPhase());
    }

    IEnumerator PlayerAttack()
    {
        yield return new WaitForSeconds(1);
    }

    IEnumerator EnemyAttack()
    {
        yield return new WaitForSeconds(1); 
    }

    IEnumerator CleanUpPhase()
    {
        // Resolve lingering effects
        yield return StartCoroutine(ResolveLingeringEffects());

    }

    IEnumerator ResolveLingeringEffects()
    {
        // Logic to resolve burn, poison, curses, etc.
        yield return new WaitForSeconds(1); // Placeholder for effect resolution
    }

    public void EndTurn()
    {
        // End the current turn
        playerTurn = !playerTurn;
        // Disable end turn button
        endTurnButton.interactable = false;
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
