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
    private static readonly float textLeftMargin = 10f;
    private static readonly float valueOffset = 120f;
    private static readonly float labelWidth = 100f;

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

        string entityTypeStr = entityType.enumDisplayNames[entityType.enumValueIndex] ?? "Unknown";
        string sourceInfo = killSource.enumValueIndex switch
        {
            0 => $"slain by {killedByName.stringValue}",
            1 => $"destroyed by spell: {spellName.stringValue}",
            2 => $"perished from {spellName.stringValue}",
            _ => "died from unknown causes"
        };

        var headerText = $"Turn {turnNumber.intValue}: {entityName.stringValue}";
        var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight + 2);
        EditorGUI.DrawRect(headerRect, headerBgColor);

        var indicatorWidth = 3f;
        var indicatorRect = new Rect(position.x, position.y, indicatorWidth,
            property.isExpanded ? EditorGUIUtility.singleLineHeight * 6 : EditorGUIUtility.singleLineHeight);
        EditorGUI.DrawRect(indicatorRect, entityTypeStr == "Friendly" ? friendlyColor : enemyColor);

        var foldoutRect = new Rect(position.x + indicatorWidth + 8f, position.y,
            position.width - indicatorWidth - padding, EditorGUIUtility.singleLineHeight);

        var style = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
        style.normal.textColor = entityTypeStr == "Friendly" ? new Color(1f, 0.7f, 0.7f) : new Color(0.7f, 1f, 0.7f);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerText, true, style);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight;
            var boxRect = new Rect(position.x + boxPadding + 5f, y,
                position.width - (boxPadding * 2),
                EditorGUIUtility.singleLineHeight * 5);
            EditorGUI.DrawRect(boxRect, new Color(0.2f, 0.2f, 0.2f, 0.1f));
            y += padding;

            var contentColor = GUI.contentColor;
            GUI.contentColor = entityTypeStr == "Friendly" ? new Color(1f, 0.9f, 0.9f) : new Color(0.9f, 1f, 0.9f);

            DrawField("• Entity:", $"{entityName.stringValue} ({entityTypeStr})", ref y, position);
            DrawField("• Cause of Death:", sourceInfo, ref y, position);
            DrawField("• Damage Taken:", $"{finalDamage.floatValue:F1}", ref y, position);
            DrawField("• Turn & Time:", $"{turnNumber.intValue} at {timestamp.stringValue}", ref y, position);

            GUI.contentColor = contentColor;
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawField(string label, string value, ref float y, Rect position)
    {
        var labelStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, alignment = TextAnchor.MiddleLeft, fixedWidth = labelWidth };
        var valueStyle = new GUIStyle(EditorStyles.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };

        var rect = new Rect(position.x + textLeftMargin, y, position.width - textLeftMargin, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rect, label, labelStyle);

        rect.x += valueOffset;
        rect.width = position.width - valueOffset - textLeftMargin;
        EditorGUI.LabelField(rect, value, valueStyle);

        y += EditorGUIUtility.singleLineHeight + 4f;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        property.isExpanded ? EditorGUIUtility.singleLineHeight * 6 : EditorGUIUtility.singleLineHeight;
}
#endif
