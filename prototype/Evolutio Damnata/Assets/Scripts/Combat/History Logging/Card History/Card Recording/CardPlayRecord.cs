using System;
using System.Collections.Generic;
using UnityEngine;

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
            effectTargets.Add($"Bloodprice ? {bloodpriceTarget}");

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
}
