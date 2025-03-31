using System;
using System.Collections.Generic;
using UnityEngine;

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

        public string EditorSummary =>
            $"Turn {turnNumber}: {playerName} played {cardName} ({manaUsed} mana) - {timestamp}";

        public CardPlayRecord(Card card, EntityManager player, int turn, int mana)
        {
            cardName = card.CardName;
            cardDescription = card.Description;
            playerName = player.name;
            turnNumber = turn;
            manaUsed = mana;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    [Header("History Settings")]
    [SerializeField] private bool keepHistoryBetweenGames = true;
    [SerializeField] private int maxHistorySize = 100;

    [Header("Card Play History")]
    [SerializeField] private List<CardPlayRecord> cardHistory = new List<CardPlayRecord>();
    
    [Header("Statistics")]
    [SerializeField] private int totalCardsPlayed;
    [SerializeField] private Dictionary<int, int> cardsPerTurn = new Dictionary<int, int>();
    [SerializeField] private Dictionary<string, int> cardsPlayedByType = new Dictionary<string, int>();

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

    public void RecordCardPlay(Card card, EntityManager player, int turnNumber, int manaUsed)
    {
        if (card == null || player == null)
        {
            Debug.LogWarning("Attempted to record card play with null card or player!");
            return;
        }

        // Create and add new record
        var record = new CardPlayRecord(card, player, turnNumber, manaUsed);
        cardHistory.Add(record);

        // Update statistics
        totalCardsPlayed++;
        
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
        Debug.Log("[CardHistory] History cleared");
    }

    public int GetTotalCardsPlayed()
    {
        return totalCardsPlayed;
    }

    public int GetCardsPlayedInTurn(int turnNumber)
    {
        return cardsPerTurn.ContainsKey(turnNumber) ? cardsPerTurn[turnNumber] : 0;
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
        
        Debug.Log($"\nTotal Cards Played: {totalCardsPlayed}");
        Debug.Log("\nCards Per Turn:");
        foreach (var kvp in cardsPerTurn)
        {
            Debug.Log($"Turn {kvp.Key}: {kvp.Value} cards");
        }
        
        Debug.Log("\nCards By Type:");
        foreach (var kvp in cardsPlayedByType)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} cards");
        }
    }
#endif
}
