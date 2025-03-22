using System.Collections;
using UnityEngine;

/**
 * The PhaseManager class is responsible for managing the different phases of the combat stage.
 * It handles the preparation phase, combat phase, and clean-up phase of the game.
 */

public class PhaseManager
{
    private CombatManager combatManager;
    private AttackLimiter attackLimiter;

    public PhaseManager(CombatManager combatManager, AttackLimiter attackLimiter)
    {
        this.combatManager = combatManager;
        this.attackLimiter = attackLimiter;
    }

    public IEnumerator PrepPhase()
    {
        Debug.Log("Entering Prep Phase");
        combatManager.ResetPhaseStates();

        if (combatManager.playerTurn)
        {
            combatManager.isPlayerPrepPhase = true;
            Debug.Log("Player's Prep Phase");
            combatManager.uiManager.SetButtonState(combatManager.endPhaseButton, true);
            yield return new WaitUntil(() => combatManager.endPhaseButton.gameObject.activeSelf == false);

            combatManager.isPlayerPrepPhase = false;
            combatManager.playerTurn = false;
            combatManager.isEnemyPrepPhase = true;
            Debug.Log("Enemy's Prep Phase");
            yield return combatManager.StartCoroutine(combatManager.enemyActions.PlayCards());
            combatManager.isEnemyPrepPhase = false;
        }
        else
        {
            combatManager.isEnemyPrepPhase = true;
            Debug.Log("Enemy's Prep Phase");
            yield return combatManager.StartCoroutine(combatManager.enemyActions.PlayCards());
            combatManager.isEnemyPrepPhase = false;

            combatManager.isPlayerPrepPhase = true;
            combatManager.playerTurn = true;
            Debug.Log("Player's Prep Phase");
            combatManager.uiManager.SetButtonState(combatManager.endPhaseButton, true);
            yield return new WaitUntil(() => combatManager.endPhaseButton.gameObject.activeSelf == false);
            combatManager.isPlayerPrepPhase = false;
        }

        yield return combatManager.StartCoroutine(CombatPhase());
    }

    public IEnumerator CombatPhase()
    {
        Debug.Log("Entering Combat Phase");
        combatManager.ResetPhaseStates();

        if (combatManager.playerTurn)
        {
            combatManager.isPlayerCombatPhase = true;
            Debug.Log("Player Attacks - Start");
            combatManager.uiManager.SetButtonState(combatManager.endTurnButton, true);
            combatManager.playerActions.playerTurnEnded = false;
            yield return new WaitUntil(() => combatManager.playerActions.playerTurnEnded);
            combatManager.isPlayerCombatPhase = false;
            Debug.Log("Player Attacks - End");

            combatManager.playerTurn = false;

            combatManager.isEnemyCombatPhase = true;
            Debug.Log("Enemy Attacks - Start");
            yield return combatManager.StartCoroutine(combatManager.enemyActions.Attack());
            combatManager.isEnemyCombatPhase = false;
            Debug.Log("Enemy Attacks - End");
        }
        else
        {
            combatManager.isEnemyCombatPhase = true;
            Debug.Log("Enemy Attacks - Start");
            yield return combatManager.StartCoroutine(combatManager.enemyActions.Attack());
            combatManager.isEnemyCombatPhase = false;
            Debug.Log("Enemy Attacks - End");

            combatManager.playerTurn = true;

            combatManager.isPlayerCombatPhase = true;
            Debug.Log("Player Attacks - Start");
            combatManager.uiManager.SetButtonState(combatManager.endTurnButton, true);
            combatManager.playerActions.playerTurnEnded = false;
            yield return new WaitUntil(() => combatManager.playerActions.playerTurnEnded);
            combatManager.isPlayerCombatPhase = false;
            Debug.Log("Player Attacks - End");
        }

        yield return combatManager.StartCoroutine(CleanUpPhase());
    }

    public IEnumerator CleanUpPhase()
    {
        Debug.Log("Entering Clean-Up Phase");
        combatManager.isCleanUpPhase = true;

        // Apply ongoing effects to all player entities
        foreach (var entity in combatManager.combatStage.spritePositioning.playerEntities)
        {
            EntityManager entityManager = entity.GetComponent<EntityManager>();
            if (entityManager != null)
            {
                entityManager.ApplyOngoingEffects();
                Debug.Log($"Resetting attacks for player entity: {entityManager.name}");
                attackLimiter.ResetAttacks(entityManager);
            }
        }

        // Apply ongoing effects to all enemy entities
        foreach (var entity in combatManager.combatStage.spritePositioning.enemyEntities)
        {
            EntityManager entityManager = entity.GetComponent<EntityManager>();
            if (entityManager != null)
            {
                entityManager.ApplyOngoingEffects();
                Debug.Log($"Resetting attacks for enemy entity: {entityManager.name}");
                attackLimiter.ResetAttacks(entityManager);
            }
        }

        combatManager.playerDeck.DrawOneCard();
        combatManager.enemyDeck.DrawOneCard();
        yield return new WaitForSeconds(1);
        combatManager.StartCoroutine(combatManager.gameStateManager.RoundStart());
        combatManager.ResetPhaseStates();
    }

    public void EndPhase()
    {
        Debug.Log("Ending Prep Phase");
    }
}
