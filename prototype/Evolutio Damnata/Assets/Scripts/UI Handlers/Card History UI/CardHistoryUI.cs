using CardSystem.History;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPlayHistoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentParent;
    [SerializeField] private GameObject cardPlayEntryPrefab;

    [Header("Layout Settings")]
    [Tooltip("Padding at the top of the content area")]
    [SerializeField] private float topPadding = 150f;
    [Tooltip("Spacing between card entries")]
    [SerializeField] private float entrySpacing = 10f;
    [Tooltip("Bottom padding after the last card entry")]
    [SerializeField] private float bottomPadding = 50f;

    [Header("Card Appearance")]
    [Tooltip("Height of the card entry prefab before scaling")]
    [SerializeField] private float entryHeight = 1024.4f;
    [Tooltip("Width of the card entry prefab before scaling")]
    [SerializeField] private float entryWidth = 1024f;
    [Tooltip("Scale factor for the card entries (1 = 100%)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float scaleFactor = 0.3f;

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
            Destroy(child.gameObject);

        float verticalOffset = 0f;                     // Total vertical position offset
        float scaledEntryHeight = entryHeight * scaleFactor; // Scaled height

        // Add initial padding to ensure first card is fully visible
        verticalOffset += topPadding;

        for (int i = 0; i < history.Count; i++)
        {
            var record = history[i];
            if (record == null) continue;

            GameObject entryGO = Instantiate(cardPlayEntryPrefab, contentParent);
            RectTransform entryRT = entryGO.GetComponent<RectTransform>();

            // Set scale according to the serialized scale factor
            entryRT.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

            // Center horizontally, position from top
            entryRT.anchorMin = new Vector2(0.5f, 1f);
            entryRT.anchorMax = new Vector2(0.5f, 1f);
            entryRT.pivot = new Vector2(0.5f, 0.5f); // Center pivot for better positioning

            // Set position manually (y goes down as offset grows)
            entryRT.anchoredPosition = new Vector2(0f, -verticalOffset);

            // Use serialized width and height
            entryRT.sizeDelta = new Vector2(entryWidth, entryHeight);

            // Apply data
            var entryUI = entryGO.GetComponent<CardPlayEntryUI>();
            if (entryUI != null) entryUI.Setup(record);

            // Use scaled height for calculating offset
            verticalOffset += scaledEntryHeight + entrySpacing;
        }

        // Set content height to fit all entries plus bottom padding
        contentParent.sizeDelta = new Vector2(contentParent.sizeDelta.x, verticalOffset + bottomPadding);
    }
}
