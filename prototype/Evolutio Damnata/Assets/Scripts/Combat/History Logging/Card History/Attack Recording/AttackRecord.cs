using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardSystem.History
{
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
}
