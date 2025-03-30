#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(StackManager.TimedEffect))]
public class TimedEffectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var cardName = property.FindPropertyRelative("cardName");
        var targetName = property.FindPropertyRelative("_targetName");
        var effectType = property.FindPropertyRelative("_effectType");
        var turnsLeft = property.FindPropertyRelative("remainingTurns");

        // Draw foldout
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

        if (property.isExpanded)
        {
            // Draw compact summary
            var summaryRect = new Rect(
                position.x + 15,
                position.y + EditorGUIUtility.singleLineHeight,
                position.width - 15,
                EditorGUIUtility.singleLineHeight
            );

            string summary = $"{cardName.stringValue} → {targetName.stringValue} " +
                           $"({effectType.stringValue}, {turnsLeft.intValue} turns)";

            EditorGUI.LabelField(summaryRect, summary, EditorStyles.boldLabel);

            // Draw detailed view
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight * 2;

            DrawProperty("Card:", cardName, ref y, position.width);
            DrawProperty("Target:", targetName, ref y, position.width);
            DrawProperty("Effect:", effectType, ref y, position.width);
            DrawProperty("Turns Left:", turnsLeft, ref y, position.width, true);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawProperty(string label, SerializedProperty prop, ref float y, float width, bool isInt = false)
    {
        var rect = new Rect(30, y, width - 30, EditorGUIUtility.singleLineHeight);
        if (isInt)
        {
            EditorGUI.LabelField(rect, new GUIContent(label), new GUIContent(prop.intValue.ToString()));
        }
        else
        {
            EditorGUI.LabelField(rect, new GUIContent(label), new GUIContent(prop.stringValue));
        }
        y += EditorGUIUtility.singleLineHeight;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = property.isExpanded ? 6 : 1;
        return EditorGUIUtility.singleLineHeight * lineCount;
    }
}
#endif