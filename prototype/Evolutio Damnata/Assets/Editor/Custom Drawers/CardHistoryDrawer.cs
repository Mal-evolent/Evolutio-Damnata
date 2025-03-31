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

        if (cardName == null || playerName == null || turnNumber == null)
        {
            EditorGUI.LabelField(position, "Invalid Card Record");
            EditorGUI.EndProperty();
            return;
        }

        // Draw foldout header with safe string access
        var headerText = $"Turn {turnNumber.intValue}: {playerName.stringValue ?? "Unknown"} → {cardName.stringValue ?? "Unknown"}";
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerText);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight;

            // Draw details with null checks
            DrawField("Card:", cardName?.stringValue ?? "Unknown", ref y);
            DrawField("Description:", cardDescription?.stringValue ?? "No description", ref y);
            DrawField("Player:", playerName?.stringValue ?? "Unknown", ref y);
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
            return EditorGUIUtility.singleLineHeight * 7; // Header + 6 fields
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
    private SerializedProperty cardsPerTurn;
    private SerializedProperty cardsPlayedByType;
    private SerializedProperty cardHistory;

    private void OnEnable()
    {
        // Cache serialized properties
        keepHistoryBetweenGames = serializedObject.FindProperty("keepHistoryBetweenGames");
        maxHistorySize = serializedObject.FindProperty("maxHistorySize");
        totalCardsPlayed = serializedObject.FindProperty("totalCardsPlayed");
        cardsPerTurn = serializedObject.FindProperty("cardsPerTurn");
        cardsPlayedByType = serializedObject.FindProperty("cardsPlayedByType");
        cardHistory = serializedObject.FindProperty("cardHistory");
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
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        if (totalCardsPlayed != null) EditorGUILayout.PropertyField(totalCardsPlayed);
        if (cardsPerTurn != null) EditorGUILayout.PropertyField(cardsPerTurn);
        if (cardsPlayedByType != null) EditorGUILayout.PropertyField(cardsPlayedByType);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        if (cardHistory != null)
        {
            EditorGUILayout.PropertyField(cardHistory, GUIContent.none);
        }

        if (GUILayout.Button("Clear History"))
        {
            var cardHistoryComponent = (CardHistory)target;
            if (cardHistoryComponent != null)
            {
                cardHistoryComponent.ClearHistory();
                EditorUtility.SetDirty(target);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif 