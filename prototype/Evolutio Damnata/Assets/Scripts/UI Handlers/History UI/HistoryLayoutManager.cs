using CardSystem.History;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class HistoryLayoutManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentParent;
    [SerializeField] private GameObject attackEntryPrefab;
    [SerializeField] private GameObject cardPlayEntryPrefab;

    [Header("Layout Settings")]
    [SerializeField] private float topPadding = 150f;
    [SerializeField] private float entrySpacing = 10f;
    [SerializeField] private float bottomPadding = 50f;
    [SerializeField] private float entryHeight = 1024.4f; // Standard height for layout calculation
    [SerializeField] private float scaleFactor = 0.3f; // Standard scale factor

    private void OnEnable()
    {
        if (CardHistory.Instance != null)
        {
            CardHistory.OnHistoryChanged += RefreshLayout;
            RefreshLayout();
        }
    }

    private void OnDisable()
    {
        if (CardHistory.Instance != null)
        {
            CardHistory.OnHistoryChanged -= RefreshLayout;
        }
    }

    private void RefreshLayout()
    {
        if (CardHistory.Instance == null || contentParent == null || attackEntryPrefab == null || cardPlayEntryPrefab == null) return;

        // Clear existing entries
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Get all history records and sort them chronologically
        var attackHistory = CardHistory.Instance.GetAttackHistory()?.Cast<object>();
        var cardPlayHistory = CardHistory.Instance.GetPlayerCardPlays().Concat(CardHistory.Instance.GetEnemyCardPlays()).Cast<object>();

        var allHistory = new List<object>();
        if (attackHistory != null) allHistory.AddRange(attackHistory);
        if (cardPlayHistory != null) allHistory.AddRange(cardPlayHistory);

        var sortedHistory = allHistory
            .Where(record => record is AttackRecord || record is CardPlayRecord || record is OngoingEffectRecord || record is OngoingEffectApplicationRecord)
            .OrderBy(record =>
            {
                // Primary sort key: TurnNumber / TurnApplied
                if (record is AttackRecord attack) return attack.TurnNumber;
                if (record is CardPlayRecord cardPlay) return cardPlay.TurnNumber;
                if (record is OngoingEffectRecord ongoingEffect) return ongoingEffect.TurnApplied;
                if (record is OngoingEffectApplicationRecord effectApplication) return effectApplication.TurnApplied;
                return int.MaxValue; // Should not happen with the Where clause, but as a fallback
            })
            .ThenBy(record =>
            {
                // Secondary sort key: Timestamp
                DateTime timestamp = DateTime.MinValue;
                try
                {
                    if (record is AttackRecord attack) DateTime.TryParseExact(attack.EditorSummary.Split(" - ").Last(), "HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out timestamp);
                    else if (record is CardPlayRecord cardPlay) DateTime.TryParseExact(cardPlay.EditorSummary.Split(" - ").Last(), "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out timestamp);
                    // Assuming other history types will also have a parsable timestamp in their summary or a dedicated field
                     else if (record is OngoingEffectRecord ongoingEffect) DateTime.TryParseExact(ongoingEffect.EditorSummary.Split(" - ").Last(), "HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out timestamp);
                    else if (record is OngoingEffectApplicationRecord effectApplication) DateTime.TryParseExact(effectApplication.EditorSummary.Split(" - ").Last(), "HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out timestamp);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HistoryLayoutManager] Error parsing timestamp for record type {record.GetType().Name}: {e.Message}. Summary: {record.GetType().GetProperty("EditorSummary")?.GetValue(record) ?? "N/A"}");
                }
                return timestamp;
            })
            .ToList();

        float currentVerticalOffset = topPadding;
        float scaledEntryHeight = entryHeight * scaleFactor;

        foreach (var record in sortedHistory)
        {
            GameObject prefabToInstantiate = null;
            // You would add more cases here for other history types
            if (record is AttackRecord)
            {
                prefabToInstantiate = attackEntryPrefab;
            }
            else if (record is CardPlayRecord)
            {
                prefabToInstantiate = cardPlayEntryPrefab;
            }

            if (prefabToInstantiate != null)
            {
                GameObject entryGO = Instantiate(prefabToInstantiate, contentParent);
                RectTransform entryRT = entryGO.GetComponent<RectTransform>();

                if (entryRT != null)
                {
                    entryRT.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                    entryRT.anchorMin = new Vector2(0.5f, 1f);
                    entryRT.anchorMax = new Vector2(0.5f, 1f);
                    entryRT.pivot = new Vector2(0.5f, 0.5f);
                    entryRT.anchoredPosition = new Vector2(0f, -currentVerticalOffset);
                    // Assuming prefabs have a consistent size for layout purposes, 
                    // or you might need to get size info from a component on the prefab
                    entryRT.sizeDelta = new Vector2(entryRT.sizeDelta.x, entryRT.sizeDelta.y); 
                }

                // Find a component on the entryGO that can set up the data
                // This assumes your entry prefabs have a script like IHistoryEntry or similar
                // For now, let's assume AttackEntryUI and CardPlayEntryUI scripts are on the prefabs
                if (record is AttackRecord attackRecord)
                {
                    var entryUI = entryGO.GetComponent<AttackEntryUI>();
                    if (entryUI != null) entryUI.Setup(attackRecord);
                }
                else if (record is CardPlayRecord cardPlayRecord)
                {
                     var entryUI = entryGO.GetComponent<CardPlayEntryUI>();
                    if (entryUI != null) entryUI.Setup(cardPlayRecord);
                }

                currentVerticalOffset += scaledEntryHeight + entrySpacing;
            }
        }

         contentParent.sizeDelta = new Vector2(contentParent.sizeDelta.x, currentVerticalOffset + bottomPadding - topPadding); // Adjust total height
    }
} 