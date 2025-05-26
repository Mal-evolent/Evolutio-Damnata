using System;
using UnityEngine;

namespace CardSystem.History
{
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
}
