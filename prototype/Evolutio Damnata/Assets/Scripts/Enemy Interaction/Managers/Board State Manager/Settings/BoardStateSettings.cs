using UnityEngine;

namespace EnemyInteraction.Managers
{
    [CreateAssetMenu(fileName = "BoardStateSettings", menuName = "EnemyInteraction/Board State Settings")]
    public class BoardStateSettings : ScriptableObject
    {
        [Header("Core Settings")]
        [Range(5, 20)] public int LateGameTurnThreshold = 10;
        [Range(1f, 1.5f)] public float LateGameBonusMultiplier = 1.1f;
        [Range(0.05f, 0.3f)] public float HealthInfluenceFactor = 0.2f;

        [Header("Advanced Board Control Settings")]
        [Range(0.1f, 0.5f)] public float BoardPresenceMultiplier = 0.25f;
        [Range(0.1f, 0.5f)] public float ResourceAdvantageWeight = 0.3f;
        [Range(0.0f, 1.0f)] public float CriticalHealthThreshold = 0.3f;

        [Header("Keyword Valuation")]
        [Range(1.1f, 1.5f)] public float TauntValue = 1.3f;
        [Range(1.1f, 1.5f)] public float RangedValue = 1.2f;
        [Range(1.1f, 1.5f)] public float ToughValue = 1.15f;
        [Range(1.1f, 1.5f)] public float OverwhelmValue = 1.25f;
        [Range(0.05f, 0.2f)] public float KeywordSynergyBonus = 0.1f;
    }
}
