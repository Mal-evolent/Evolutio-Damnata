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
        Debug.Log("[PhaseManager] ===== ENTERING PREP PHASE =====");
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
        Debug.Log("[PhaseManager] ===== ENTERING COMBAT PHASE =====");
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
        Debug.Log("[PhaseManager] ===== ENTERING CLEAN-UP PHASE =====");

        _combatManager.CurrentPhase = CombatPhase.CleanUp;
        Debug.Log("[PhaseManager] Set phase to CleanUp");

        // Verify dependencies
        if (_combatManager.CombatStage == null)
        {
            Debug.LogError("[PhaseManager] CRITICAL: CombatStage is null!");
            yield break;
        }

        ISpritePositioning spritePositioning = _combatManager.CombatStage.SpritePositioning;
        if (spritePositioning == null)
        {
            Debug.LogError("[PhaseManager] CRITICAL: SpritePositioning is null!");
            yield break;
        }

        Debug.Log($"[PhaseManager] Found {spritePositioning.PlayerEntities.Count} player entities and {spritePositioning.EnemyEntities.Count} enemy entities");

        // Process player entities with error handling per entity
        Debug.Log("[PhaseManager] --- Processing Player Entities ---");
        for (int i = 0; i < spritePositioning.PlayerEntities.Count; i++)
        {
            try
            {
                var entity = spritePositioning.PlayerEntities[i];
                if (entity == null)
                {
                    Debug.LogWarning($"[PhaseManager] Player entity at index {i} is null (likely destroyed)");
                    continue;
                }

                var entityManager = entity.GetComponent<EntityManager>();
                if (entityManager == null)
                {
                    Debug.LogWarning($"[PhaseManager] {entity.name} has no EntityManager component");
                    continue;
                }

                Debug.Log($"[PhaseManager] Processing player entity #{i}: {entityManager.name}");
                entityManager.ApplyOngoingEffects();
                Debug.Log($"[PhaseManager] Applied effects to {entityManager.name}");

                _attackLimiter.ResetAttacks(entityManager);
                Debug.Log($"[PhaseManager] Reset attacks for {entityManager.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PhaseManager] Error processing player entity: {e.Message}");
            }
        }

        // Process enemy entities with error handling per entity
        Debug.Log("[PhaseManager] --- Processing Enemy Entities ---");
        for (int i = 0; i < spritePositioning.EnemyEntities.Count; i++)
        {
            try
            {
                var entity = spritePositioning.EnemyEntities[i];
                if (entity == null)
                {
                    Debug.LogWarning($"[PhaseManager] Enemy entity at index {i} is null (likely destroyed)");
                    continue;
                }

                var entityManager = entity.GetComponent<EntityManager>();
                if (entityManager == null)
                {
                    Debug.LogWarning($"[PhaseManager] {entity.name} has no EntityManager component");
                    continue;
                }

                Debug.Log($"[PhaseManager] Processing enemy entity #{i}: {entityManager.name}");
                entityManager.ApplyOngoingEffects();
                Debug.Log($"[PhaseManager] Applied effects to {entityManager.name}");

                _attackLimiter.ResetAttacks(entityManager);
                Debug.Log($"[PhaseManager] Reset attacks for {entityManager.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PhaseManager] Error processing enemy entity: {e.Message}");
            }
        }

        // Card drawing
        Debug.Log("[PhaseManager] Drawing cards for both players");
        _combatManager.PlayerDeck.DrawOneCard();
        _combatManager.EnemyDeck.DrawOneCard();

        Debug.Log("[PhaseManager] Waiting 1 second before next round");
        yield return new WaitForSeconds(1);

        // Start next round
        Debug.Log("[PhaseManager] Starting new round");
        ((MonoBehaviour)_combatManager).StartCoroutine(_roundManager.RoundStart());
        _combatManager.ResetPhaseState();

        Debug.Log("[PhaseManager] ===== CLEAN-UP PHASE COMPLETED =====");
    }

    public void EndPhase()
    {
        Debug.Log("Ending Prep Phase");
    }
}