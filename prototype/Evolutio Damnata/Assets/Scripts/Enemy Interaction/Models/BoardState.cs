using System.Collections.Generic;
using UnityEngine;

public class BoardState
{
    public List<EntityManager> EnemyMonsters { get; set; } = new List<EntityManager>();
    public List<EntityManager> PlayerMonsters { get; set; } = new List<EntityManager>();
    public int EnemyHealth { get; set; }
    public int PlayerHealth { get; set; }
    public int TurnCount { get; set; }
    public int EnemyMana { get; set; }
    public float EnemyBoardControl { get; set; }
    public float PlayerBoardControl { get; set; }
    public float BoardControlDifference { get; set; }
    public int HealthAdvantage { get; set; }
    public float HealthRatio { get; set; }
    public int playerHandSize { get; set; }
    public int enemyHandSize { get; set; }
    public int CardAdvantage { get; set; }
    public CombatPhase CurrentPhase { get; set; }
    public bool IsPlayerTurn { get; set; }
    public bool IsNextTurnPlayerFirst { get; set; }
    public int EnemyMaxHealth { get; set; }
    public int PlayerMaxHealth { get; set; }

    // Default constructor to initialize properties
    public BoardState()
    {
        EnemyMonsters = new List<EntityManager>();
        PlayerMonsters = new List<EntityManager>();
        CardAdvantage = 0;
    }
}
