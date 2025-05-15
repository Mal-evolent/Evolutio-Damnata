using UnityEngine;

namespace EnemyInteraction.Managers
{
    [System.Serializable]
    public class CardPlaySettings
    {
        [Header("Card Evaluation Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance to make intentionally suboptimal plays")]
        public float SuboptimalPlayChance = 0.10f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Variance in card evaluation scores")]
        public float EvaluationVariance = 0.15f;

        [SerializeField, Range(0.2f, 2f), Tooltip("Delay between enemy actions in seconds")]
        public float ActionDelay = 0.5f;

        [SerializeField, Range(0f, 1f)]
        public float SkipCardPlayChance = 0.15f;

        [SerializeField]
        public float CardHoldBoardAdvantageThreshold = 1.3f;

        [SerializeField, Range(0f, 1f)]
        public float FutureValueMultiplier = 0.7f;

        [Header("Strategic Gameplay Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance to stop playing cards when in advantageous position")]
        public float StrategicStopChance = 0.3f;

        [SerializeField, Range(1f, 3f), Tooltip("Minimum board advantage ratio to consider stopping early")]
        public float EarlyStopBoardAdvantageThreshold = 1.2f;

        [SerializeField, Range(0f, 100f), Tooltip("Score threshold below which cards are considered low value")]
        public float LowValueCardThreshold = 60f;

        [SerializeField, Range(0f, 100f), Tooltip("Score threshold above which cards are considered high value")]
        public float HighValueCardThreshold = 70f;

        [SerializeField, Range(3, 15), Tooltip("Health threshold at which player is considered at low health")]
        public int PlayerLowHealthThreshold = 10;

        [SerializeField, Range(0f, 2f), Tooltip("Future value multiplier for expensive cards in early game")]
        public float EarlyGameExpensiveCardMultiplier = 1.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Chance to hold expensive cards for future turns")]
        public float HoldExpensiveCardChance = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("Chance to hold cards with high future value")]
        public float HoldHighFutureValueChance = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Factor for comparing future value to current value")]
        public float FutureToCurrentValueRatio = 0.7f;

        [Header("Initialization Settings")]
        [SerializeField, Range(5, 60), Tooltip("Maximum attempts when initializing critical components")]
        public int MaxInitializationAttempts = 30;

        [SerializeField, Range(0.05f, 1f), Tooltip("Delay between initialization attempts in seconds")]
        public float InitializationRetryDelay = 0.1f;

        [SerializeField, Range(1, 20), Tooltip("Minimum deck size to consider card conservation strategies")]
        public int LowDeckSizeThreshold = 10;

        [SerializeField, Range(0f, 1f), Tooltip("Chance to be selective with cards when deck size is low")]
        public float LowDeckSizeConservationChance = 0.4f;
    }
}
