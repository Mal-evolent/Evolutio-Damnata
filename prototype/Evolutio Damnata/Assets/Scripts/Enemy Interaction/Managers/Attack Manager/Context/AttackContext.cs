using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data container class that holds all the contextual information needed for enemy attack decisions.
/// </summary>
public class AttackContext
{
    /// <summary>
    /// List of enemy entities that can perform attacks
    /// </summary>
    public List<EntityManager> EnemyEntities { get; set; }

    /// <summary>
    /// List of player entities that can be targeted
    /// </summary>
    public List<EntityManager> PlayerEntities { get; set; }

    /// <summary>
    /// Reference to the player's health icon which can be directly attacked if no other targets exist
    /// </summary>
    public HealthIconManager PlayerHealthIcon { get; set; }

    /// <summary>
    /// Current state of the game board, used for strategic decisions
    /// </summary>
    public BoardState BoardState { get; set; }

    /// <summary>
    /// Indicates whether the attack context is valid and can be used
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Contains error message if context creation failed
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Creates a new empty attack context
    /// </summary>
    public AttackContext()
    {
        IsValid = false;
        EnemyEntities = new List<EntityManager>();
        PlayerEntities = new List<EntityManager>();
    }

    /// <summary>
    /// Creates a new attack context with the specified entities and board state
    /// </summary>
    public AttackContext(List<EntityManager> enemyEntities, List<EntityManager> playerEntities, BoardState boardState)
    {
        EnemyEntities = enemyEntities ?? new List<EntityManager>();
        PlayerEntities = playerEntities ?? new List<EntityManager>();
        BoardState = boardState;
        IsValid = true;
    }
}
