using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public combatStage combatStage;
    public TMP_Text turnUI;

    public int turnCount = 0;
    public int playerMana = 0;
    public int enemyMana = 0;
    public int playerHealth = 30;
    public int enemyHealth = 30;

    public Button endPhaseButton;
    public Button endTurnButton;

    public bool playerGoesFirst = true;
    public bool playerTurn;

    public bool isPlayerPrepPhase = false;
    public bool isPlayerCombatPhase = false;
    public bool isEnemyPrepPhase = false;
    public bool isEnemyCombatPhase = false;
    public bool isCleanUpPhase = false;

    public Deck playerDeck;
    public Deck enemyDeck;

    //used for phase tracking
    private bool isPlayerTurn;
    private bool playerTurnEnded = false;

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        Debug.Log("Initializing Game");
        SetButtonState(endPhaseButton, true);
        SetButtonState(endTurnButton, false);

        // Set isPlayerTurn based on playerGoesFirst
        isPlayerTurn = playerGoesFirst;

        StartCoroutine(RoundStart());
    }

    private IEnumerator RoundStart()
    {
        Debug.Log("Starting New Round");
        combatStage.currentMana++;
        turnCount++;
        turnUI.text = "turn: " + turnCount;
        playerMana = combatStage.currentMana;
        enemyMana = combatStage.currentMana;

        combatStage.updateManaUI();

        isPlayerTurn = playerGoesFirst;
        playerGoesFirst = !playerGoesFirst;

        yield return StartCoroutine(PrepPhase());
    }

    private IEnumerator PrepPhase()
    {
        Debug.Log("Entering Prep Phase");
        ResetPhaseStates();

        if (isPlayerTurn)
        {
            isPlayerPrepPhase = true;
            Debug.Log("Player's Prep Phase");
            SetButtonState(endPhaseButton, true);
            yield return new WaitUntil(() => endPhaseButton.gameObject.activeSelf == false);

            isPlayerPrepPhase = false;
            isPlayerTurn = false;
            isEnemyPrepPhase = true;
            Debug.Log("Enemy's Prep Phase");
            yield return StartCoroutine(EnemyPlayCards());
            isEnemyPrepPhase = false;
        }
        else
        {
            isEnemyPrepPhase = true;
            Debug.Log("Enemy's Prep Phase");
            yield return StartCoroutine(EnemyPlayCards());
            isEnemyPrepPhase = false;

            isPlayerPrepPhase = true;
            isPlayerTurn = true;
            Debug.Log("Player's Prep Phase");
            SetButtonState(endPhaseButton, true);
            yield return new WaitUntil(() => endPhaseButton.gameObject.activeSelf == false);
            isPlayerPrepPhase = false;
        }

        yield return StartCoroutine(CombatPhase());
    }

    private IEnumerator EnemyPlayCards()
    {
        Debug.Log("Enemy Playing Cards");
        yield return new WaitForSeconds(2);
    }

    public void EndPhase()
    {
        Debug.Log("Ending Prep Phase");
        SetButtonState(endPhaseButton, false);
    }

    private IEnumerator CombatPhase()
    {
        Debug.Log("Entering Combat Phase");
        ResetPhaseStates();

        if (isPlayerTurn)
        {
            isPlayerCombatPhase = true;
            Debug.Log("Player Attacks - Start");
            SetButtonState(endTurnButton, true);
            playerTurnEnded = false;
            yield return new WaitUntil(() => playerTurnEnded);
            isPlayerCombatPhase = false;
            Debug.Log("Player Attacks - End");

            isPlayerTurn = false;

            isEnemyCombatPhase = true;
            Debug.Log("Enemy Attacks - Start");
            yield return StartCoroutine(EnemyAttack());
            isEnemyCombatPhase = false;
            Debug.Log("Enemy Attacks - End");
        }
        else
        {
            isEnemyCombatPhase = true;
            Debug.Log("Enemy Attacks - Start");
            yield return StartCoroutine(EnemyAttack());
            isEnemyCombatPhase = false;
            Debug.Log("Enemy Attacks - End");

            isPlayerTurn = true;

            isPlayerCombatPhase = true;
            Debug.Log("Player Attacks - Start");
            SetButtonState(endTurnButton, true);
            playerTurnEnded = false;
            yield return new WaitUntil(() => playerTurnEnded);
            isPlayerCombatPhase = false;
            Debug.Log("Player Attacks - End");
        }

        yield return StartCoroutine(CleanUpPhase());
    }

    private IEnumerator EnemyAttack()
    {
        Debug.Log("Enemy Attacks");
        yield return new WaitForSeconds(2);
    }

    public void EndTurn()
    {
        Debug.Log("Ending Turn");
        SetButtonState(endTurnButton, false);
        playerTurnEnded = true;
    }

    private IEnumerator CleanUpPhase()
    {
        Debug.Log("Entering Clean-Up Phase");
        isCleanUpPhase = true;
        playerDeck.DrawOneCard();
        enemyDeck.DrawOneCard();
        yield return new WaitForSeconds(1);
        StartCoroutine(RoundStart());
        ResetPhaseStates();
    }

    private void ResetPhaseStates()
    {
        isPlayerPrepPhase = false;
        isPlayerCombatPhase = false;
        isEnemyPrepPhase = false;
        isEnemyCombatPhase = false;
        isCleanUpPhase = false;
    }

    private void SetButtonState(Button button, bool state)
    {
        if (button != null)
        {
            button.gameObject.SetActive(state);
        }
    }

    public void UpdateMana(int playerManaChange, int enemyManaChange)
    {
        playerMana = Mathf.Clamp(playerMana + playerManaChange, 0, combatStage.currentMana);
        enemyMana = Mathf.Clamp(enemyMana + enemyManaChange, 0, combatStage.currentMana);
    }
}
