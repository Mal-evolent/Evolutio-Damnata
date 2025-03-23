using UnityEngine;
using TMPro;
using UnityEngine.UI;

/*
 * The CombatManager class is responsible for managing the combat stage of the game.
 * It keeps track of the game state, player and enemy health, mana, and turn count.
 * It also handles the end phase and end turn buttons, and the player and enemy actions.
 */

public class CombatManager : MonoBehaviour
{
    public CombatStage combatStage;
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

    public Deck playerDeck;
    public Deck enemyDeck;

    public roundManager gameStateManager;
    public PhaseManager phaseManager;
    public PlayerActions playerActions;
    public EnemyActions enemyActions;
    public UIManager uiManager;

    public bool isPlayerPrepPhase = false;
    public bool isPlayerCombatPhase = false;
    public bool isEnemyPrepPhase = false;
    public bool isEnemyCombatPhase = false;
    public bool isCleanUpPhase = false;

    private AttackLimiter attackLimiter;

    public void Start()
    {
        attackLimiter = new AttackLimiter();
        gameStateManager = new roundManager(this);
        phaseManager = new PhaseManager(this, attackLimiter);
        playerActions = new PlayerActions(this);
        enemyActions = new EnemyActions(this, combatStage.spritePositioning, enemyDeck, combatStage.cardLibrary, combatStage);
        uiManager = new UIManager(this);

        gameStateManager.InitializeGame();
    }

    public void EndPhase()
    {
        uiManager.SetButtonState(endPhaseButton, false);
        phaseManager.EndPhase();
    }

    public void EndTurn()
    {
        uiManager.SetButtonState(endTurnButton, false);
        playerActions.EndTurn();
    }

    public void ResetPhaseStates()
    {
        isPlayerPrepPhase = false;
        isPlayerCombatPhase = false;
        isEnemyPrepPhase = false;
        isEnemyCombatPhase = false;
        isCleanUpPhase = false;
    }
}
