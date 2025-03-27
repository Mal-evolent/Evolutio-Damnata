using System.Collections;
using UnityEngine;

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
        Debug.Log("Entering Prep Phase");
        _combatManager.ResetPhaseState();

        if (_combatManager.PlayerTurn)
        {
            _combatManager.CurrentPhase = CombatPhase.PlayerPrep;
            Debug.Log("Player's Prep Phase");
            _uiManager.SetButtonState(_combatManager.EndPhaseButton, true);
            yield return new WaitUntil(() => _combatManager.EndPhaseButton.gameObject.activeSelf == false);

            _combatManager.CurrentPhase = CombatPhase.EnemyPrep;
            _combatManager.PlayerTurn = false;
            Debug.Log("Enemy's Prep Phase");
            yield return ((MonoBehaviour)_combatManager).StartCoroutine(_enemyActions.PlayCards());
        }
        else
        {
            _combatManager.CurrentPhase = CombatPhase.EnemyPrep;
            Debug.Log("Enemy's Prep Phase");
            yield return ((MonoBehaviour)_combatManager).StartCoroutine(_enemyActions.PlayCards());

            _combatManager.CurrentPhase = CombatPhase.PlayerPrep;
            _combatManager.PlayerTurn = true;
            Debug.Log("Player's Prep Phase");
            _uiManager.SetButtonState(_combatManager.EndPhaseButton, true);
            yield return new WaitUntil(() => _combatManager.EndPhaseButton.gameObject.activeSelf == false);
        }

        _combatManager.CurrentPhase = CombatPhase.None;
        yield return ((MonoBehaviour)_combatManager).StartCoroutine(ExecuteCombatPhase());
    }

    public IEnumerator ExecuteCombatPhase()
    {
        Debug.Log("Entering Combat Phase");
        _combatManager.ResetPhaseState();

        if (_combatManager.PlayerTurn)
        {
            _combatManager.CurrentPhase = CombatPhase.PlayerCombat;
            Debug.Log("Player Attacks - Start");
            _uiManager.SetButtonState(_combatManager.EndTurnButton, true);
            _playerActions.PlayerTurnEnded = false;
            yield return new WaitUntil(() => _playerActions.PlayerTurnEnded);
            Debug.Log("Player Attacks - End");

            _combatManager.PlayerTurn = false;
            _combatManager.CurrentPhase = CombatPhase.EnemyCombat;
            Debug.Log("Enemy Attacks - Start");
            yield return ((MonoBehaviour)_combatManager).StartCoroutine(_enemyActions.Attack());
            Debug.Log("Enemy Attacks - End");
        }
        else
        {
            _combatManager.CurrentPhase = CombatPhase.EnemyCombat;
            Debug.Log("Enemy Attacks - Start");
            yield return ((MonoBehaviour)_combatManager).StartCoroutine(_enemyActions.Attack());
            Debug.Log("Enemy Attacks - End");

            _combatManager.PlayerTurn = true;
            _combatManager.CurrentPhase = CombatPhase.PlayerCombat;
            Debug.Log("Player Attacks - Start");
            _uiManager.SetButtonState(_combatManager.EndTurnButton, true);
            _playerActions.PlayerTurnEnded = false;
            yield return new WaitUntil(() => _playerActions.PlayerTurnEnded);
            Debug.Log("Player Attacks - End");
        }

        _combatManager.CurrentPhase = CombatPhase.None;
        yield return ((MonoBehaviour)_combatManager).StartCoroutine(CleanUpPhase());
    }

    private IEnumerator CleanUpPhase()
    {
        Debug.Log("Entering Clean-Up Phase");
        _combatManager.CurrentPhase = CombatPhase.CleanUp;

        // Get the sprite positioning from combat stage
        ISpritePositioning spritePositioning = _combatManager.CombatStage.SpritePositioning;

        // Apply ongoing effects to all player entities
        foreach (var entity in spritePositioning.PlayerEntities)
        {
            var entityManager = entity.GetComponent<EntityManager>();
            if (entityManager != null)
            {
                entityManager.ApplyOngoingEffects();
                Debug.Log($"Resetting attacks for player entity: {entityManager.name}");
                _attackLimiter.ResetAttacks(entityManager);
            }
        }

        // Apply ongoing effects to all enemy entities
        foreach (var entity in spritePositioning.EnemyEntities)
        {
            var entityManager = entity.GetComponent<EntityManager>();
            if (entityManager != null)
            {
                entityManager.ApplyOngoingEffects();
                Debug.Log($"Resetting attacks for enemy entity: {entityManager.name}");
                _attackLimiter.ResetAttacks(entityManager);
            }
        }

        _combatManager.PlayerDeck.DrawOneCard();
        _combatManager.EnemyDeck.DrawOneCard();
        yield return new WaitForSeconds(1);

        ((MonoBehaviour)_combatManager).StartCoroutine(_roundManager.RoundStart());
        _combatManager.ResetPhaseState();
    }

    public void EndPhase()
    {
        Debug.Log("Ending Prep Phase");
    }
}