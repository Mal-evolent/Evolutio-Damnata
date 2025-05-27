using System;
using System.Collections.Generic;
using UnityEngine;
using EnemyInteraction.Interfaces;

namespace CardSystem.History
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
        [SerializeField] private string editorSummary; // Add a backing field for EditorSummary

        // Change to allow setting the property
        public string EditorSummary { get => editorSummary; private set => editorSummary = value; }
        public bool IsEnemyCard => isEnemyCard;
        public string CardName => cardName;
        public int TurnNumber => turnNumber;
        public string Keywords => keywords;
        public string TargetName => targetName;

        public IReadOnlyList<string> EffectTargets => effectTargets.AsReadOnly();

        // Existing constructors and methods remain unchanged
        public CardPlayRecord(Card card, ICombatEntity target, int turn, int mana)
        {
            InitializeCardInfo(card.CardName, card.Description, target, turn, mana);
            ExtractKeywords(card);
            LogCardCreation();
        }

        public CardPlayRecord(CardDataWrapper cardWrapper, ICombatEntity target, int turn, int mana)
        {
            // Extract data from CardDataWrapper and pass to InitializeCardInfo
            InitializeCardInfo(cardWrapper.CardName, cardWrapper.Description, target, turn, mana);
            // Assuming keywords are not available in CardDataWrapper for now, or need a different extraction logic.
            // If keywords are needed, we might need to adjust CardDataWrapper or how this is handled.
            // For now, we'll skip ExtractKeywords for CardDataWrapper.
            LogCardCreation();
        }

        private void InitializeCardInfo(string name, string description, ICombatEntity entity, int turn, int mana)
        {
            cardName = name;
            cardDescription = description;
            targetName = entity != null ? entity.Name : "No Target";
            turnNumber = turn;
            manaUsed = mana;
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            isEnemyCard = IsCurrentPhaseEnemyPhase();

            EditorSummary = $"{(isEnemyCard ? "ENEMY" : "PLAYER")} played {cardName} on turn {turnNumber} targeting {targetName}";
        }

        private void ExtractKeywords(Card card)
        {
            // Implementation details omitted as they aren't shown in the original code snippet
        }

        private bool HasBloodpriceEffect()
        {
            // Implementation details omitted as they aren't shown in the original code snippet
            return false;
        }

        private void RecordBloodpriceTarget()
        {
            // Implementation details omitted as they aren't shown in the original code snippet
        }

        private void LogCardCreation()
        {
            Debug.Log($"[CardPlayRecord] Created record for {cardName} on turn {turnNumber}");
        }

        private bool IsCurrentPhaseEnemyPhase()
        {
            var combatManager = GameObject.FindObjectOfType<CombatManager>();
            if (combatManager == null) return false;

            ICombatManager combatManagerInterface = combatManager as ICombatManager;
            return combatManagerInterface != null &&
                (combatManagerInterface.IsEnemyPrepPhase() ||
                 combatManagerInterface.IsEnemyCombatPhase());
        }

        // Add method to add effect targets (this would be used by CardHistory.cs)
        public void AddEffectTarget(string effectInfo)
        {
            effectTargets.Add(effectInfo);
        }
    }
}
