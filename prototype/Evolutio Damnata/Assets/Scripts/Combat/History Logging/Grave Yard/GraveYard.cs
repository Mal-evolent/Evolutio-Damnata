using System;
using System.Collections.Generic;
using UnityEngine;

public class GraveYard : MonoBehaviour, IGraveYard
{
    [Header("History Settings")]
    [SerializeField] private bool keepHistoryBetweenGames = true;
    [SerializeField] private int maxHistorySize = 100;

    [Header("Death Records")]
    [SerializeField] private List<DeadEntityRecord> graveyardHistory = new List<DeadEntityRecord>();
    
    [Header("Statistics")]
    [SerializeField] private int totalEntitiesBuried;
    [SerializeField] private Dictionary<EntityManager.MonsterType, int> entitiesByType = new Dictionary<EntityManager.MonsterType, int>();

    private CombatManager combatManager;
    private static GraveYard instance;

    public static GraveYard Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GraveYard>();
                if (instance == null)
                {
                    Debug.LogError("No GraveYard found in scene!");
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
            ClearGraveyard();
        }

        combatManager = FindObjectOfType<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogError("CombatManager not found in scene!");
        }
    }

    public bool KeepHistoryBetweenGames
    {
        get => keepHistoryBetweenGames;
        set => keepHistoryBetweenGames = value;
    }

    public int MaxHistorySize
    {
        get => maxHistorySize;
        set => maxHistorySize = value;
    }

    public void AddToGraveyard(EntityManager entity, EntityManager killedBy, float damage)
    {
        if (entity == null) return;

        var record = new DeadEntityRecord(entity, killedBy, damage, combatManager?.TurnCount ?? 0);
        AddRecordToGraveyard(record);
    }

    public void AddSpellKill(EntityManager entity, string spellName, float damage, bool isOngoingEffect = false)
    {
        if (entity == null) return;

        var record = new DeadEntityRecord(entity, spellName, damage, isOngoingEffect, combatManager?.TurnCount ?? 0);
        AddRecordToGraveyard(record);
    }

    private void AddRecordToGraveyard(DeadEntityRecord record)
    {
        graveyardHistory.Add(record);
        totalEntitiesBuried++;

        // Update type statistics
        if (!entitiesByType.ContainsKey(record.EntityType))
        {
            entitiesByType[record.EntityType] = 0;
        }
        entitiesByType[record.EntityType]++;

        // Trim history if needed
        if (graveyardHistory.Count > maxHistorySize)
        {
            graveyardHistory.RemoveAt(0);
        }

        Debug.Log($"[GraveYard] {record.EditorSummary}");
    }

    public List<DeadEntityRecord> GetGraveyardHistory()
    {
        return new List<DeadEntityRecord>(graveyardHistory);
    }

    public void ClearGraveyard()
    {
        graveyardHistory.Clear();
        totalEntitiesBuried = 0;
        entitiesByType.Clear();
        Debug.Log("[GraveYard] History cleared");
    }

    [System.Serializable]
    public class DeadEntityRecord
    {
        [SerializeField] private string entityName;
        [SerializeField] private EntityManager.MonsterType entityType;
        [SerializeField] private string killedByName;
        [SerializeField] private EntityManager.MonsterType killedByType;
        [SerializeField] private string timestamp;
        [SerializeField] private int turnNumber;
        [SerializeField] private float finalDamage;
        [SerializeField] private KillSource killSource;
        [SerializeField] private string spellName;

        public enum KillSource
        {
            Combat,
            Spell,
            OngoingEffect,
            Other
        }

        // Public properties to access private fields
        public string EntityName => entityName;
        public EntityManager.MonsterType EntityType => entityType;
        public string KilledByName => killedByName;
        public EntityManager.MonsterType KilledByType => killedByType;
        public string Timestamp => timestamp;
        public int TurnNumber => turnNumber;
        public float FinalDamage => finalDamage;
        public KillSource Source => killSource;
        public string SpellName => spellName;

        public string EditorSummary
        {
            get
            {
                string sourceInfo = killSource switch
                {
                    KillSource.Combat => $"was slain by {killedByName} ({killedByType})",
                    KillSource.Spell => $"was destroyed by spell: {spellName}",
                    KillSource.OngoingEffect => $"perished from {spellName} effect",
                    _ => "died from unknown causes"
                };

                return $"{entityName} ({entityType}) {sourceInfo} - {finalDamage} damage on turn {turnNumber} at {timestamp}";
            }
        }

        // Constructor for combat kills
        public DeadEntityRecord(EntityManager entity, EntityManager killedBy, float damage, int turn)
        {
            entityName = entity.name;
            entityType = entity.GetMonsterType();
            killedByName = killedBy != null ? killedBy.name : "Unknown";
            killedByType = killedBy != null ? killedBy.GetMonsterType() : EntityManager.MonsterType.Enemy;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
            turnNumber = turn;
            finalDamage = damage;
            killSource = KillSource.Combat;
        }

        // Constructor for spell kills
        public DeadEntityRecord(EntityManager entity, string spell, float damage, bool isOngoingEffect, int turn)
        {
            entityName = entity.name;
            entityType = entity.GetMonsterType();
            killedByName = "Spell Effect";
            killedByType = EntityManager.MonsterType.Enemy; // Default for spells
            spellName = spell;
            timestamp = DateTime.Now.ToString("HH:mm:ss");
            turnNumber = turn;
            finalDamage = damage;
            killSource = isOngoingEffect ? KillSource.OngoingEffect : KillSource.Spell;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Log Full History")]
    private void LogFullHistory()
    {
        Debug.Log("=== Graveyard History ===");
        foreach (var record in graveyardHistory)
        {
            Debug.Log(record.EditorSummary);
        }
        
        Debug.Log($"\nTotal Entities Buried: {totalEntitiesBuried}");
        Debug.Log("\nEntities By Type:");
        foreach (var kvp in entitiesByType)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} deaths");
        }
    }
#endif
}

