using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CardHistory : MonoBehaviour, ICardHistory
{
    [System.Serializable]
    public class CardPlayRecord
    {
        [SerializeField] private string cardName;
        [SerializeField] private string cardDescription;
        [SerializeField] private string playerName;
        [SerializeField] private int turnNumber;
        [SerializeField] private int manaUsed;
        [SerializeField] private string timestamp;
        [SerializeField] private bool isEnemyCard;

        public string EditorSummary =>
            $"Turn {turnNumber}: {(isEnemyCard ? "Enemy" : "Player")} played {cardName} ({manaUsed} mana) - {timestamp}";

        public bool IsEnemyCard => isEnemyCard;
        public string CardName => cardName;
        public int TurnNumber => turnNumber;

        public CardPlayRecord(Card card, EntityManager entity, int turn, int mana)
        {
            cardName = card.CardName;
            cardDescription = card.Description;
            playerName = entity.name;
            turnNumber = turn;
            manaUsed = mana;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
            isEnemyCard = entity.GetMonsterType() == EntityManager.MonsterType.Enemy;
        }
        
        public CardPlayRecord(CardDataWrapper cardWrapper, EntityManager entity, int turn, int mana)
        {
            cardName = cardWrapper.CardName;
            cardDescription = cardWrapper.Description;
            playerName = entity.name;
            turnNumber = turn;
            manaUsed = mana;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
            isEnemyCard = entity.GetMonsterType() == EntityManager.MonsterType.Enemy;
        }
    }

    [Header("History Settings")]
    [SerializeField] private bool keepHistoryBetweenGames = true;
    [SerializeField] private int maxHistorySize = 100;

    [Header("Card Play History")]
    [SerializeField] private List<CardPlayRecord> cardHistory = new List<CardPlayRecord>();
    
    [Header("Statistics")]
    [SerializeField] private int totalCardsPlayed;
    [SerializeField] private int playerCardsPlayed;
    [SerializeField] private int enemyCardsPlayed;
    [SerializeField] private Dictionary<int, int> cardsPerTurn = new Dictionary<int, int>();
    [SerializeField] private Dictionary<string, int> cardsPlayedByType = new Dictionary<string, int>();
    [SerializeField] private Dictionary<string, int> playerCardsPlayedByType = new Dictionary<string, int>();
    [SerializeField] private Dictionary<string, int> enemyCardsPlayedByType = new Dictionary<string, int>();

    private static CardHistory instance;
    public static CardHistory Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CardHistory>();
                if (instance == null)
                {
                    Debug.LogError("No CardHistory found in scene!");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (!keepHistoryBetweenGames)
        {
            ClearHistory();
        }
    }

    public void RecordCardPlay(Card card, EntityManager entity, int turnNumber, int manaUsed)
    {
        if (card == null || entity == null)
        {
            Debug.LogWarning("Attempted to record card play with null card or entity!");
            return;
        }

        // Create and add new record
        var record = new CardPlayRecord(card, entity, turnNumber, manaUsed);
        cardHistory.Add(record);

        // Update statistics
        totalCardsPlayed++;
        
        // Update player/enemy specific counts
        if (record.IsEnemyCard)
        {
            enemyCardsPlayed++;
        }
        else
        {
            playerCardsPlayed++;
        }
        
        // Update cards per turn
        if (!cardsPerTurn.ContainsKey(turnNumber))
        {
            cardsPerTurn[turnNumber] = 0;
        }
        cardsPerTurn[turnNumber]++;

        // Update cards by type
        string cardType = card.GetType().Name;
        if (!cardsPlayedByType.ContainsKey(cardType))
        {
            cardsPlayedByType[cardType] = 0;
        }
        cardsPlayedByType[cardType]++;

        // Update player or enemy cards by type
        if (record.IsEnemyCard)
        {
            if (!enemyCardsPlayedByType.ContainsKey(cardType))
            {
                enemyCardsPlayedByType[cardType] = 0;
            }
            enemyCardsPlayedByType[cardType]++;
        }
        else
        {
            if (!playerCardsPlayedByType.ContainsKey(cardType))
            {
                playerCardsPlayedByType[cardType] = 0;
            }
            playerCardsPlayedByType[cardType]++;
        }

        // Trim history if needed
        if (cardHistory.Count > maxHistorySize)
        {
            cardHistory.RemoveAt(0);
        }

        Debug.Log($"[CardHistory] {record.EditorSummary}");
    }

    public void RecordCardPlay(CardDataWrapper cardWrapper, EntityManager entity, int turnNumber, int manaUsed)
    {
        if (cardWrapper == null || entity == null)
        {
            Debug.LogWarning("Attempted to record card play with null card wrapper or entity!");
            return;
        }

        // Create and add new record
        var record = new CardPlayRecord(cardWrapper, entity, turnNumber, manaUsed);
        cardHistory.Add(record);

        // Update statistics
        totalCardsPlayed++;
        
        // Update player/enemy specific counts
        if (record.IsEnemyCard)
        {
            enemyCardsPlayed++;
        }
        else
        {
            playerCardsPlayed++;
        }
        
        // Update cards per turn
        if (!cardsPerTurn.ContainsKey(turnNumber))
        {
            cardsPerTurn[turnNumber] = 0;
        }
        cardsPerTurn[turnNumber]++;

        // Update cards by type - use a generic "SpellCard" type since we don't have actual type info
        string cardType = "SpellCard";
        if (!cardsPlayedByType.ContainsKey(cardType))
        {
            cardsPlayedByType[cardType] = 0;
        }
        cardsPlayedByType[cardType]++;

        // Update player or enemy cards by type
        if (record.IsEnemyCard)
        {
            if (!enemyCardsPlayedByType.ContainsKey(cardType))
            {
                enemyCardsPlayedByType[cardType] = 0;
            }
            enemyCardsPlayedByType[cardType]++;
        }
        else
        {
            if (!playerCardsPlayedByType.ContainsKey(cardType))
            {
                playerCardsPlayedByType[cardType] = 0;
            }
            playerCardsPlayedByType[cardType]++;
        }

        // Trim history if needed
        if (cardHistory.Count > maxHistorySize)
        {
            cardHistory.RemoveAt(0);
        }

        Debug.Log($"[CardHistory] {record.EditorSummary}");
    }

    public void ClearHistory()
    {
        cardHistory.Clear();
        totalCardsPlayed = 0;
        cardsPerTurn.Clear();
        cardsPlayedByType.Clear();
        playerCardsPlayedByType.Clear();
        enemyCardsPlayedByType.Clear();
        Debug.Log("[CardHistory] History cleared");
    }

    public int GetTotalCardsPlayed()
    {
        return totalCardsPlayed;
    }
    
    public int GetPlayerCardsPlayed()
    {
        return playerCardsPlayed;
    }
    
    public int GetEnemyCardsPlayed()
    {
        return enemyCardsPlayed;
    }

    public int GetCardsPlayedInTurn(int turnNumber)
    {
        return cardsPerTurn.ContainsKey(turnNumber) ? cardsPerTurn[turnNumber] : 0;
    }
    
    public List<CardPlayRecord> GetEnemyCardPlays()
    {
        return cardHistory.Where(record => record.IsEnemyCard).ToList();
    }
    
    public List<CardPlayRecord> GetPlayerCardPlays()
    {
        return cardHistory.Where(record => !record.IsEnemyCard).ToList();
    }

    // Editor-only methods
#if UNITY_EDITOR
    [ContextMenu("Log Full History")]
    private void LogFullHistory()
    {
        Debug.Log("=== Card Play History ===");
        foreach (var record in cardHistory)
        {
            Debug.Log(record.EditorSummary);
        }
        
        Debug.Log($"\nTotal Cards Played: {totalCardsPlayed} (Player: {playerCardsPlayed}, Enemy: {enemyCardsPlayed})");
        Debug.Log("\nCards Per Turn:");
        foreach (var kvp in cardsPerTurn)
        {
            Debug.Log($"Turn {kvp.Key}: {kvp.Value} cards");
        }
        
        Debug.Log("\nCards By Type (All):");
        foreach (var kvp in cardsPlayedByType)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} cards");
        }
        
        Debug.Log("\nCards By Type (Player):");
        foreach (var kvp in playerCardsPlayedByType)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} cards");
        }
        
        Debug.Log("\nCards By Type (Enemy):");
        foreach (var kvp in enemyCardsPlayedByType)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} cards");
        }
    }
    
    [ContextMenu("Log Enemy Card History")]
    private void LogEnemyCardHistory()
    {
        var enemyCards = GetEnemyCardPlays();
        Debug.Log($"=== Enemy Card Play History ({enemyCards.Count} plays) ===");
        foreach (var record in enemyCards)
        {
            Debug.Log(record.EditorSummary);
        }
    }
    
    [ContextMenu("Log Player Card History")]
    private void LogPlayerCardHistory()
    {
        var playerCards = GetPlayerCardPlays();
        Debug.Log($"=== Player Card Play History ({playerCards.Count} plays) ===");
        foreach (var record in playerCards)
        {
            Debug.Log(record.EditorSummary);
        }
    }
#endif
}
