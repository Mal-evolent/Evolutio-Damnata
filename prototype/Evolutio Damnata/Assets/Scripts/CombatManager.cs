using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public int playerMana = 1;
    public int enemyMana = 1;
    public int maxMana = 1;
    public int playerHealth = 30;
    public int enemyHealth = 30;

    public bool playerTurn = true;
    private bool playerGoesFirst = true;

    public Button endPhaseButton;
    public Button endTurnButton;

    public TMP_Text manaText;
    public TMP_Text turnText;
    public Slider manaBar;

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        Debug.Log("Initializing Game");
        UpdateManaUI();
        SetButtonState(endPhaseButton, true);
        SetButtonState(endTurnButton, false);
        StartCoroutine(PrepPhase());
    }

    private IEnumerator PrepPhase()
    {
        Debug.Log("Entering Prep Phase");
        maxMana++;
        playerMana = maxMana;
        enemyMana = maxMana;
        UpdateManaUI();
        playerTurn = playerGoesFirst;
        playerGoesFirst = !playerGoesFirst;

        if (playerTurn)
        {
            Debug.Log("Player's Prep Phase");
            SetButtonState(endPhaseButton, true);
            yield return null; // Wait for player to press End Phase
        }
        else
        {
            Debug.Log("Enemy's Prep Phase");
            yield return StartCoroutine(EnemyPlayCards());
            EndPhase();
        }
    }

    public void EndPhase()
    {
        Debug.Log("Ending Phase");
        SetButtonState(endPhaseButton, false);
        if (playerTurn)
        {
            Debug.Log("Player's End Phase");
            playerTurn = false;
            StartCoroutine(EnemyPrepPhase());
        }
        else
        {
            Debug.Log("Enemy's End Phase");
            playerTurn = true;
            StartCoroutine(CombatPhase());
        }
    }

    private IEnumerator EnemyPrepPhase()
    {
        Debug.Log("Enemy Prep Phase");
        yield return StartCoroutine(EnemyPlayCards());
        EndPhase();
    }

    private IEnumerator EnemyPlayCards()
    {
        Debug.Log("Enemy Playing Cards");
        yield return new WaitForSeconds(2);
    }

    private IEnumerator CombatPhase()
    {
        Debug.Log("Entering Combat Phase");
        if (playerTurn)
        {
            Debug.Log("Player's Combat Phase");
            SetButtonState(endTurnButton, true);
            yield return null; // Wait for player to press End Turn
        }
        else
        {
            Debug.Log("Enemy's Combat Phase");
            yield return StartCoroutine(EnemyAttack());
            EndTurn();
        }
    }

    public void EndTurn()
    {
        Debug.Log("Ending Turn");
        SetButtonState(endTurnButton, false);
        playerTurn = !playerTurn;

        if (playerTurn)
        {
            Debug.Log("Player's Turn");
            StartCoroutine(PlayerAttackPhase());
        }
        else
        {
            Debug.Log("Enemy's Turn");
            StartCoroutine(EnemyAttackPhase());
        }
    }

    private IEnumerator PlayerAttackPhase()
    {
        Debug.Log("Player's Attack Phase");
        SetButtonState(endTurnButton, true);
        yield return null; // Wait for player to press End Turn
        StartCoroutine(CleanUpPhase());
    }

    private IEnumerator EnemyAttackPhase()
    {
        Debug.Log("Enemy's Attack Phase");
        yield return StartCoroutine(EnemyAttack());
        StartCoroutine(CleanUpPhase());
    }

    private IEnumerator EnemyAttack()
    {
        Debug.Log("Enemy Attacks");
        yield return new WaitForSeconds(2);
    }

    private IEnumerator CleanUpPhase()
    {
        Debug.Log("Entering Clean Up Phase");
        yield return new WaitForSeconds(1);
        StartCoroutine(PrepPhase());
    }

    private void SetButtonState(Button button, bool state)
    {
        if (button != null)
        {
            button.gameObject.SetActive(state);
        }
    }

    private void UpdateManaUI()
    {
        if (manaBar != null)
        {
            manaBar.maxValue = maxMana;
            manaBar.value = playerMana;
        }
        if (manaText != null)
        {
            manaText.text = playerMana.ToString();
        }
        else
        {
            Debug.LogWarning("Mana Text not set in the Combat Manager");
        }
    }
}
