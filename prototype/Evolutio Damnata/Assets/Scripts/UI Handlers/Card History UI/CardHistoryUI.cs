using CardSystem.History;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CardPlayHistoryUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject cardPlayEntryPrefab;

    private void OnEnable()
    {
        // Subscribe to CardHistory updates
        if (CardHistory.Instance != null)
        {
            CardHistory.OnHistoryChanged += HandleHistoryChanged;
            RefreshHistory();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from CardHistory updates
        CardHistory.OnHistoryChanged -= HandleHistoryChanged;
    }

    private void HandleHistoryChanged()
    {
        if (CardHistory.Instance != null)
        {
            RefreshHistory();
        }
    }

    private void RefreshHistory()
    {
        if (CardHistory.Instance == null) return;

        // Get both player and enemy card plays
        var playerCards = CardHistory.Instance.GetPlayerCardPlays();
        var enemyCards = CardHistory.Instance.GetEnemyCardPlays();

        // Combine and sort by turn number
        var allCards = playerCards.Concat(enemyCards)
            .OrderBy(card => card.TurnNumber)
            .ToList();

        Refresh(allCards);
    }

    public void Refresh(List<CardPlayRecord> history)
    {
        if (history == null)
        {
            Debug.LogWarning("[CardPlayHistoryUI] Received null history list");
            return;
        }

        // Clear old entries
        foreach (Transform child in contentParent)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        // Re-populate
        foreach (var record in history)
        {
            if (record == null) continue;

            GameObject entry = Instantiate(cardPlayEntryPrefab, contentParent);
            if (entry == null)
            {
                Debug.LogError("[CardPlayHistoryUI] Failed to instantiate card play entry prefab");
                continue;
            }

            var cardNameText = entry.transform.Find("CardNameText")?.GetComponent<TMP_Text>();
            var turnText = entry.transform.Find("TurnText")?.GetComponent<TMP_Text>();
            var ownerTag = entry.transform.Find("OwnerTag")?.GetComponent<TMP_Text>();
            var effectTargetsText = entry.transform.Find("EffectTargetsText")?.GetComponent<TMP_Text>();

            if (cardNameText != null) cardNameText.text = record.CardName;
            if (turnText != null) turnText.text = $"Turn {record.TurnNumber}";
            if (ownerTag != null) ownerTag.text = record.IsEnemyCard ? "Enemy" : "Player";
            if (effectTargetsText != null)
            {
                effectTargetsText.text = record.EffectTargets.Count > 0
                    ? string.Join("\n", record.EffectTargets)
                    : "";
            }
        }
    }
}
