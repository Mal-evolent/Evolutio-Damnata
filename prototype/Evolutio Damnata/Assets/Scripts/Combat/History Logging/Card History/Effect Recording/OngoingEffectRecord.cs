using System;
using UnityEngine;

namespace CardSystem.History
{
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
}
