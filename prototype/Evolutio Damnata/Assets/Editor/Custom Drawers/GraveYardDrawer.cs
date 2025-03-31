#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GraveYard.DeadEntityRecord))]
public class DeadEntityRecordDrawer : PropertyDrawer
{
    private static readonly Color friendlyColor = new Color(0.85f, 0.4f, 0.4f, 0.6f);
    private static readonly Color enemyColor = new Color(0.4f, 0.85f, 0.4f, 0.6f);
    private static readonly Color headerBgColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    private static readonly float padding = 5f;
    private static readonly float boxPadding = 15f;
    private static readonly float textLeftMargin = 20f;
    private static readonly float labelWidth = 100f;
    private static readonly float valueOffset = 110f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var entityName = property.FindPropertyRelative("entityName");
        var entityType = property.FindPropertyRelative("entityType");
        var killedByName = property.FindPropertyRelative("killedByName");
        var killedByType = property.FindPropertyRelative("killedByType");
        var timestamp = property.FindPropertyRelative("timestamp");
        var turnNumber = property.FindPropertyRelative("turnNumber");
        var finalDamage = property.FindPropertyRelative("finalDamage");
        var killSource = property.FindPropertyRelative("killSource");
        var spellName = property.FindPropertyRelative("spellName");

        if (entityName == null || entityType == null || turnNumber == null)
        {
            EditorGUI.LabelField(position, "Invalid Entity Record");
            EditorGUI.EndProperty();
            return;
        }

        string entityTypeStr = entityType?.enumDisplayNames[entityType.enumValueIndex] ?? "Unknown";
        string sourceInfo = "";
        
        switch (killSource.enumValueIndex)
        {
            case 0: // Combat
                sourceInfo = $"slain by {killedByName.stringValue}";
                break;
            case 1: // Spell
                sourceInfo = $"destroyed by spell: {spellName.stringValue}";
                break;
            case 2: // OngoingEffect
                sourceInfo = $"perished from {spellName.stringValue}";
                break;
            default:
                sourceInfo = "died from unknown causes";
                break;
        }

        var headerText = $"Turn {turnNumber.intValue}: {entityName.stringValue}";
        
        // Draw header background
        var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.DrawRect(headerRect, headerBgColor);

        // Draw type indicator bar
        var indicatorWidth = 3f;
        var indicatorRect = new Rect(position.x, position.y, indicatorWidth, 
            property.isExpanded ? EditorGUIUtility.singleLineHeight * 6 : EditorGUIUtility.singleLineHeight);
        EditorGUI.DrawRect(indicatorRect, entityTypeStr == "Friendly" ? friendlyColor : enemyColor);

        // Draw foldout and header with adjusted positioning
        var foldoutRect = new Rect(position.x + indicatorWidth + 8f, position.y,
            position.width - indicatorWidth - padding, EditorGUIUtility.singleLineHeight);
        
        var style = new GUIStyle(EditorStyles.foldout);
        style.normal.textColor = entityTypeStr == "Friendly" ? 
            new Color(1f, 0.7f, 0.7f) : new Color(0.7f, 1f, 0.7f);
        style.fontStyle = FontStyle.Bold;
        
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerText, true, style);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight;

            // Draw detail box background with adjusted positioning
            var boxRect = new Rect(position.x + boxPadding + 5f, y,
                position.width - (boxPadding * 2), 
                EditorGUIUtility.singleLineHeight * 5);
            EditorGUI.DrawRect(boxRect, new Color(0.2f, 0.2f, 0.2f, 0.1f));
            
            y += padding;

            var contentColor = GUI.contentColor;
            GUI.contentColor = entityTypeStr == "Friendly" ? 
                new Color(1f, 0.9f, 0.9f) : new Color(0.9f, 1f, 0.9f);

            DrawField($"• Entity:", $"{entityName.stringValue} ({entityTypeStr})", ref y, position.width);
            DrawField($"• Cause of Death:", sourceInfo, ref y, position.width);
            DrawField($"• Damage Taken:", $"{finalDamage.floatValue:F1}", ref y, position.width);
            DrawField($"• Turn & Time:", $"{turnNumber.intValue} at {timestamp.stringValue}", ref y, position.width);

            GUI.contentColor = contentColor;
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawField(string label, string value, ref float y, float totalWidth)
    {
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            fixedWidth = labelWidth
        };
        
        var valueStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        var rect = new Rect(textLeftMargin, y, totalWidth - (textLeftMargin * 2), EditorGUIUtility.singleLineHeight);
        
        // Draw label with fixed width
        EditorGUI.LabelField(rect, label, labelStyle);
        
        // Draw value with offset
        rect.x += valueOffset;
        rect.width = totalWidth - valueOffset - textLeftMargin;
        EditorGUI.LabelField(rect, value, valueStyle);
        
        y += EditorGUIUtility.singleLineHeight + 2f; // Added small extra spacing between fields
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight * 6;
        }
        return EditorGUIUtility.singleLineHeight;
    }
}

[CustomEditor(typeof(GraveYard))]
public class GraveYardEditor : Editor
{
    private SerializedProperty keepHistoryBetweenGames;
    private SerializedProperty maxHistorySize;
    private SerializedProperty graveyardHistory;
    private SerializedProperty totalEntitiesBuried;
    private SerializedProperty entitiesByType;
    private bool showFriendlyDeaths = true;
    private bool showEnemyDeaths = true;
    private Vector2 friendlyScrollPos;
    private Vector2 enemyScrollPos;

    private void OnEnable()
    {
        // Cache serialized properties
        keepHistoryBetweenGames = serializedObject.FindProperty("keepHistoryBetweenGames");
        maxHistorySize = serializedObject.FindProperty("maxHistorySize");
        graveyardHistory = serializedObject.FindProperty("graveyardHistory");
        totalEntitiesBuried = serializedObject.FindProperty("totalEntitiesBuried");
        entitiesByType = serializedObject.FindProperty("entitiesByType");
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
        DrawSettingsSection();
        EditorGUILayout.Space();
        DrawStatisticsSection();
        EditorGUILayout.Space();
        DrawHistorySection();

        if (GUILayout.Button("Clear Graveyard"))
        {
            var graveyardComponent = (GraveYard)target;
            if (graveyardComponent != null)
            {
                graveyardComponent.ClearGraveyard();
                EditorUtility.SetDirty(target);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSettingsSection()
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 13;
        headerStyle.margin = new RectOffset(5, 5, 5, 5);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("⚙️ Graveyard Settings", headerStyle);
        EditorGUILayout.Space(3);
        EditorGUILayout.PropertyField(keepHistoryBetweenGames);
        EditorGUILayout.PropertyField(maxHistorySize);
        EditorGUILayout.Space(3);
        EditorGUILayout.EndVertical();
    }

    private void DrawStatisticsSection()
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 13;
        headerStyle.margin = new RectOffset(5, 5, 5, 5);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("📊 Statistics", headerStyle);
        EditorGUILayout.Space(3);
        EditorGUI.BeginDisabledGroup(true);
        if (totalEntitiesBuried != null) EditorGUILayout.PropertyField(totalEntitiesBuried);
        if (entitiesByType != null) EditorGUILayout.PropertyField(entitiesByType);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space(3);
        EditorGUILayout.EndVertical();
    }

    private void DrawHistorySection()
    {
        if (graveyardHistory == null || graveyardHistory.arraySize == 0)
        {
            EditorGUILayout.HelpBox("The graveyard is empty... for now.", MessageType.Info);
            return;
        }

        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 13;
        headerStyle.margin = new RectOffset(5, 5, 5, 5);

        // Friendly Deaths Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(3);
        showFriendlyDeaths = EditorGUILayout.Foldout(showFriendlyDeaths, "💀 Friendly Deaths", true);
        if (showFriendlyDeaths)
        {
            friendlyScrollPos = EditorGUILayout.BeginScrollView(friendlyScrollPos, GUILayout.Height(200));
            DrawDeathRecords(true);
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Enemy Deaths Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(3);
        showEnemyDeaths = EditorGUILayout.Foldout(showEnemyDeaths, "☠️ Enemy Deaths", true);
        if (showEnemyDeaths)
        {
            enemyScrollPos = EditorGUILayout.BeginScrollView(enemyScrollPos, GUILayout.Height(200));
            DrawDeathRecords(false);
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawDeathRecords(bool friendly)
    {
        for (int i = 0; i < graveyardHistory.arraySize; i++)
        {
            var record = graveyardHistory.GetArrayElementAtIndex(i);
            var entityType = record.FindPropertyRelative("entityType");
            
            bool isFriendly = entityType.enumValueIndex == (int)EntityManager.MonsterType.Friendly;
            if (isFriendly != friendly) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(record, GUIContent.none);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }
}
#endif 