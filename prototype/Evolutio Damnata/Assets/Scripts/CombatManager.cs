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

    public Button endPhaseButton;
    public Button endTurnButton;

    public TMP_Text manaText;
    public TMP_Text turnText;
    public Slider manaBar;

    public bool playerGoesFirst = true;
    public bool playerTurn;

    public bool isPlayerPrepPhase = false;
    public bool isPlayerCombatPhase = false;
    public bool isEnemyPrepPhase = false;
    public bool isEnemyCombatPhase = false;

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
        StartCoroutine(RoundStart());
    }

    private IEnumerator RoundStart()
    {
        Debug.Log("Starting New Round");
        maxMana++;
        playerMana = maxMana;
        enemyMana = maxMana;
        UpdateManaUI();

        playerTurn = playerGoesFirst;
        playerGoesFirst = !playerGoesFirst;

        yield return StartCoroutine(PrepPhase());
    }

    private IEnumerator PrepPhase()
    {
        Debug.Log("Entering Prep Phase");
        ResetPhaseStates();

        if (playerTurn)
        {
            isPlayerPrepPhase = true;
            Debug.Log("Player's Prep Phase");
            SetButtonState(endPhaseButton, true);
            yield return new WaitUntil(() => endPhaseButton.gameObject.activeSelf == false);

            isPlayerPrepPhase = false;
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

        if (playerTurn)
        {
            isPlayerCombatPhase = true;
            Debug.Log("Player Attacks");
            SetButtonState(endTurnButton, true);
            yield return new WaitUntil(() => endTurnButton.gameObject.activeSelf == false);
            isPlayerCombatPhase = false;

            isEnemyCombatPhase = true;
            Debug.Log("Enemy Attacks");
            yield return StartCoroutine(EnemyAttack());
            isEnemyCombatPhase = false;
        }
        else
        {
            isEnemyCombatPhase = true;
            Debug.Log("Enemy Attacks");
            yield return StartCoroutine(EnemyAttack());
            isEnemyCombatPhase = false;

            isPlayerCombatPhase = true;
            Debug.Log("Player Attacks");
            SetButtonState(endTurnButton, true);
            yield return new WaitUntil(() => endTurnButton.gameObject.activeSelf == false);
            isPlayerCombatPhase = false;
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
    }

    private IEnumerator CleanUpPhase()
    {
        Debug.Log("Entering Clean-Up Phase");
        ResetPhaseStates();
        yield return new WaitForSeconds(1);
        StartCoroutine(RoundStart());
    }

    private void ResetPhaseStates()
    {
        isPlayerPrepPhase = false;
        isPlayerCombatPhase = false;
        isEnemyPrepPhase = false;
        isEnemyCombatPhase = false;
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
