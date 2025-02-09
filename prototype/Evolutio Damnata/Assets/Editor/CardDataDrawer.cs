using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

[CustomPropertyDrawer(typeof(CardData))]
public class CardDataDrawer : OdinValueDrawer<CardData>
{
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
        DrawChildProperty(property, "CardImage");
        DrawChildProperty(property, "Description");
        DrawChildProperty(property, "ManaCost");

        // Draw the IsSpellCard and IsMonsterCard toggles
        var isSpellCard = property.Children["IsSpellCard"];
        var isMonsterCard = property.Children["IsMonsterCard"];

        if (isSpellCard != null)
        {
            isSpellCard.Draw();
        }
        else
        {
            Debug.LogWarning($"Property 'IsSpellCard' not found on '{property.Path}'.");
        }

        if (isMonsterCard != null)
        {
            isMonsterCard.Draw();
        }
        else
        {
            Debug.LogWarning($"Property 'IsMonsterCard' not found on '{property.Path}'.");
        }

        // Draw the spell card fields if IsSpellCard is true
        if (isSpellCard != null && isSpellCard.ValueEntry.WeakSmartValue as bool? == true)
        {
            DrawChildProperty(property, "EffectType");
            DrawChildProperty(property, "EffectValue");
            DrawChildProperty(property, "Duration");
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
            Debug.Log($"Drawing property '{childName}' on '{property.Path}'.");
            childProperty.Draw();
        }
        else
        {
            Debug.LogWarning($"Property '{childName}' not found on '{property.Path}'.");
        }
    }
}
