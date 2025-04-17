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
        [SerializeField] private string targetName;
        [SerializeField] private int turnNumber;
        [SerializeField] private int manaUsed;
        [SerializeField] private string timestamp;
        [SerializeField] private bool isEnemyCard;
        [SerializeField] private string keywords;
        [SerializeField] private List<string> effectTargets = new List<string>();

        public string EditorSummary
        {
            get
            {
                string summary = $"Turn {turnNumber}: {(isEnemyCard ? "Enemy" : "Player")} played {cardName} ({manaUsed} mana)";

                if (!string.IsNullOrEmpty(keywords))
                    summary += $" [{keywords}]";

                if (!string.IsNullOrEmpty(targetName))
                    summary += $" targeting {targetName}";

                if (effectTargets.Count > 0)
                {
                    string effectsText = string.Join(", ", effectTargets);
                    summary += $" | Effects: {effectsText}";
                }

                summary += $" - {timestamp}";
                return summary;
            }
        }

        public bool IsEnemyCard => isEnemyCard;
        public string CardName => cardName;
        public int TurnNumber => turnNumber;
        public string Keywords => keywords;
        public string TargetName => targetName;
        public IReadOnlyList<string> EffectTargets => effectTargets;

        public CardPlayRecord(Card card, EntityManager target, int turn, int mana)
        {
            // Determine card ownership directly from the current game phase
            isEnemyCard = IsCurrentPhaseEnemyPhase();

            // Initialize basic card information
            InitializeCardInfo(card?.CardName, card?.Description, target, turn, mana);
            ExtractKeywords(card);

            // Record primary target
            if (target != null)
            {
                targetName = target.name;
            }

            // For Bloodprice effect, record that it also targets the caster
            if (HasBloodpriceEffect())
            {
                RecordBloodpriceTarget();
            }

            LogCardCreation();
        }

        public CardPlayRecord(CardDataWrapper cardWrapper, EntityManager target, int turn, int mana)
        {
            // Determine card ownership directly from the current game phase
            isEnemyCard = IsCurrentPhaseEnemyPhase();

            // Initialize basic card information
            InitializeCardInfo(cardWrapper?.CardName, cardWrapper?.Description, target, turn, mana);

            // Extract keywords from the wrapper
            if (cardWrapper?.EffectTypes != null && cardWrapper.EffectTypes.Count > 0)
            {
                keywords = string.Join(", ", cardWrapper.EffectTypes);
            }

            // Record primary target
            if (target != null)
            {
                targetName = target.name;
            }

            // For Bloodprice effect, record that it also targets the caster
            if (HasBloodpriceEffect())
            {
                RecordBloodpriceTarget();
            }

            LogCardCreation();
        }

        private void InitializeCardInfo(string name, string description, EntityManager entity, int turn, int mana)
        {
            cardName = name ?? "Unknown";
            cardDescription = description ?? string.Empty;
            turnNumber = turn;
            manaUsed = mana;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
            targetName = entity?.name ?? "None";
            keywords = string.Empty;
        }

        private void ExtractKeywords(Card card)
        {
            if (card == null) return;

            if (card is SpellCard spellCard && spellCard.EffectTypes?.Count > 0)
            {
                keywords = string.Join(", ", spellCard.EffectTypes);
            }
            else if (card.CardType?.Keywords?.Count > 0)
            {
                keywords = string.Join(", ", card.CardType.Keywords);
            }
        }

        private bool HasBloodpriceEffect()
        {
            return keywords.Contains("Bloodprice");
        }

        private void RecordBloodpriceTarget()
        {
            // Find the appropriate health icon based on card owner
            string bloodpriceTarget = isEnemyCard ? "Enemy Health" : "Player Health";
            effectTargets.Add($"Bloodprice → {bloodpriceTarget}");

            Debug.Log($"[CardHistory] Recorded bloodprice effect targeting {bloodpriceTarget} for card {cardName}");
        }

        private void LogCardCreation()
        {
            Debug.Log($"[CardHistory] Created record for '{cardName}' ({(isEnemyCard ? "Enemy" : "Player")})" +
                     $" targeting {targetName} with keywords: {keywords}");
        }

        // Helper method to determine if the current phase belongs to the enemy
        private bool IsCurrentPhaseEnemyPhase()
        {
            var combatManager = GameObject.FindObjectOfType<CombatManager>();
            if (combatManager == null) return false;

            ICombatManager combatManagerInterface = combatManager as ICombatManager;
            return combatManagerInterface != null &&
                (combatManagerInterface.IsEnemyPrepPhase() ||
                 combatManagerInterface.IsEnemyCombatPhase());
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
        [SerializeField] private List<Keywords.MonsterKeyword> attackerKeywords = new List<Keywords.MonsterKeyword>();

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
        public IReadOnlyList<Keywords.MonsterKeyword> AttackerKeywords => attackerKeywords;

        public AttackRecord(EntityManager attacker, EntityManager target, int turn, float damage, float counterDamage, bool isRanged)
        {
            attackerName = attacker.name;
            targetName = target.name;
            turnNumber = turn;
            damageDealt = damage;
            this.counterDamage = counterDamage;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
            wasRangedAttack = isRanged;
            isEnemyAttack = attacker.GetMonsterType() == EntityManager.MonsterType.Enemy;

            // Store the attacker's keywords
            if (attacker.GetCardData()?.Keywords != null)
            {
                attackerKeywords.AddRange(attacker.GetCardData().Keywords);
            }

            // If the attack is ranged but the Ranged keyword isn't in the list, add it
            // This ensures consistency with the wasRangedAttack flag
            if (isRanged && !attackerKeywords.Contains(Keywords.MonsterKeyword.Ranged))
            {
                attackerKeywords.Add(Keywords.MonsterKeyword.Ranged);
            }

            Debug.Log($"[CardHistory] Recorded attack: {attackerName} -> {targetName}, damage: {damage}, counter: {counterDamage}, isEnemyAttack: {isEnemyAttack}, keywords: {string.Join(", ", attackerKeywords)}");
        }
    }

    [System.Serializable]
    public class OngoingEffectRecord
    {
        [SerializeField] private string effectType;
        [SerializeField] private string targetName;
        [SerializeField] private int turnApplied;
        [SerializeField] private int initialDuration;
        [SerializeField] private int effectValue;
        [SerializeField] private string sourceName;
        [SerializeField] private bool isEnemyEffect;
        [SerializeField] private string timestamp;

        public string EditorSummary =>
            $"Turn {turnApplied}: {(isEnemyEffect ? "Enemy" : "Player")} applied {effectType} to {targetName} " +
            $"({effectValue} dmg/turn for {initialDuration} turns) from {sourceName} - {timestamp}";

        public bool IsEnemyEffect => isEnemyEffect;
        public string EffectType => effectType;
        public string TargetName => targetName;
        public int TurnApplied => turnApplied;
        public int RemainingDuration { get; set; }
        public int EffectValue => effectValue;

        public OngoingEffectRecord(IOngoingEffect effect, int duration, int turnNumber, string sourceCardName)
        {
            // Determine effect ownership directly from the current game phase
            isEnemyEffect = IsCurrentPhaseEnemyPhase();

            effectType = effect.EffectType.ToString();

            // Explicitly capture the target entity information
            if (effect.TargetEntity != null)
            {
                targetName = effect.TargetEntity.name;
            }
            else
            {
                targetName = "Unknown";
            }

            turnApplied = turnNumber;
            initialDuration = duration;
            RemainingDuration = duration;
            effectValue = effect.EffectValue;
            sourceName = sourceCardName ?? "Unknown";
            timestamp = DateTime.Now.ToString("HH:mm:ss");

            Debug.Log($"[CardHistory] Recorded ongoing effect: {effectType} targeting {targetName} for {effectValue}/turn over {duration} turns from {sourceName}");
        }

        // Helper method to determine if the current phase belongs to the enemy
        private bool IsCurrentPhaseEnemyPhase()
        {
            var combatManager = GameObject.FindObjectOfType<CombatManager>();
            if (combatManager == null) return false;

            ICombatManager combatManagerInterface = combatManager as ICombatManager;
            return combatManagerInterface != null &&
                (combatManagerInterface.IsEnemyPrepPhase() ||
                 combatManagerInterface.IsEnemyCombatPhase());
        }
    }

    [System.Serializable]
    public class OngoingEffectApplicationRecord
    {
        [SerializeField] private string effectType;
        [SerializeField] private string targetName;
        [SerializeField] private int turnApplied;
        [SerializeField] private int damageDealt;
        [SerializeField] private string timestamp;

        public string EditorSummary =>
            $"Turn {turnApplied}: {effectType} applied to {targetName} dealing {damageDealt} damage - {timestamp}";

        public string EffectType => effectType;
        public string TargetName => targetName;
        public int TurnApplied => turnApplied;
        public int DamageDealt => damageDealt;

        public OngoingEffectApplicationRecord(SpellEffect effectType, EntityManager target, int turnNumber, int damage)
        {
            this.effectType = effectType.ToString();
            targetName = target?.name ?? "Unknown";
            turnApplied = turnNumber;
            damageDealt = damage;
            timestamp = DateTime.Now.ToString("HH:mm:ss");

            Debug.Log($"[CardHistory] Recorded effect application: {this.effectType} applied to {targetName} for {damage} damage");
        }
    }

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
    public void RecordAdditionalEffectTarget(string effectName, string targetName)
    {
        if (cardHistory.Count == 0) return;

        var latestRecord = cardHistory[cardHistory.Count - 1];
        string effectInfo = $"{effectName} → {targetName}";

        // Add to effectTargets through reflection (since the field is private)
        var effectTargetsField = typeof(CardPlayRecord).GetField("effectTargets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (effectTargetsField != null)
        {
            var effectTargets = effectTargetsField.GetValue(latestRecord) as List<string>;
            if (effectTargets != null)
            {
                effectTargets.Add(effectInfo);
                Debug.Log($"[CardHistory] Added effect target: {effectInfo} to card {latestRecord.CardName}");
            }
        }
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

        // Check if attacker has Tough keyword and adjust counter damage
        // Tough units take half damage (rounded down) for counter attacks too
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
