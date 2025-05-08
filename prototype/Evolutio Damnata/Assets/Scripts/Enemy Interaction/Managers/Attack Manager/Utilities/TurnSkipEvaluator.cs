using System.Collections;
using System.Collections.Generic;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Managers;
using EnemyInteraction.Utilities;
using UnityEngine;

public class TurnSkipEvaluator
{
    private readonly float _skipTurnConsiderationChance;
    private readonly float _skipTurnBoardAdvantageThreshold;

    public TurnSkipEvaluator(float skipChance, float advantageThreshold)
    {
        _skipTurnConsiderationChance = skipChance;
        _skipTurnBoardAdvantageThreshold = advantageThreshold;
    }

    public bool ShouldSkipTurn(BoardState boardState,
                              IEntityCacheManager entityCacheManager,
                              IAttackStrategyManager strategyManager)
    {
        // Get the player health icon first to check if it's available as a target
        HealthIconManager playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();

        // Get cached player entities to check if direct attack is possible
        entityCacheManager.RefreshEntityCaches();
        List<EntityManager> playerEntities = entityCacheManager.CachedPlayerEntities;

        // If there are no player entities and player health icon is available,
        // we should NEVER skip the turn - always take direct shots at player health
        if (AIUtilities.CanTargetHealthIcon(playerEntities) && playerHealthIcon != null)
        {
            Debug.Log("[TurnSkipEvaluator] Player health icon is directly targetable - never skipping turn");
            return false;
        }

        // First, check if we should even consider skipping (random chance)
        if (Random.value > _skipTurnConsiderationChance)
            return false;

        Debug.Log("[TurnSkipEvaluator] Considering whether to skip turn...");

        // Check if board state is available
        if (boardState == null)
            return false;

        // Calculate the enemy's board advantage
        float enemyBoardAdvantage = CalculateBoardAdvantage(boardState);

        // Check if the enemy has sufficient board advantage to consider skipping
        bool hasSufficientAdvantage = enemyBoardAdvantage >= _skipTurnBoardAdvantageThreshold;

        // Check if player will go first next turn (if we can determine this)
        bool playerGoesNextTurn = boardState.IsNextTurnPlayerFirst;

        // Check if player is at low health (making a skip less advisable)
        bool playerAtLowHealth = boardState.PlayerHealth <= 10;

        // Late game considerations (don't skip in late game)
        bool isLateGame = boardState.TurnCount >= 5;

        // Strategic mode from the strategy manager
        StrategicMode currentStrategy = strategyManager.DetermineStrategicMode(boardState);

        // Don't skip if we're in aggressive mode
        if (currentStrategy == StrategicMode.Aggro)
        {
            Debug.Log("[TurnSkipEvaluator] Won't skip turn - current strategy is aggressive");
            return false;
        }

        // Don't skip if player is at low health
        if (playerAtLowHealth)
        {
            Debug.Log("[TurnSkipEvaluator] Won't skip turn - player health is low, should press advantage");
            return false;
        }

        // Don't skip in late game
        if (isLateGame)
        {
            Debug.Log("[TurnSkipEvaluator] Won't skip turn - game is in later stages");
            return false;
        }

        // If enemy has significant board advantage and is in defensive mode,
        // consider skipping to preserve board position
        if (hasSufficientAdvantage && currentStrategy == StrategicMode.Defensive)
        {
            return EvaluateSkipWithAdvantage(enemyBoardAdvantage, playerGoesNextTurn);
        }

        Debug.Log("[TurnSkipEvaluator] Decided not to skip turn");
        return false;
    }

    private float CalculateBoardAdvantage(BoardState boardState)
    {
        // Ensure we don't divide by zero
        float playerControl = boardState.PlayerBoardControl > 0 ? boardState.PlayerBoardControl : 1;
        return boardState.EnemyBoardControl / playerControl;
    }

    private bool EvaluateSkipWithAdvantage(float enemyBoardAdvantage, bool playerGoesNextTurn)
    {
        // If player goes next turn, higher chance to skip (preserve board for their turn)
        if (playerGoesNextTurn)
        {
            Debug.Log($"[TurnSkipEvaluator] Skipping turn - enemy has board advantage of {enemyBoardAdvantage:F2} and player goes next");
            return true;
        }

        // Even if we go next, still consider skipping with a good advantage
        if (enemyBoardAdvantage >= _skipTurnBoardAdvantageThreshold * 1.5f)
        {
            Debug.Log($"[TurnSkipEvaluator] Skipping turn - enemy has strong board advantage of {enemyBoardAdvantage:F2}");
            return true;
        }

        return false;
    }
}
