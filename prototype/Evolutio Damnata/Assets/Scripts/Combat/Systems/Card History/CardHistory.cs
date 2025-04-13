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
            
            // Special handling for health icons - determine who PLAYED the card, not the target
            if (entity is HealthIconManager healthIcon)
            {
                // For spells on health icons, the card owner is the opposite of what the target is
                isEnemyCard = healthIcon.IsPlayerIcon; // if targeting player icon, it's an enemy card; if targeting enemy icon, it's a player card
                
                Debug.Log($"[CardHistory] Card played against health icon: {cardName}, target is player icon: {healthIcon.IsPlayerIcon}, isEnemyCard={isEnemyCard}");
            }
            else if (card is SpellCard)
            {
                // For spell cards targeting monsters, the owner is opposite of the target's type
                isEnemyCard = entity.GetMonsterType() == EntityManager.MonsterType.Friendly;
                
                Debug.Log($"[CardHistory] Spell card played against monster: {cardName}, target is {entity.GetMonsterType()}, isEnemyCard={isEnemyCard}");
            }
            else
            {
                // For monster cards, use the entity's type directly
                isEnemyCard = entity.GetMonsterType() == EntityManager.MonsterType.Enemy;
                
                Debug.Log($"[CardHistory] Monster card played: {cardName}, entity type is {entity.GetMonsterType()}, isEnemyCard={isEnemyCard}");
            }
        }
        
        public CardPlayRecord(CardDataWrapper cardWrapper, EntityManager entity, int turn, int mana)
        {
            cardName = cardWrapper.CardName;
            cardDescription = cardWrapper.Description;
            playerName = entity.name;
            turnNumber = turn;
            manaUsed = mana;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
            
            // Special handling for health icons - determine who PLAYED the card, not the target
            if (entity is HealthIconManager healthIcon)
            {
                // For spells on health icons, the card owner is the opposite of what the target is
                isEnemyCard = healthIcon.IsPlayerIcon; // if targeting player icon, it's an enemy card; if targeting enemy icon, it's a player card
                
                Debug.Log($"[CardHistory] Spell played against health icon: {cardName}, target is player icon: {healthIcon.IsPlayerIcon}, isEnemyCard={isEnemyCard}");
            }
            else
            {
                // For spell cards targeting monsters, the owner is opposite of the target's type
                isEnemyCard = entity.GetMonsterType() == EntityManager.MonsterType.Friendly;
                
                Debug.Log($"[CardHistory] Spell played against monster: {cardName}, target is {entity.GetMonsterType()}, isEnemyCard={isEnemyCard}");
            }
        }
    }

    [System.Serializable]
    public class AttackRecord
    {
        [SerializeField] private string attackerName;
        [SerializeField] private string targetName;
        [SerializeField] private int turnNumber;
        [SerializeField] private float damageDealt;
        [SerializeField] private string timestamp;
        [SerializeField] private bool isEnemyAttack;
        [SerializeField] private bool wasRangedAttack;
        [SerializeField] private float counterDamage;

        public string EditorSummary =>
            $"Turn {turnNumber}: {(isEnemyAttack ? "Enemy" : "Player")} {attackerName} attacked {targetName} for {damageDealt} damage" + 
            (wasRangedAttack ? " (Ranged)" : "") + 
            (!wasRangedAttack ? $" (Took {counterDamage} counter damage)" : "") +
            $" - {timestamp}";

        public bool IsEnemyAttack => isEnemyAttack;
        public string AttackerName => attackerName;
        public string TargetName => targetName;
        public int TurnNumber => turnNumber;
        public float DamageDealt => damageDealt;
        public bool WasRangedAttack => wasRangedAttack;

        public AttackRecord(EntityManager attacker, EntityManager target, int turn, float damage, float counterDamage, bool isRanged)
        {
            attackerName = attacker.name;
            targetName = target.name;
            turnNumber = turn;
            damageDealt = damage;
            this.counterDamage = counterDamage;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
            wasRangedAttack = isRanged;
            
            // Determine if this is an enemy attack based on attacker type
            isEnemyAttack = attacker.GetMonsterType() == EntityManager.MonsterType.Enemy;
            
            Debug.Log($"[CardHistory] Recorded attack: {attackerName} -> {targetName}, damage: {damage}, counter: {counterDamage}, isEnemyAttack: {isEnemyAttack}");
        }
    }

    [Header("History Settings")]
    [SerializeField] private bool keepHistoryBetweenGames = true;
    [SerializeField] private int maxHistorySize = 100;

    [Header("Card Play History")]
    [SerializeField] private List<CardPlayRecord> cardHistory = new List<CardPlayRecord>();
    
    [Header("Attack History")]
    [SerializeField] private List<AttackRecord> attackHistory = new List<AttackRecord>();
    
    [Header("Statistics")]
    [SerializeField] private int totalCardsPlayed;
    [SerializeField] private int playerCardsPlayed;
    [SerializeField] private int enemyCardsPlayed;
    [SerializeField] private int totalAttacks;
    [SerializeField] private int playerAttacks;
    [SerializeField] private int enemyAttacks;
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

        Debug.Log($"[CardHistory] Recording card play: {card.CardName} (IsSpellCard: {card is SpellCard}) by {(entity.GetMonsterType() == EntityManager.MonsterType.Enemy ? "Enemy" : "Player")}");

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

        Debug.Log($"[CardHistory] Recording spell card play: {cardWrapper.CardName} by {(entity.GetMonsterType() == EntityManager.MonsterType.Enemy ? "Enemy" : "Player")}");

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

    public void RecordAttack(EntityManager attacker, EntityManager target, int turnNumber, float damageDealt, float counterDamage, bool isRangedAttack)
    {
        if (attacker == null || target == null)
        {
            Debug.LogWarning("Attempted to record attack with null attacker or target!");
            return;
        }

        Debug.Log($"[CardHistory] Recording attack: {attacker.name} attacked {target.name} for {damageDealt} damage" +
                 (isRangedAttack ? " (Ranged)" : ""));

        // Create and add new record
        var record = new AttackRecord(attacker, target, turnNumber, damageDealt, counterDamage, isRangedAttack);
        attackHistory.Add(record);

        // Update statistics
        totalAttacks++;
        
        // Update player/enemy specific counts
        if (record.IsEnemyAttack)
        {
            enemyAttacks++;
        }
        else
        {
            playerAttacks++;
        }
        
        // Trim history if needed
        if (attackHistory.Count > maxHistorySize)
        {
            attackHistory.RemoveAt(0);
        }

        Debug.Log($"[CardHistory] {record.EditorSummary}");
    }

    public void ClearHistory()
    {
        cardHistory.Clear();
        attackHistory.Clear();
        totalCardsPlayed = 0;
        playerCardsPlayed = 0;
        enemyCardsPlayed = 0;
        totalAttacks = 0;
        playerAttacks = 0;
        enemyAttacks = 0;
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

    public void LogAllCardHistory()
    {
        Debug.Log("=== CARD HISTORY LOG ===");
        Debug.Log($"Total cards recorded: {cardHistory.Count}");

        Debug.Log("\n=== PLAYER CARDS ===");
        var playerCards = GetPlayerCardPlays();
        foreach (var record in playerCards)
        {
            Debug.Log($"- {record.CardName} (Turn {record.TurnNumber})");
        }

        Debug.Log("\n=== ENEMY CARDS ===");
        var enemyCards = GetEnemyCardPlays();
        foreach (var record in enemyCards)
        {
            Debug.Log($"- {record.CardName} (Turn {record.TurnNumber})");
        }

        Debug.Log("\n=== STATISTICS ===");
        Debug.Log($"Total Cards Played: {totalCardsPlayed} (Player: {playerCardsPlayed}, Enemy: {enemyCardsPlayed})");
        
        Debug.Log("\n=== CARDS BY TYPE ===");
        foreach (var kvp in cardsPlayedByType)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} cards");
        }
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
        
        Debug.Log("\n=== Attack History ===");
        foreach (var record in attackHistory)
        {
            Debug.Log(record.EditorSummary);
        }
        
        Debug.Log($"\nTotal Cards Played: {totalCardsPlayed} (Player: {playerCardsPlayed}, Enemy: {enemyCardsPlayed})");
        Debug.Log($"Total Attacks: {totalAttacks} (Player: {playerAttacks}, Enemy: {enemyAttacks})");
        
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

