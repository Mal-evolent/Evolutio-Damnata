using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardState
{
    public List<EntityManager> EnemyMonsters { get; set; } = new List<EntityManager>();
    public List<EntityManager> PlayerMonsters { get; set; } = new List<EntityManager>();
    public int EnemyHealth { get; set; }
    public int PlayerHealth { get; set; }
    public int TurnCount { get; set; }
    public int EnemyMana { get; set; }
    public int PlayerMana { get; set; }

    public float EnemyBoardControl { get; set; }
    public float PlayerBoardControl { get; set; }
    public float BoardControlDifference { get; set; }
    public int HealthAdvantage { get; set; }
    public float HealthRatio { get; set; }
    public int CardAdvantage { get; set; }
    public CombatPhase CurrentPhase { get; set; }
    public bool IsPlayerTurn { get; set; }
    public bool IsNextTurnPlayerFirst { get; set; }
    public int EnemyMaxHealth { get; set; }
    public int PlayerMaxHealth { get; set; }
    public int PlayerHandSize { get; set; }
    public int EnemyHandSize { get; set; }

    public float HealthImportanceFactor { get; set; } = 1.0f;

    // Deck references
    private Deck _playerDeck;
    private Deck _enemyDeck;

    // Proper properties for deck information
    public Deck PlayerDeck
    {
        get => _playerDeck;
        set => _playerDeck = value;
    }

    public Deck EnemyDeck
    {
        get => _enemyDeck;
        set => _enemyDeck = value;
    }

    // Deck size properties
    public int PlayerDeckSize => _playerDeck != null ? _playerDeck.Cards.Count : 0;
    public int EnemyDeckSize => _enemyDeck != null ? _enemyDeck.Cards.Count : 0;

    // Maximum hand size based on deck configuration
    public int PlayerMaxHandSize => _playerDeck != null ? _playerDeck.HandSize : 0;
    public int EnemyMaxHandSize => _enemyDeck != null ? _enemyDeck.HandSize : 0;

    // Default constructor for serialization support
    public BoardState() { }

    // Constructor with deck references
    public BoardState(Deck playerDeck, Deck enemyDeck)
    {
        _playerDeck = playerDeck;
        _enemyDeck = enemyDeck;
    }

    // Method to update deck references if they change
    public void UpdateDeckReferences(Deck playerDeck, Deck enemyDeck)
    {
        _playerDeck = playerDeck;
        _enemyDeck = enemyDeck;
    }

    // Method to validate deck references
    public bool ValidateDeckReferences()
    {
        bool playerDeckValid = _playerDeck != null;
        bool enemyDeckValid = _enemyDeck != null;

        if (!playerDeckValid) Debug.LogWarning("BoardState: PlayerDeck reference is missing");
        if (!enemyDeckValid) Debug.LogWarning("BoardState: EnemyDeck reference is missing");

        return playerDeckValid && enemyDeckValid;
    }

    // Method to update from current combat state
    public void UpdateFromCombatManager(ICombatManager combatManager)
    {
        if (combatManager == null)
        {
            Debug.LogError("BoardState: UpdateFromCombatManager called with null combatManager");
            return;
        }

        // Update health values
        PlayerHealth = combatManager.PlayerHealth;
        EnemyHealth = combatManager.EnemyHealth;
        PlayerMaxHealth = combatManager.MaxHealth;
        EnemyMaxHealth = combatManager.MaxHealth;

        // Update turn and phase info
        TurnCount = combatManager.TurnCount;
        CurrentPhase = combatManager.CurrentPhase;
        IsPlayerTurn = combatManager.PlayerTurn;
        IsNextTurnPlayerFirst = combatManager.PlayerGoesFirst;

        // Update mana
        EnemyMana = combatManager.EnemyMana;

        // Update card counts
        PlayerHandSize = combatManager.PlayerHandSize;
        EnemyHandSize = combatManager.EnemyHandSize;

        //player mana
        PlayerMana = combatManager.PlayerMana;

        // Update deck references if needed
        if (_playerDeck != combatManager.PlayerDeck || _enemyDeck != combatManager.EnemyDeck)
        {
            UpdateDeckReferences(combatManager.PlayerDeck, combatManager.EnemyDeck);
        }
    }

    // Calculate board control metrics
    public void UpdateBoardControlMetrics()
    {
        // Calculate total attack and health for player monsters
        float playerAttackSum = 0f;
        float playerHealthSum = 0f;
        foreach (var monster in PlayerMonsters)
        {
            if (monster != null && !monster.dead && monster.placed)
            {
                playerAttackSum += monster.GetAttack();
                playerHealthSum += monster.GetHealth();
            }
        }

        // Calculate total attack and health for enemy monsters
        float enemyAttackSum = 0f;
        float enemyHealthSum = 0f;
        foreach (var monster in EnemyMonsters)
        {
            if (monster != null && !monster.dead && monster.placed)
            {
                enemyAttackSum += monster.GetAttack();
                enemyHealthSum += monster.GetHealth();
            }
        }

        // Calculate board control metrics with weighting
        PlayerBoardControl = playerAttackSum * 1.2f + playerHealthSum * 0.8f;
        EnemyBoardControl = enemyAttackSum * 1.2f + enemyHealthSum * 0.8f;
        BoardControlDifference = EnemyBoardControl - PlayerBoardControl;

        // Update health advantage
        HealthAdvantage = EnemyHealth - PlayerHealth;

        // Update health ratio with safety check
        HealthRatio = PlayerHealth > 0 ? (float)EnemyHealth / PlayerHealth : float.MaxValue;

        // Update card advantage
        CardAdvantage = EnemyHandSize - PlayerHandSize;
    }

    // Method to update monsters on the board
    public void UpdateMonsters(List<EntityManager> playerMonsters, List<EntityManager> enemyMonsters)
    {
        PlayerMonsters = new List<EntityManager>(playerMonsters ?? new List<EntityManager>());
        EnemyMonsters = new List<EntityManager>(enemyMonsters ?? new List<EntityManager>());

        // Recalculate board metrics after updating monsters
        UpdateBoardControlMetrics();
    }

    // Create a snapshot of the current board state
    public static BoardState CreateSnapshot(ICombatManager combatManager, List<EntityManager> playerMonsters, List<EntityManager> enemyMonsters)
    {
        if (combatManager == null)
        {
            Debug.LogError("BoardState: CreateSnapshot called with null combatManager");
            return new BoardState();
        }

        var boardState = new BoardState(combatManager.PlayerDeck, combatManager.EnemyDeck);
        boardState.UpdateFromCombatManager(combatManager);
        boardState.UpdateMonsters(playerMonsters, enemyMonsters);
        return boardState;
    }

    // Helper to get all monsters
    public List<EntityManager> GetAllMonsters()
    {
        var allMonsters = new List<EntityManager>();
        allMonsters.AddRange(PlayerMonsters.Where(m => m != null && !m.dead && m.placed));
        allMonsters.AddRange(EnemyMonsters.Where(m => m != null && !m.dead && m.placed));
        return allMonsters;
    }

    // Calculate the estimated number of turns until game end
    public int EstimateRemainingTurns()
    {
        float playerDamagePerTurn = PlayerMonsters.Where(m => m != null && !m.dead && m.placed)
                                                .Sum(m => m.GetAttack());

        float enemyDamagePerTurn = EnemyMonsters.Where(m => m != null && !m.dead && m.placed)
                                               .Sum(m => m.GetAttack());

        // Avoid division by zero
        playerDamagePerTurn = Mathf.Max(playerDamagePerTurn, 1);
        enemyDamagePerTurn = Mathf.Max(enemyDamagePerTurn, 1);

        int turnsUntilPlayerDies = playerDamagePerTurn > 0 ? Mathf.CeilToInt(PlayerHealth / playerDamagePerTurn) : 99;
        int turnsUntilEnemyDies = enemyDamagePerTurn > 0 ? Mathf.CeilToInt(EnemyHealth / enemyDamagePerTurn) : 99;

        return Mathf.Min(turnsUntilPlayerDies, turnsUntilEnemyDies);
    }

    // Get a debug string representation of the board state
    public override string ToString()
    {
        return $"BoardState: Turn={TurnCount}, " +
               $"Phase={CurrentPhase}, " +
               $"Health={EnemyHealth}/{EnemyMaxHealth} vs {PlayerHealth}/{PlayerMaxHealth}, " +
               $"Monsters={EnemyMonsters.Count(m => m != null && !m.dead && m.placed)} vs " +
               $"{PlayerMonsters.Count(m => m != null && !m.dead && m.placed)}, " +
               $"Control={EnemyBoardControl:F1} vs {PlayerBoardControl:F1}";
    }
}
