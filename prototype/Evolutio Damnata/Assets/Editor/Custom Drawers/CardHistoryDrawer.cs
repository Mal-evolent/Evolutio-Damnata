#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CardHistory.CardPlayRecord))]
public class CardPlayRecordDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var cardName = property.FindPropertyRelative("cardName");
        var cardDescription = property.FindPropertyRelative("cardDescription");
        var playerName = property.FindPropertyRelative("playerName");
        var turnNumber = property.FindPropertyRelative("turnNumber");
        var manaUsed = property.FindPropertyRelative("manaUsed");
        var timestamp = property.FindPropertyRelative("timestamp");
        var isEnemyCard = property.FindPropertyRelative("isEnemyCard");

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
            DrawField("Description:", cardDescription?.stringValue ?? "No description", ref y);
            DrawField("Player Type:", playerType, ref y);
            DrawField("Entity Name:", playerName?.stringValue ?? "Unknown", ref y);
            DrawField("Turn:", turnNumber?.intValue.ToString() ?? "0", ref y);
            DrawField("Mana Used:", manaUsed?.intValue.ToString() ?? "0", ref y);
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
            return EditorGUIUtility.singleLineHeight * 8; // Header + 7 fields (added player type)
        }
        return EditorGUIUtility.singleLineHeight;
    }
}

[CustomPropertyDrawer(typeof(CardHistory.AttackRecord))]
public class AttackRecordDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var attackerName = property.FindPropertyRelative("attackerName");
        var targetName = property.FindPropertyRelative("targetName");
        var turnNumber = property.FindPropertyRelative("turnNumber");
        var damageDealt = property.FindPropertyRelative("damageDealt");
        var counterDamage = property.FindPropertyRelative("counterDamage");
        var timestamp = property.FindPropertyRelative("timestamp");
        var isEnemyAttack = property.FindPropertyRelative("isEnemyAttack");
        var wasRangedAttack = property.FindPropertyRelative("wasRangedAttack");

        if (attackerName == null || targetName == null)
        {
            EditorGUI.LabelField(position, "Invalid Attack Record");
            EditorGUI.EndProperty();
            return;
        }

        // Determine attacker type for display
        string attackerType = isEnemyAttack != null && isEnemyAttack.boolValue ? "Enemy" : "Player";
        
        // Use custom colors for enemy vs player
        Color defaultColor = GUI.color;
        if (isEnemyAttack != null && isEnemyAttack.boolValue)
        {
            GUI.color = new Color(1f, 0.7f, 0.7f); // Light red for enemy
        }
        else
        {
            GUI.color = new Color(0.7f, 1f, 0.7f); // Light green for player
        }

        // Draw foldout header with safe string access
        string rangedText = wasRangedAttack != null && wasRangedAttack.boolValue ? " (Ranged)" : "";
        var headerText = $"Turn {turnNumber?.intValue ?? 0}: {attackerType} {attackerName?.stringValue ?? "Unknown"} → {targetName?.stringValue ?? "Unknown"} ({damageDealt?.floatValue ?? 0} dmg){rangedText}";
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerText);
        
        // Reset color
        GUI.color = defaultColor;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight;

            // Draw details with null checks
            DrawField("Attacker:", attackerName?.stringValue ?? "Unknown", ref y);
            DrawField("Target:", targetName?.stringValue ?? "Unknown", ref y);
            DrawField("Attacker Type:", attackerType, ref y);
            DrawField("Damage Dealt:", damageDealt?.floatValue.ToString("F1") ?? "0", ref y);
            
            string counterText = wasRangedAttack != null && wasRangedAttack.boolValue 
                ? "0 (Ranged Attack)" 
                : counterDamage?.floatValue.ToString("F1") ?? "0";
            DrawField("Counter Damage:", counterText, ref y);
            
            DrawField("Turn:", turnNumber?.intValue.ToString() ?? "0", ref y);
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
            return EditorGUIUtility.singleLineHeight * 8; // Header + 7 fields
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
    private SerializedProperty cardsPerTurn;
    private SerializedProperty cardsPlayedByType;
    private SerializedProperty playerCardsPlayedByType;
    private SerializedProperty enemyCardsPlayedByType;
    private SerializedProperty cardHistory;
    private SerializedProperty attackHistory;

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
        cardsPerTurn = serializedObject.FindProperty("cardsPerTurn");
        cardsPlayedByType = serializedObject.FindProperty("cardsPlayedByType");
        playerCardsPlayedByType = serializedObject.FindProperty("playerCardsPlayedByType");
        enemyCardsPlayedByType = serializedObject.FindProperty("enemyCardsPlayedByType");
        cardHistory = serializedObject.FindProperty("cardHistory");
        attackHistory = serializedObject.FindProperty("attackHistory");
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
        
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("History", EditorStyles.boldLabel);
        
        if (cardHistory != null)
        {
            EditorGUILayout.PropertyField(cardHistory, new GUIContent("Card Play History"));
        }
        
        if (attackHistory != null)
        {
            EditorGUILayout.PropertyField(attackHistory, new GUIContent("Attack History"));
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