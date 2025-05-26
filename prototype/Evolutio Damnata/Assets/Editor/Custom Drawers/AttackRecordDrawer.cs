// Assets/Editor/Custom Drawers/AttackRecordDrawer.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using CardSystem.History;

[CustomPropertyDrawer(typeof(AttackRecord))]
public class AttackRecordDrawer : PropertyDrawer
{
    private static Texture2D damageBarTexture;
    private static GUIStyle targetStyle;
    private static GUIStyle damageStyle;
    private static GUIStyle counterDamageStyle;
    private static GUIStyle badgeStyle;
    private static GUIStyle timestampStyle;

    // Add dictionary to map keywords to colors
    private static readonly Dictionary<string, Color> keywordColors = new Dictionary<string, Color>
    {
        { "RANGED", new Color(0.2f, 0.6f, 0.2f) },
        { "TAUNT", new Color(0.7f, 0.5f, 0.1f) },
        { "TOUGH", new Color(0.3f, 0.3f, 0.7f) },
        { "OVERWHELM", new Color(0.7f, 0.2f, 0.5f) }
    };

    private void InitializeStyles()
    {
        if (targetStyle == null)
        {
            targetStyle = new GUIStyle(EditorStyles.label);
            targetStyle.normal.textColor = new Color(0.0f, 0.5f, 0.0f); // Green for targets
            targetStyle.fontStyle = FontStyle.Bold;
        }

        if (damageStyle == null)
        {
            damageStyle = new GUIStyle(EditorStyles.boldLabel);
            damageStyle.normal.textColor = new Color(0.8f, 0.2f, 0.2f); // Red for damage
        }

        if (counterDamageStyle == null)
        {
            counterDamageStyle = new GUIStyle(EditorStyles.label);
            counterDamageStyle.normal.textColor = new Color(0.6f, 0.4f, 0.1f); // Orange for counter damage
        }

        if (badgeStyle == null)
        {
            badgeStyle = new GUIStyle(EditorStyles.miniLabel);
            badgeStyle.normal.textColor = Color.white;
            badgeStyle.fontStyle = FontStyle.Bold;
            badgeStyle.alignment = TextAnchor.MiddleCenter;
            badgeStyle.padding = new RectOffset(4, 4, 2, 2);
        }

        if (timestampStyle == null)
        {
            timestampStyle = new GUIStyle(EditorStyles.label);
            timestampStyle.alignment = TextAnchor.MiddleLeft;
        }

        if (damageBarTexture == null)
        {
            damageBarTexture = new Texture2D(1, 1);
            damageBarTexture.SetPixel(0, 0, new Color(0.8f, 0.2f, 0.2f, 0.7f)); // Semi-transparent red
            damageBarTexture.Apply();
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        InitializeStyles();

        // Get properties
        var attackerName = property.FindPropertyRelative("attackerName");
        var targetName = property.FindPropertyRelative("targetName");
        var turnNumber = property.FindPropertyRelative("turnNumber");
        var damageDealt = property.FindPropertyRelative("damageDealt");
        var counterDamage = property.FindPropertyRelative("counterDamage");
        var timestamp = property.FindPropertyRelative("timestamp");
        var isEnemyAttack = property.FindPropertyRelative("isEnemyAttack");
        var wasRangedAttack = property.FindPropertyRelative("wasRangedAttack");
        var attackerKeywordsArray = property.FindPropertyRelative("attackerKeywords");

        if (attackerName == null || targetName == null)
        {
            EditorGUI.LabelField(position, "Invalid Attack Record");
            EditorGUI.EndProperty();
            return;
        }

        // Determine attacker type for display
        string attackerType = isEnemyAttack != null && isEnemyAttack.boolValue ? "Enemy" : "Player";
        bool isEnemyAttackValue = isEnemyAttack != null && isEnemyAttack.boolValue;
        bool isRangedAttack = wasRangedAttack != null && wasRangedAttack.boolValue;
        float damageValue = damageDealt?.floatValue ?? 0;
        float counterValue = counterDamage?.floatValue ?? 0;

        // Use custom colors for enemy vs player
        Color defaultColor = GUI.color;
        if (isEnemyAttackValue)
        {
            GUI.color = new Color(1f, 0.7f, 0.7f); // Light red for enemy
        }
        else
        {
            GUI.color = new Color(0.7f, 1f, 0.7f); // Light green for player
        }

        // Create a fancier header with attack direction and damage
        string rangedText = isRangedAttack ? "⟫⟫" : "→"; // Double arrow for ranged attacks
        string attackDirectionSymbol = isEnemyAttackValue ? "←" : "→"; // Shows logical direction
        var headerText = $"Turn {turnNumber?.intValue ?? 0}: {attackerType} {attackerName?.stringValue ?? "Unknown"} {rangedText} {targetName?.stringValue ?? "Unknown"}";

        // Draw the header
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerText);

        // Draw damage indicator at the end of the header
        var damageRect = new Rect(
            foldoutRect.xMax - 90,
            foldoutRect.y,
            80,
            foldoutRect.height);

        // Reset color for damage display
        GUI.color = defaultColor;

        // Display damage with color
        GUI.Label(damageRect, $"{damageValue:F1} damage", damageStyle);

        // Define badge spacing and positioning
        float badgeWidth = 80;
        float badgeSpacing = 5;

        // Collect active keywords from the attacker's saved keywords
        List<Keywords.MonsterKeyword> activeKeywords = new List<Keywords.MonsterKeyword>();

        // Read keywords from the serialized array
        if (attackerKeywordsArray != null)
        {
            for (int i = 0; i < attackerKeywordsArray.arraySize; i++)
            {
                var keywordProp = attackerKeywordsArray.GetArrayElementAtIndex(i);
                if (keywordProp != null)
                {
                    // The enum value is stored as an integer
                    Keywords.MonsterKeyword keyword = (Keywords.MonsterKeyword)keywordProp.enumValueIndex;
                    if (keyword != Keywords.MonsterKeyword.None)
                    {
                        activeKeywords.Add(keyword);
                    }
                }
            }
        }

        // Calculate total width needed for badges
        float totalWidth = activeKeywords.Count * (badgeWidth + badgeSpacing) - badgeSpacing;

        // Start position for the first badge, adjusting for the total width
        float badgesStartX = foldoutRect.xMax - 180 - totalWidth + badgeWidth;

        // Draw badges for all active keywords
        foreach (var keyword in activeKeywords)
        {
            if (keyword != Keywords.MonsterKeyword.None)
            {
                string keywordText = keyword.ToString().ToUpper();
                DrawKeywordBadge(keywordText, badgesStartX, foldoutRect.y - 1, badgeWidth, foldoutRect.height + 2, isEnemyAttackValue);
                badgesStartX += (badgeWidth + badgeSpacing);
            }
        }

        // Reset color
        GUI.color = defaultColor;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            var y = position.y + EditorGUIUtility.singleLineHeight + 4; // Add spacing
            float contentWidth = position.width - 60; // Adjust for indentation

            // Draw attacker with appropriate styling
            DrawField("Attacker:", attackerName?.stringValue ?? "Unknown", contentWidth, ref y);

            // Show keywords present on attacker
            if (activeKeywords.Count > 0)
            {
                List<string> keywordStrings = new List<string>();
                foreach (var keyword in activeKeywords)
                {
                    if (keyword != Keywords.MonsterKeyword.None)
                    {
                        keywordStrings.Add(keyword.ToString());
                    }
                }

                string keywordsText = string.Join(", ", keywordStrings);
                DrawField("Keywords:", keywordsText, contentWidth, ref y);
            }

            // Draw target with target highlighting style
            GUI.color = new Color(0.0f, 0.5f, 0.0f); // Green for target
            DrawField("Target:", targetName?.stringValue ?? "Unknown", contentWidth, ref y);
            GUI.color = defaultColor;

            // Draw attack type with combat role
            string attackTypeDesc = isRangedAttack ? "Ranged Attack" : "Melee Attack";
            string roleDesc = isEnemyAttackValue ? "(Enemy → Player)" : "(Player → Enemy)";
            DrawField("Attack Type:", $"{attackTypeDesc} {roleDesc}", contentWidth, ref y);

            // Draw fancy damage bar visualization
            y += 4; // Add spacing
            Rect damageBarHeader = new Rect(30, y, contentWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(damageBarHeader, "Damage:", EditorStyles.boldLabel);
            y += EditorGUIUtility.singleLineHeight;

            // Calculate max width for the damage bar (full width would be for 50+ damage)
            float maxBarWidth = contentWidth - 60;
            float barWidth = Mathf.Min(maxBarWidth, (damageValue / 50f) * maxBarWidth);

            // Draw damage value
            Rect damageValueRect = new Rect(40, y, 50, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(damageValueRect, $"{damageValue:F1}", damageStyle);

            // Draw damage bar
            Rect damageBarRect = new Rect(90, y + 3, barWidth, EditorGUIUtility.singleLineHeight - 6);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(damageBarRect, damageBarTexture);
            }
            y += EditorGUIUtility.singleLineHeight + 4;

            // Only show counter damage for melee attacks
            if (!isRangedAttack)
            {
                // Draw counter damage with a different color
                Rect counterHeader = new Rect(30, y, contentWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(counterHeader, "Counter Damage:", EditorStyles.boldLabel);
                y += EditorGUIUtility.singleLineHeight;

                // Draw counter damage value
                Rect counterValueRect = new Rect(40, y, 50, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(counterValueRect, $"{counterValue:F1}", counterDamageStyle);

                // Draw counter damage bar (orange)
                float counterBarWidth = Mathf.Min(maxBarWidth, (counterValue / 50f) * maxBarWidth);
                Rect counterBarRect = new Rect(90, y + 3, counterBarWidth, EditorGUIUtility.singleLineHeight - 6);
                if (Event.current.type == EventType.Repaint)
                {
                    Color prevColor = GUI.color;
                    GUI.color = new Color(0.9f, 0.6f, 0.2f, 0.7f); // Orange
                    GUI.DrawTexture(counterBarRect, EditorGUIUtility.whiteTexture);
                    GUI.color = prevColor;
                }
                y += EditorGUIUtility.singleLineHeight + 4;
            }
            else
            {
                // For ranged attacks, explain no counter damage
                DrawField("Counter Damage:", "None (Ranged Attack)", contentWidth, ref y);
            }

            // Draw turn and timestamp on separate lines with adequate spacing
            DrawField("Turn:", turnNumber?.intValue.ToString() ?? "0", contentWidth, ref y);

            // Add a little extra space before the timestamp to improve separation
            y += 2;

            // Draw timestamp with full width available to prevent clipping
            var timestampRect = new Rect(30, y, contentWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(timestampRect, $"Time: {timestamp?.stringValue ?? "Unknown"}", timestampStyle);
            y += EditorGUIUtility.singleLineHeight;

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawKeywordBadge(string keyword, float x, float y, float width, float height, bool isEnemyAttack)
    {
        var badgeRect = new Rect(x, y, width, height);

        // Get the color for this keyword, fallback to default if not found
        Color badgeColor;
        if (keywordColors.TryGetValue(keyword, out Color color))
        {
            badgeColor = isEnemyAttack ? color * 0.8f : color; // Slightly darken enemy badges
        }
        else
        {
            badgeColor = isEnemyAttack ? new Color(0.7f, 0.2f, 0.2f) : new Color(0.2f, 0.6f, 0.2f);
        }

        EditorGUI.DrawRect(badgeRect, badgeColor);
        GUI.Label(badgeRect, keyword, badgeStyle);
    }

    private void DrawField(string label, string value, float contentWidth, ref float y)
    {
        var rect = new Rect(30, y, contentWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rect, $"{label} {value}");
        y += EditorGUIUtility.singleLineHeight;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
        {
            var wasRangedAttack = property.FindPropertyRelative("wasRangedAttack");
            bool isRanged = wasRangedAttack != null && wasRangedAttack.boolValue;

            // Calculate the height based on content
            int baseFieldCount = 8; // Attacker, Keywords, Target, Attack Type, Damage visualization, Turn, Time
            if (!isRanged)
            {
                baseFieldCount += 2; // Add 2 more lines for counter damage
            }

            // Add additional padding to ensure no clipping
            return EditorGUIUtility.singleLineHeight * baseFieldCount + 50;
        }
        return EditorGUIUtility.singleLineHeight + 10;
    }

    // Clean up the texture when the editor is done with it
    ~AttackRecordDrawer()
    {
        if (damageBarTexture != null)
        {
            Object.DestroyImmediate(damageBarTexture);
            damageBarTexture = null;
        }
    }
}
#endif
