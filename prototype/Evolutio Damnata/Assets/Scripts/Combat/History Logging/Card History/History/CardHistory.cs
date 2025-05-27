// Assets/Scripts/Combat/Systems/Card History/CardHistory.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardSystem.History;

public class CardHistory : MonoBehaviour, ICardHistory
{
    // Add event for history changes
    public static event Action OnHistoryChanged;

    [Header("History Settings")]
    [SerializeField] private bool keepHistoryBetweenGames = true;
    [SerializeField] private int maxHistorySize = 100;

    [Header("Card Play History")]
    [SerializeField] private List<CardPlayRecord> cardHistory = new List<CardPlayRecord>();

    [Header("Attack History")]
    [SerializeField] private List<AttackRecord> attackHistory = new List<AttackRecord>();

    [Header("Ongoing Effect History")]
    [SerializeField] private List<OngoingEffectRecord> ongoingEffectHistory = new List<OngoingEffectRecord>();

    [Header("Effect Application History")]
    [SerializeField] private List<OngoingEffectApplicationRecord> effectApplicationHistory = new List<OngoingEffectApplicationRecord>();

    [Header("Statistics")]
    [SerializeField] private int totalCardsPlayed;
    [SerializeField] private int playerCardsPlayed;
    [SerializeField] private int enemyCardsPlayed;
    [SerializeField] private int totalAttacks;
    [SerializeField] private int playerAttacks;
    [SerializeField] private int enemyAttacks;
    [SerializeField] private int totalOngoingEffects;
    [SerializeField] private int playerOngoingEffects;
    [SerializeField] private int enemyOngoingEffects;
    [SerializeField] private Dictionary<int, int> cardsPerTurn = new Dictionary<int, int>();
    [SerializeField] private Dictionary<string, int> cardsPlayedByType = new Dictionary<string, int>();
    [SerializeField] private Dictionary<string, int> playerCardsPlayedByType = new Dictionary<string, int>();
    [SerializeField] private Dictionary<string, int> enemyCardsPlayedByType = new Dictionary<string, int>();
    [SerializeField] private Dictionary<string, int> effectsByType = new Dictionary<string, int>();

    private static CardHistory instance;
    public static CardHistory Instance => instance ?? (instance = FindObjectOfType<CardHistory>());

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
        if (card == null)
        {
            Debug.LogWarning("[CardHistory] Attempted to record card play with null card!");
            return;
        }

        var record = new CardPlayRecord(card, entity, turnNumber, manaUsed);
        AddCardRecord(record, card.GetType().Name);
    }

    public void RecordCardPlay(CardDataWrapper cardWrapper, EntityManager entity, int turnNumber, int manaUsed)
    {
        if (cardWrapper == null)
        {
            Debug.LogWarning("[CardHistory] Attempted to record card play with null card wrapper!");
            return;
        }

        var record = new CardPlayRecord(cardWrapper, entity, turnNumber, manaUsed);
        AddCardRecord(record, "SpellCard");
    }

    // Records an additional effect target for the most recently played card
    // Useful for spells with multiple effects targeting different entities
    // Records an additional effect target for the most recently played card
    // Useful for spells with multiple effects targeting different entities
    public void RecordAdditionalEffectTarget(string effectName, string targetName)
    {
        if (cardHistory.Count == 0) return;

        var latestRecord = cardHistory[cardHistory.Count - 1];
        string effectInfo = $"{effectName} → {targetName}";

        // Use the AddEffectTarget method instead of reflection
        latestRecord.AddEffectTarget(effectInfo);
        Debug.Log($"[CardHistory] Added effect target: {effectInfo} to card {latestRecord.CardName}");
    }


    /// <summary>
    /// Records a new ongoing effect being applied to an entity
    /// </summary>
    public void RecordOngoingEffect(IOngoingEffect effect, int duration, string sourceCardName, int turnNumber = -1)
    {
        if (effect == null)
        {
            Debug.LogWarning("[CardHistory] Attempted to record ongoing effect with null effect!");
            return;
        }

        // Log the target entity even if it's null
        if (effect.TargetEntity == null)
        {
            Debug.LogWarning("[CardHistory] Ongoing effect has null target entity. Recording with 'Unknown' target.");
        }
        else
        {
            Debug.Log($"[CardHistory] Recording ongoing effect targeting {effect.TargetEntity.name}");
        }

        // Get current turn if not provided
        if (turnNumber < 0)
        {
            var combatManager = GameObject.FindObjectOfType<CombatManager>();
            turnNumber = combatManager != null ? combatManager.TurnCount : 1;
        }

        var record = new OngoingEffectRecord(effect, duration, turnNumber, sourceCardName);
        AddOngoingEffectRecord(record);

        // Only add the effect target to the card history once
        if (effect.TargetEntity != null && !string.IsNullOrEmpty(sourceCardName))
        {
            RecordAdditionalEffectTarget(effect.EffectType.ToString(), effect.TargetEntity.name);
        }
    }

    /// <summary>
    /// Records an application of an ongoing effect (e.g., Burn damage)
    /// </summary>
    public void RecordEffectApplication(SpellEffect effectType, EntityManager target, int damage, int turnNumber = -1)
    {
        if (target == null)
        {
            Debug.LogWarning("[CardHistory] Attempted to record effect application with null target!");
            return;
        }

        // Get current turn if not provided
        if (turnNumber < 0)
        {
            var combatManager = GameObject.FindObjectOfType<CombatManager>();
            turnNumber = combatManager != null ? combatManager.TurnCount : 1;
        }

        var record = new OngoingEffectApplicationRecord(effectType, target, turnNumber, damage);
        effectApplicationHistory.Add(record);

        if (effectApplicationHistory.Count > maxHistorySize)
        {
            effectApplicationHistory.RemoveAt(0);
        }

        Debug.Log($"[CardHistory] {record.EditorSummary}");
    }

    private void AddCardRecord(CardPlayRecord record, string cardType)
    {
        cardHistory.Add(record);
        totalCardsPlayed++;

        if (record.IsEnemyCard)
        {
            enemyCardsPlayed++;
            UpdateCardTypeCount(enemyCardsPlayedByType, cardType);
        }
        else
        {
            playerCardsPlayed++;
            UpdateCardTypeCount(playerCardsPlayedByType, cardType);
        }

        UpdateTurnCount(record.TurnNumber);
        UpdateCardTypeCount(cardsPlayedByType, cardType);

        if (cardHistory.Count > maxHistorySize)
        {
            cardHistory.RemoveAt(0);
        }

        Debug.Log($"[CardHistory] {record.EditorSummary}");
        
        // Notify subscribers of history change
        OnHistoryChanged?.Invoke();
    }

    private void AddOngoingEffectRecord(OngoingEffectRecord record)
    {
        ongoingEffectHistory.Add(record);
        totalOngoingEffects++;

        if (record.IsEnemyEffect)
        {
            enemyOngoingEffects++;
        }
        else
        {
            playerOngoingEffects++;
        }

        UpdateEffectTypeCount(record.EffectType);

        if (ongoingEffectHistory.Count > maxHistorySize)
        {
            ongoingEffectHistory.RemoveAt(0);
        }

        Debug.Log($"[CardHistory] {record.EditorSummary}");
    }

    private void UpdateTurnCount(int turnNumber)
    {
        if (!cardsPerTurn.ContainsKey(turnNumber))
        {
            cardsPerTurn[turnNumber] = 0;
        }
        cardsPerTurn[turnNumber]++;
    }

    private void UpdateCardTypeCount(Dictionary<string, int> dictionary, string cardType)
    {
        if (!dictionary.ContainsKey(cardType))
        {
            dictionary[cardType] = 0;
        }
        dictionary[cardType]++;
    }

    private void UpdateEffectTypeCount(string effectType)
    {
        if (!effectsByType.ContainsKey(effectType))
        {
            effectsByType[effectType] = 0;
        }
        effectsByType[effectType]++;
    }

    public void RecordAttack(EntityManager attacker, EntityManager target, int turnNumber, float damageDealt, float counterDamage, bool isRangedAttack)
    {
        if (attacker == null || target == null)
        {
            Debug.LogWarning("[CardHistory] Attempted to record attack with null attacker or target!");
            return;
        }

        // Check if target has Tough keyword and adjust the damage dealt accordingly
        if (target.HasKeyword(Keywords.MonsterKeyword.Tough))
        {
            damageDealt = Mathf.Floor(damageDealt / 2f);
            Debug.Log($"[CardHistory] Adjusted damage dealt to Tough unit {target.name}: {damageDealt} (halved)");
        }

        // Check if attacker has Tough keyword and adjust counter damage
        if (attacker.HasKeyword(Keywords.MonsterKeyword.Tough))
        {
            counterDamage = Mathf.Floor(counterDamage / 2f);
            Debug.Log($"[CardHistory] Adjusted counter damage for Tough unit {attacker.name}: {counterDamage} (halved)");
        }

        var record = new AttackRecord(attacker, target, turnNumber, damageDealt, counterDamage, isRangedAttack);
        attackHistory.Add(record);

        totalAttacks++;
        if (record.IsEnemyAttack) enemyAttacks++; else playerAttacks++;

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
        ongoingEffectHistory.Clear();
        effectApplicationHistory.Clear();

        totalCardsPlayed = 0;
        playerCardsPlayed = 0;
        enemyCardsPlayed = 0;
        totalAttacks = 0;
        playerAttacks = 0;
        enemyAttacks = 0;
        totalOngoingEffects = 0;
        playerOngoingEffects = 0;
        enemyOngoingEffects = 0;

        cardsPerTurn.Clear();
        cardsPlayedByType.Clear();
        playerCardsPlayedByType.Clear();
        enemyCardsPlayedByType.Clear();
        effectsByType.Clear();

        Debug.Log("[CardHistory] History cleared");
        
        // Notify subscribers of history change
        OnHistoryChanged?.Invoke();
    }

    public int GetTotalCardsPlayed() => totalCardsPlayed;
    public int GetPlayerCardsPlayed() => playerCardsPlayed;
    public int GetEnemyCardsPlayed() => enemyCardsPlayed;
    public int GetCardsPlayedInTurn(int turnNumber) => cardsPerTurn.ContainsKey(turnNumber) ? cardsPerTurn[turnNumber] : 0;
    public List<CardPlayRecord> GetEnemyCardPlays() => cardHistory.Where(record => record.IsEnemyCard).ToList();
    public List<CardPlayRecord> GetPlayerCardPlays() => cardHistory.Where(record => !record.IsEnemyCard).ToList();

    // Additional getter methods for the new history types
    public List<OngoingEffectRecord> GetOngoingEffectHistory() => ongoingEffectHistory.ToList();
    public List<OngoingEffectApplicationRecord> GetEffectApplicationHistory() => effectApplicationHistory.ToList();
    public List<OngoingEffectRecord> GetPlayerOngoingEffects() => ongoingEffectHistory.Where(record => !record.IsEnemyEffect).ToList();
    public List<OngoingEffectRecord> GetEnemyOngoingEffects() => ongoingEffectHistory.Where(record => record.IsEnemyEffect).ToList();

    public void LogAllCardHistory()
    {
        Debug.Log("=== CARD HISTORY LOG ===");
        Debug.Log($"Total cards recorded: {cardHistory.Count}");

        Debug.Log("\n=== PLAYER CARDS ===");
        foreach (var record in GetPlayerCardPlays())
        {
            Debug.Log($"- {record.CardName} (Turn {record.TurnNumber})");
        }

        Debug.Log("\n=== ENEMY CARDS ===");
        foreach (var record in GetEnemyCardPlays())
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

        Debug.Log("\n=== Ongoing Effect History ===");
        foreach (var record in ongoingEffectHistory)
        {
            Debug.Log(record.EditorSummary);
        }

        Debug.Log("\n=== Effect Application History ===");
        foreach (var record in effectApplicationHistory)
        {
            Debug.Log(record.EditorSummary);
        }

        Debug.Log($"\nTotal Cards Played: {totalCardsPlayed} (Player: {playerCardsPlayed}, Enemy: {enemyCardsPlayed})");
        Debug.Log($"Total Attacks: {totalAttacks} (Player: {playerAttacks}, Enemy: {enemyAttacks})");
        Debug.Log($"Total Ongoing Effects: {totalOngoingEffects} (Player: {playerOngoingEffects}, Enemy: {enemyOngoingEffects})");

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

        Debug.Log("\nEffects By Type:");
        foreach (var kvp in effectsByType)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} effects");
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

    [ContextMenu("Log Ongoing Effect History")]
    private void LogOngoingEffectHistory()
    {
        Debug.Log($"=== Ongoing Effect History ({ongoingEffectHistory.Count} effects) ===");
        foreach (var record in ongoingEffectHistory)
        {
            Debug.Log(record.EditorSummary);
        }

        Debug.Log($"\n=== Player Ongoing Effects ({playerOngoingEffects} effects) ===");
        foreach (var record in GetPlayerOngoingEffects())
        {
            Debug.Log(record.EditorSummary);
        }

        Debug.Log($"\n=== Enemy Ongoing Effects ({enemyOngoingEffects} effects) ===");
        foreach (var record in GetEnemyOngoingEffects())
        {
            Debug.Log(record.EditorSummary);
        }
    }

    [ContextMenu("Log Effect Applications")]
    private void LogEffectApplicationHistory()
    {
        Debug.Log($"=== Effect Application History ({effectApplicationHistory.Count} applications) ===");
        foreach (var record in effectApplicationHistory)
        {
            Debug.Log(record.EditorSummary);
        }
    }
#endif
}
