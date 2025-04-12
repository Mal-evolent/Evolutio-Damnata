using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

[CustomPropertyDrawer(typeof(CardData))]
public class CardDataDrawer : OdinValueDrawer<CardData>
{
    private InspectorProperty isSpellCard;
    private InspectorProperty isMonsterCard;
    private InspectorProperty duration;

    protected override void Initialize()
    {
        base.Initialize();
        isSpellCard = this.Property.Children["IsSpellCard"];
        isMonsterCard = this.Property.Children["IsMonsterCard"];
        duration = this.Property.Children["Duration"];
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        var property = this.Property;

        // Ensure the label is not null
        if (label == null)
        {
            label = new GUIContent("Card Data");
        }

        // Draw the default fields
        SirenixEditorGUI.BeginBox();
        SirenixEditorGUI.BeginBoxHeader();
        EditorGUILayout.LabelField(label);
        SirenixEditorGUI.EndBoxHeader();

        // Draw the fields
        DrawChildProperty(property, "CardName");
        var cardImageProperty = property.Children["CardImage"];
        if (cardImageProperty != null)
        {
            cardImageProperty.Draw();
        }
        DrawChildProperty(property, "Description");
        DrawChildProperty(property, "ManaCost");

        // Draw the IsSpellCard and IsMonsterCard toggles
        isSpellCard?.Draw();
        isMonsterCard?.Draw();

        // Draw the spell card fields if IsSpellCard is true
        if (isSpellCard != null && isSpellCard.ValueEntry.WeakSmartValue as bool? == true)
        {
            DrawChildProperty(property, "EffectTypes");
            DrawChildProperty(property, "EffectValue");
            DrawChildProperty(property, "Duration");

            // Only draw EffectValuePerRound if Duration is greater than 0
            int durationValue = duration?.ValueEntry.WeakSmartValue as int? ?? 0;
            if (durationValue > 0)
            {
                DrawChildProperty(property, "EffectValuePerRound");
            }
        }

        // Draw the monster card fields if IsMonsterCard is true
        if (isMonsterCard != null && isMonsterCard.ValueEntry.WeakSmartValue as bool? == true)
        {
            DrawChildProperty(property, "AttackPower");
            DrawChildProperty(property, "Health");
            DrawChildProperty(property, "Keywords");
        }

        SirenixEditorGUI.EndBox();
    }

    private void DrawChildProperty(InspectorProperty property, string childName)
    {
        var childProperty = property.Children[childName];
        if (childProperty != null)
        {
            childProperty.Draw();
        }
        else
        {
            Debug.LogWarning($"Property '{childName}' not found on '{property.Path}'.");
        }
    }
}
