using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class PhaseManager : IPhaseManager
{
    private readonly ICombatManager _combatManager;
    private readonly AttackLimiter _attackLimiter;
    private readonly IUIManager _uiManager;
    private readonly IEnemyActions _enemyActions;
    private readonly IPlayerActions _playerActions;
    private readonly IRoundManager _roundManager;

    public PhaseManager(
        ICombatManager combatManager,
        AttackLimiter attackLimiter,
        IUIManager uiManager,
        IEnemyActions enemyActions,
        IPlayerActions playerActions,
        IRoundManager roundManager)
    {
        _combatManager = combatManager;
        _attackLimiter = attackLimiter;
        _uiManager = uiManager;
        _enemyActions = enemyActions;
        _playerActions = playerActions;
        _roundManager = roundManager;
    }

    public IEnumerator PrepPhase()
    {
        Debug.Log("[PhaseManager] ===== ENTERING PREP PHASE =====");
        _combatManager.ResetPhaseState();

        if (_combatManager.PlayerTurn)
        {
            yield return RunSafely(PlayerPrepPhase(), "Player Prep Phase");
            yield return RunSafely(EnemyPrepPhase(), "Enemy Prep Phase");
        }
        else
        {
            yield return RunSafely(EnemyPrepPhase(), "Enemy Prep Phase");
            yield return RunSafely(PlayerPrepPhase(), "Player Prep Phase");
        }

        _combatManager.CurrentPhase = CombatPhase.None;
        yield return RunSafely(ExecuteCombatPhase(), "Execute Combat Phase");
    }

    public IEnumerator ExecuteCombatPhase()
    {
        Debug.Log("[PhaseManager] ===== ENTERING COMBAT PHASE =====");
        _combatManager.ResetPhaseState();

        if (_combatManager.PlayerTurn)
        {
            yield return RunSafely(PlayerCombatPhase(), "Player Combat Phase");
            yield return RunSafely(EnemyCombatPhase(), "Enemy Combat Phase");
        }
        else
        {
            yield return RunSafely(EnemyCombatPhase(), "Enemy Combat Phase");
            yield return RunSafely(PlayerCombatPhase(), "Player Combat Phase");
        }

        _combatManager.CurrentPhase = CombatPhase.None;
        yield return RunSafely(CleanUpPhase(), "Clean Up Phase");
    }

    private IEnumerator CleanUpPhase()
    {
        Debug.Log("[PhaseManager] ===== ENTERING CLEAN-UP PHASE =====");
        _combatManager.CurrentPhase = CombatPhase.CleanUp;

        if (_combatManager.CombatStage == null || _combatManager.CombatStage.SpritePositioning == null)
        {
            Debug.LogError("[PhaseManager] CombatStage references missing!");
            yield break;
        }

        var spritePositioning = _combatManager.CombatStage.SpritePositioning;

        yield return RunSafely(ProcessEntities(spritePositioning.PlayerEntities, "Player"), "Process Player Entities");
        yield return RunSafely(ProcessEntities(spritePositioning.EnemyEntities, "Enemy"), "Process Enemy Entities");

        yield return RunSafely(DrawCards(), "Draw Cards");
        yield return new WaitForSeconds(1);

        Debug.Log("[PhaseManager] Starting new round");
        yield return RunSafely(_roundManager.RoundStart(), "Round Start");
        _combatManager.ResetPhaseState();
    }

    public void EndPhase()
    {
        Debug.Log("[PhaseManager] Ending current phase");
    }

    #region Phase Handlers
    private IEnumerator PlayerPrepPhase()
    {
        _combatManager.CurrentPhase = CombatPhase.PlayerPrep;
        Debug.Log("[PhaseManager] Player's Prep Phase");
        _uiManager.SetButtonState(_combatManager.EndPhaseButton, true);
        yield return new WaitUntil(() => !_combatManager.EndPhaseButton.gameObject.activeSelf);
        _combatManager.PlayerTurn = false;
    }

    private IEnumerator EnemyPrepPhase()
    {
        _combatManager.CurrentPhase = CombatPhase.EnemyPrep;
        Debug.Log("[PhaseManager] Enemy's Prep Phase");
        yield return _enemyActions.PlayCards();
    }

    private IEnumerator PlayerCombatPhase()
    {
        _combatManager.CurrentPhase = CombatPhase.PlayerCombat;
        Debug.Log("[PhaseManager] Player's Combat Phase");
        _uiManager.SetButtonState(_combatManager.EndTurnButton, true);
        _playerActions.PlayerTurnEnded = false;
        yield return new WaitUntil(() => _playerActions.PlayerTurnEnded);
        _combatManager.PlayerTurn = false;
    }

    private IEnumerator EnemyCombatPhase()
    {
        _combatManager.CurrentPhase = CombatPhase.EnemyCombat;
        Debug.Log("[PhaseManager] Enemy's Combat Phase");
        yield return _enemyActions.Attack();
    }
    #endregion

    #region Helper Methods
    private IEnumerator RunSafely(IEnumerator coroutine, string context)
    {
        if (coroutine == null) yield break;

        while (true)
        {
            bool moveNext;
            try
            {
                moveNext = coroutine.MoveNext();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PhaseManager] Error in {context}: {e.Message}");
                yield break;
            }

            if (!moveNext) break;
            yield return coroutine.Current;
        }
    }

    private IEnumerator ProcessEntities(List<GameObject> entities, string entityType)
    {
        if (entities == null) yield break;

        Debug.Log($"[PhaseManager] Processing {entityType} entities (Count: {entities.Count})");

        // Create a new list to avoid modification during iteration
        var entitiesToProcess = new List<GameObject>(entities);

        for (int i = 0; i < entitiesToProcess.Count; i++)
        {
            var entity = entitiesToProcess[i];
            if (entity == null)
            {
                Debug.LogWarning($"[PhaseManager] {entityType} entity {i} is null - removing from list");
                entities.Remove(entity);
                continue;
            }

            if (!entity.activeInHierarchy)
            {
                Debug.Log($"[PhaseManager] {entityType} entity {i} is inactive - skipping");
                continue;
            }

            var entityManager = entity.GetComponent<EntityManager>();
            if (entityManager == null)
            {
                Debug.LogWarning($"[PhaseManager] {entityType} entity {i} has no EntityManager");
                continue;
            }

            if (entityManager.dead)
            {
                Debug.Log($"[PhaseManager] Skipping dead {entityType} entity: {entity.name}");
                continue;
            }

            try
            {
                Debug.Log($"[PhaseManager] Processing {entityType} entity: {entity.name}");
                entityManager.ApplyOngoingEffects();
                _attackLimiter.ResetAttacks(entityManager);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PhaseManager] Error processing {entityType} entity {entity.name}: {e.Message}");
            }

            yield return null;
        }
    }

    private IEnumerator DrawCards()
    {
        Debug.Log("[PhaseManager] Drawing cards");
        _combatManager.PlayerDeck?.DrawOneCard();
        _combatManager.EnemyDeck?.DrawOneCard();
        yield return null;
    }
    #endregion
}