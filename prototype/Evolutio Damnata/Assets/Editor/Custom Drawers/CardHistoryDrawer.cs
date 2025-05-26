// Assets/Editor/Custom Drawers/CardHistoryDrawer.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using CardSystem.History;

[CustomPropertyDrawer(typeof(CardPlayRecord))]
public class CardPlayRecordDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var cardName = property.FindPropertyRelative("cardName");
        var cardDescription = property.FindPropertyRelative("cardDescription");
        var targetName = property.FindPropertyRelative("targetName");
        var turnNumber = property.FindPropertyRelative("turnNumber");
        var manaUsed = property.FindPropertyRelative("manaUsed");
        var timestamp = property.FindPropertyRelative("timestamp");
        var isEnemyCard = property.FindPropertyRelative("isEnemyCard");
        var keywords = property.FindPropertyRelative("keywords");
        var effectTargets = property.FindPropertyRelative("effectTargets");

        if (cardName == null || turnNumber == null)
        {
            EditorGUI.LabelField(position, "Invalid Card Record");
            EditorGUI.EndProperty();
            return;
        }

        // Determine player type for display
        string playerType = isEnemyCard != null && isEnemyCard.boolValue ? "Enemy" : "Player";

        // Use custom colors for enemy vs player
        Color defaultColor = GUI.color;
        if (isEnemyCard != null && isEnemyCard.boolValue)
        {
            GUI.color = new Color(1f, 0.7f, 0.7f); // Light red for enemy
        }
        else
        {
            GUI.color = new Color(0.7f, 1f, 0.7f); // Light green for player
        }

        // Draw foldout header with safe string access
        var headerText = $"Turn {turnNumber.intValue}: {playerType} → {cardName.stringValue ?? "Unknown"} ({manaUsed?.intValue ?? 0} mana)";

        // Add keywords to header if available
        if (keywords != null && !string.IsNullOrEmpty(keywords.stringValue))
        {
            headerText += $" [{keywords.stringValue}]";
        }

        // Add target to header if available
        if (targetName != null && !string.IsNullOrEmpty(targetName.stringValue) && targetName.stringValue != "None")
        {
            headerText += $" → {targetName.stringValue}";
        }

        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerText);

        // Reset color
        GUI.color = defaultColor;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight;

            // Draw details with null checks
            DrawField("Card:", cardName?.stringValue ?? "Unknown", ref y);

            // Only draw description if it's not empty
            if (!string.IsNullOrEmpty(cardDescription?.stringValue))
            {
                DrawField("Description:", cardDescription.stringValue, ref y);
            }

            DrawField("Player Type:", playerType, ref y);

            // Show target name with highlighted style
            if (targetName != null && !string.IsNullOrEmpty(targetName.stringValue) && targetName.stringValue != "None")
            {
                GUI.color = new Color(0.0f, 0.5f, 0.0f); // Green for target
                DrawField("Target:", targetName.stringValue, ref y);
                GUI.color = defaultColor;
            }

            // Display effect targets with highlighted style
            if (effectTargets != null && effectTargets.arraySize > 0)
            {
                GUI.color = new Color(0.0f, 0.0f, 0.7f); // Blue for effect targets
                DrawField("Effect Targets:", "", ref y);

                for (int i = 0; i < effectTargets.arraySize; i++)
                {
                    var effectTarget = effectTargets.GetArrayElementAtIndex(i);
                    DrawField($"  • ", effectTarget.stringValue, ref y);
                }

                GUI.color = defaultColor;
            }

            DrawField("Turn:", turnNumber?.intValue.ToString() ?? "0", ref y);
            DrawField("Mana Used:", manaUsed?.intValue.ToString() ?? "0", ref y);

            // Only show keywords if they exist
            if (keywords != null && !string.IsNullOrEmpty(keywords.stringValue))
            {
                DrawField("Keywords:", keywords.stringValue, ref y);
            }

            DrawField("Time:", timestamp?.stringValue ?? "Unknown", ref y);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawField(string label, string value, ref float y)
    {
        var rect = new Rect(30, y, 300, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rect, $"{label} {value}");
        y += EditorGUIUtility.singleLineHeight;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
        {
            var cardDescription = property.FindPropertyRelative("cardDescription");
            var targetName = property.FindPropertyRelative("targetName");
            var keywords = property.FindPropertyRelative("keywords");
            var effectTargets = property.FindPropertyRelative("effectTargets");

            // Start with base height for the always-shown fields
            int fieldCount = 5; // Card, Player Type, Turn, Mana Used, Time

            // Add optional fields
            if (!string.IsNullOrEmpty(cardDescription?.stringValue)) fieldCount++;
            if (targetName != null && !string.IsNullOrEmpty(targetName.stringValue) && targetName.stringValue != "None") fieldCount++;
            if (keywords != null && !string.IsNullOrEmpty(keywords.stringValue)) fieldCount++;

            // Add effect targets count
            if (effectTargets != null && effectTargets.arraySize > 0)
            {
                fieldCount += effectTargets.arraySize + 1; // +1 for the header
            }

            return EditorGUIUtility.singleLineHeight * (fieldCount + 1); // +1 for the header
        }
        return EditorGUIUtility.singleLineHeight;
    }
}

[CustomPropertyDrawer(typeof(OngoingEffectRecord))]
public class OngoingEffectRecordDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var effectType = property.FindPropertyRelative("effectType");
        var targetName = property.FindPropertyRelative("targetName");
        var turnApplied = property.FindPropertyRelative("turnApplied");
        var initialDuration = property.FindPropertyRelative("initialDuration");
        var effectValue = property.FindPropertyRelative("effectValue");
        var sourceName = property.FindPropertyRelative("sourceName");
        var isEnemyEffect = property.FindPropertyRelative("isEnemyEffect");
        var timestamp = property.FindPropertyRelative("timestamp");

        if (effectType == null || targetName == null)
        {
            EditorGUI.LabelField(position, "Invalid Effect Record");
            EditorGUI.EndProperty();
            return;
        }

        // Determine owner type for display
        string ownerType = isEnemyEffect != null && isEnemyEffect.boolValue ? "Enemy" : "Player";

        // Use custom colors for enemy vs player
        Color defaultColor = GUI.color;
        if (isEnemyEffect != null && isEnemyEffect.boolValue)
        {
            GUI.color = new Color(1f, 0.7f, 0.7f); // Light red for enemy
        }
        else
        {
            GUI.color = new Color(0.7f, 1f, 0.7f); // Light green for player
        }

        // Draw foldout header with safe string access
        var headerText = $"Turn {turnApplied?.intValue ?? 0}: {ownerType} {effectType?.stringValue ?? "Unknown"} → {targetName?.stringValue ?? "Unknown"} ({effectValue?.intValue ?? 0} dmg/{initialDuration?.intValue ?? 0} turns)";
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerText);

        // Reset color
        GUI.color = defaultColor;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight;

            // Draw details with null checks
            DrawField("Effect Type:", effectType?.stringValue ?? "Unknown", ref y);

            // Highlight target name
            GUI.color = new Color(0.0f, 0.5f, 0.0f); // Green for target
            DrawField("Target:", targetName?.stringValue ?? "Unknown", ref y);
            GUI.color = defaultColor;

            DrawField("Source Card:", sourceName?.stringValue ?? "Unknown", ref y);
            DrawField("Owner:", ownerType, ref y);
            DrawField("Effect Value:", effectValue?.intValue.ToString() ?? "0", ref y);
            DrawField("Duration:", initialDuration?.intValue.ToString() ?? "0", ref y);
            DrawField("Turn Applied:", turnApplied?.intValue.ToString() ?? "0", ref y);
            DrawField("Time:", timestamp?.stringValue ?? "Unknown", ref y);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawField(string label, string value, ref float y)
    {
        var rect = new Rect(30, y, 300, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rect, $"{label} {value}");
        y += EditorGUIUtility.singleLineHeight;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight * 9; // Header + 8 fields
        }
        return EditorGUIUtility.singleLineHeight;
    }
}

[CustomPropertyDrawer(typeof(OngoingEffectApplicationRecord))]
public class OngoingEffectApplicationRecordDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var effectType = property.FindPropertyRelative("effectType");
        var targetName = property.FindPropertyRelative("targetName");
        var turnApplied = property.FindPropertyRelative("turnApplied");
        var damageDealt = property.FindPropertyRelative("damageDealt");
        var timestamp = property.FindPropertyRelative("timestamp");

        if (effectType == null || targetName == null)
        {
            EditorGUI.LabelField(position, "Invalid Effect Application Record");
            EditorGUI.EndProperty();
            return;
        }

        // Draw foldout header with safe string access
        var headerText = $"Turn {turnApplied?.intValue ?? 0}: {effectType?.stringValue ?? "Unknown"} → {targetName?.stringValue ?? "Unknown"} ({damageDealt?.intValue ?? 0} damage)";
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerText);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight;

            // Draw details with null checks
            DrawField("Effect Type:", effectType?.stringValue ?? "Unknown", ref y);

            // Highlight target name
            Color defaultColor = GUI.color;
            GUI.color = new Color(0.0f, 0.5f, 0.0f); // Green for target
            DrawField("Target:", targetName?.stringValue ?? "Unknown", ref y);
            GUI.color = defaultColor;

            DrawField("Damage Dealt:", damageDealt?.intValue.ToString() ?? "0", ref y);
            DrawField("Turn Applied:", turnApplied?.intValue.ToString() ?? "0", ref y);
            DrawField("Time:", timestamp?.stringValue ?? "Unknown", ref y);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawField(string label, string value, ref float y)
    {
        var rect = new Rect(30, y, 300, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rect, $"{label} {value}");
        y += EditorGUIUtility.singleLineHeight;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight * 6; // Header + 5 fields
        }
        return EditorGUIUtility.singleLineHeight;
    }
}

[CustomEditor(typeof(CardHistory))]
public class CardHistoryEditor : Editor
{
    private SerializedProperty keepHistoryBetweenGames;
    private SerializedProperty maxHistorySize;
    private SerializedProperty totalCardsPlayed;
    private SerializedProperty playerCardsPlayed;
    private SerializedProperty enemyCardsPlayed;
    private SerializedProperty totalAttacks;
    private SerializedProperty playerAttacks;
    private SerializedProperty enemyAttacks;
    private SerializedProperty totalOngoingEffects;
    private SerializedProperty playerOngoingEffects;
    private SerializedProperty enemyOngoingEffects;
    private SerializedProperty cardsPerTurn;
    private SerializedProperty cardsPlayedByType;
    private SerializedProperty playerCardsPlayedByType;
    private SerializedProperty enemyCardsPlayedByType;
    private SerializedProperty effectsByType;
    private SerializedProperty cardHistory;
    private SerializedProperty attackHistory;
    private SerializedProperty ongoingEffectHistory;
    private SerializedProperty effectApplicationHistory;

    private bool showCardHistory = true;
    private bool showAttackHistory = true;
    private bool showOngoingEffectHistory = true;
    private bool showEffectApplicationHistory = true;

    private void OnEnable()
    {
        // Cache serialized properties
        keepHistoryBetweenGames = serializedObject.FindProperty("keepHistoryBetweenGames");
        maxHistorySize = serializedObject.FindProperty("maxHistorySize");
        totalCardsPlayed = serializedObject.FindProperty("totalCardsPlayed");
        playerCardsPlayed = serializedObject.FindProperty("playerCardsPlayed");
        enemyCardsPlayed = serializedObject.FindProperty("enemyCardsPlayed");
        totalAttacks = serializedObject.FindProperty("totalAttacks");
        playerAttacks = serializedObject.FindProperty("playerAttacks");
        enemyAttacks = serializedObject.FindProperty("enemyAttacks");
        totalOngoingEffects = serializedObject.FindProperty("totalOngoingEffects");
        playerOngoingEffects = serializedObject.FindProperty("playerOngoingEffects");
        enemyOngoingEffects = serializedObject.FindProperty("enemyOngoingEffects");
        cardsPerTurn = serializedObject.FindProperty("cardsPerTurn");
        cardsPlayedByType = serializedObject.FindProperty("cardsPlayedByType");
        playerCardsPlayedByType = serializedObject.FindProperty("playerCardsPlayedByType");
        enemyCardsPlayedByType = serializedObject.FindProperty("enemyCardsPlayedByType");
        effectsByType = serializedObject.FindProperty("effectsByType");
        cardHistory = serializedObject.FindProperty("cardHistory");
        attackHistory = serializedObject.FindProperty("attackHistory");
        ongoingEffectHistory = serializedObject.FindProperty("ongoingEffectHistory");
        effectApplicationHistory = serializedObject.FindProperty("effectApplicationHistory");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (keepHistoryBetweenGames == null || maxHistorySize == null)
        {
            EditorGUILayout.HelpBox("Some properties could not be found. The script might need to be recompiled.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Card History Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(keepHistoryBetweenGames);
        EditorGUILayout.PropertyField(maxHistorySize);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Card Statistics", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);

        // Total cards section
        if (totalCardsPlayed != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Cards Played", GUILayout.Width(150));
            EditorGUILayout.LabelField(totalCardsPlayed.intValue.ToString());
            EditorGUILayout.EndHorizontal();
        }

        // Player cards
        if (playerCardsPlayed != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Player Cards Played", GUILayout.Width(150));
            EditorGUILayout.LabelField(playerCardsPlayed.intValue.ToString());
            EditorGUILayout.EndHorizontal();
        }

        // Enemy cards
        if (enemyCardsPlayed != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enemy Cards Played", GUILayout.Width(150));
            EditorGUILayout.LabelField(enemyCardsPlayed.intValue.ToString());
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attack Statistics", EditorStyles.boldLabel);

        // Total attacks
        if (totalAttacks != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Attacks", GUILayout.Width(150));
            EditorGUILayout.LabelField(totalAttacks.intValue.ToString());
            EditorGUILayout.EndHorizontal();
        }

        // Player attacks
        if (playerAttacks != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Player Attacks", GUILayout.Width(150));
            EditorGUILayout.LabelField(playerAttacks.intValue.ToString());
            EditorGUILayout.EndHorizontal();
        }

        // Enemy attacks
        if (enemyAttacks != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enemy Attacks", GUILayout.Width(150));
            EditorGUILayout.LabelField(enemyAttacks.intValue.ToString());
            EditorGUILayout.EndHorizontal();
        }

        // Ongoing effect statistics
        if (totalOngoingEffects != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ongoing Effect Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Ongoing Effects", GUILayout.Width(150));
            EditorGUILayout.LabelField(totalOngoingEffects.intValue.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Player Ongoing Effects", GUILayout.Width(150));
            EditorGUILayout.LabelField(playerOngoingEffects.intValue.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enemy Ongoing Effects", GUILayout.Width(150));
            EditorGUILayout.LabelField(enemyOngoingEffects.intValue.ToString());
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        // More detailed statistics in foldouts
        if (cardsPerTurn != null) EditorGUILayout.PropertyField(cardsPerTurn);
        if (cardsPlayedByType != null)
        {
            EditorGUILayout.PropertyField(cardsPlayedByType, new GUIContent("Cards By Type (All)"));
        }
        if (playerCardsPlayedByType != null)
        {
            EditorGUILayout.PropertyField(playerCardsPlayedByType, new GUIContent("Cards By Type (Player)"));
        }
        if (enemyCardsPlayedByType != null)
        {
            EditorGUILayout.PropertyField(enemyCardsPlayedByType, new GUIContent("Cards By Type (Enemy)"));
        }

        if (effectsByType != null)
        {
            EditorGUILayout.PropertyField(effectsByType, new GUIContent("Effects By Type"));
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("History Records", EditorStyles.boldLabel);

        // Card play history
        if (cardHistory != null)
        {
            showCardHistory = EditorGUILayout.Foldout(showCardHistory, $"Card Play History ({cardHistory.arraySize} records)", true);
            if (showCardHistory)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(cardHistory, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }
        }

        // Attack history
        if (attackHistory != null)
        {
            showAttackHistory = EditorGUILayout.Foldout(showAttackHistory, $"Attack History ({attackHistory.arraySize} records)", true);
            if (showAttackHistory)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(attackHistory, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }
        }

        // Ongoing effect history
        if (ongoingEffectHistory != null)
        {
            showOngoingEffectHistory = EditorGUILayout.Foldout(showOngoingEffectHistory, $"Ongoing Effect History ({ongoingEffectHistory.arraySize} records)", true);
            if (showOngoingEffectHistory)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(ongoingEffectHistory, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }
        }

        // Effect application history
        if (effectApplicationHistory != null)
        {
            showEffectApplicationHistory = EditorGUILayout.Foldout(showEffectApplicationHistory, $"Effect Application History ({effectApplicationHistory.arraySize} records)", true);
            if (showEffectApplicationHistory)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(effectApplicationHistory, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear History"))
        {
            var cardHistoryComponent = (CardHistory)target;
            if (cardHistoryComponent != null)
            {
                cardHistoryComponent.ClearHistory();
                EditorUtility.SetDirty(target);
            }
        }

        if (GUILayout.Button("Log All History"))
        {
            var cardHistoryComponent = (CardHistory)target;
            if (cardHistoryComponent != null)
            {
                // Use reflection to call the private method
                var method = typeof(CardHistory).GetMethod("LogFullHistory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(cardHistoryComponent, null);
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
