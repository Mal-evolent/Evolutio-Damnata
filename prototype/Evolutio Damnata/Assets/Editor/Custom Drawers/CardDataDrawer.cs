using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using System.Linq;

[CreateAssetMenu(fileName = "SpellCardCategorizer", menuName = "Card Game/Spell Card Categorizer")]
public class SpellCardCategorizer : ScriptableObject
{
    [SerializeField]
    private List<SpellCardCategory> categories = new List<SpellCardCategory>();

    public List<SpellCardCategory> Categories => categories;

    // Initialize with default categories
    private void OnEnable()
    {
        if (categories == null || categories.Count == 0)
        {
            categories = new List<SpellCardCategory>
                            {
                                new SpellCardCategory("Damage", new List<SpellEffect> { SpellEffect.Damage },
                                    new List<string> { "EffectValue" }),

                                new SpellCardCategory("Healing", new List<SpellEffect> { SpellEffect.Heal },
                                    new List<string> { "EffectValue" }),

                                new SpellCardCategory("Burn", new List<SpellEffect> { SpellEffect.Burn },
                                    new List<string> { "Duration", "EffectValue", "EffectValuePerRound" }),

                                new SpellCardCategory("Card Draw", new List<SpellEffect> { SpellEffect.Draw },
                                    new List<string> { "DrawValue" }),

                                new SpellCardCategory("Blood Price", new List<SpellEffect> { SpellEffect.Bloodprice },
                                    new List<string> { "BloodpriceValue" })
                            };
        }
    }

    // Get category by checking if card contains any of the effects for the category
    public SpellCardCategory GetCategoryForCard(CardData card)
    {
        if (!card.IsSpellCard || card.EffectTypes == null || card.EffectTypes.Count == 0)
            return null;

        foreach (var category in categories)
        {
            foreach (var effect in category.EffectTypes)
            {
                if (card.EffectTypes.Contains(effect))
                {
                    return category;
                }
            }
        }

        return null;
    }

    // Check if a card has a specific category
    public bool CardHasCategory(CardData card, string categoryName)
    {
        var category = categories.FirstOrDefault(c => c.Name == categoryName);
        if (category == null)
            return false;

        return card.EffectTypes.Any(e => category.EffectTypes.Contains(e));
    }

    // Get all categories that apply to a card
    public List<SpellCardCategory> GetCategoriesForCard(CardData card)
    {
        if (!card.IsSpellCard || card.EffectTypes == null || card.EffectTypes.Count == 0)
            return new List<SpellCardCategory>();

        return categories.Where(category =>
            card.EffectTypes.Any(effect => category.EffectTypes.Contains(effect))).ToList();
    }

    // Add a new category
    public void AddCategory(string name, List<SpellEffect> effectTypes, List<string> relevantProperties)
    {
        categories.Add(new SpellCardCategory(name, effectTypes, relevantProperties));
    }
}

// Class to define a spell card category
[System.Serializable]
public class SpellCardCategory
{
    [SerializeField]
    private string name;

    [SerializeField]
    private List<SpellEffect> effectTypes = new List<SpellEffect>();

    [SerializeField]
    private List<string> relevantProperties = new List<string>();

    public string Name => name;
    public List<SpellEffect> EffectTypes => effectTypes;
    public List<string> RelevantProperties => relevantProperties;

    public SpellCardCategory(string name, List<SpellEffect> effectTypes, List<string> relevantProperties)
    {
        this.name = name;
        this.effectTypes = effectTypes;
        this.relevantProperties = relevantProperties;
    }
}

// Custom drawer for displaying categorized spell card properties
[CustomPropertyDrawer(typeof(CardData))]
public class CategorizedCardDataDrawer : OdinValueDrawer<CardData>
{
    private InspectorProperty isSpellCard;
    private InspectorProperty isMonsterCard;
    private InspectorProperty effectTypes;
    private SpellCardCategorizer categorizer;

    protected override void Initialize()
    {
        base.Initialize();
        isSpellCard = this.Property.Children["IsSpellCard"];
        isMonsterCard = this.Property.Children["IsMonsterCard"];
        effectTypes = this.Property.Children["EffectTypes"];

        // Find or create the categorizer
        categorizer = AssetDatabase.FindAssets("t:SpellCardCategorizer")
            .Select(guid => AssetDatabase.LoadAssetAtPath<SpellCardCategorizer>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault();

        if (categorizer == null)
        {
            categorizer = ScriptableObject.CreateInstance<SpellCardCategorizer>();
            AssetDatabase.CreateAsset(categorizer, "Assets/Resources/SpellCardCategorizer.asset");
            AssetDatabase.SaveAssets();
        }
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        var property = this.Property;
        var cardData = property.ValueEntry.WeakSmartValue as CardData;

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

        // Draw the basic card fields
        DrawChildProperty(property, "CardName");
        DrawChildProperty(property, "CardImage");
        DrawChildProperty(property, "Description");
        DrawChildProperty(property, "ManaCost");

        // Draw the IsSpellCard and IsMonsterCard toggles
        isSpellCard?.Draw();
        isMonsterCard?.Draw();

        // Handle monster cards
        if (isMonsterCard != null && isMonsterCard.ValueEntry.WeakSmartValue as bool? == true)
        {
            DrawChildProperty(property, "AttackPower");
            DrawChildProperty(property, "Health");
            DrawChildProperty(property, "Keywords");
        }

        // Handle spell cards with categories
        if (isSpellCard != null && isSpellCard.ValueEntry.WeakSmartValue as bool? == true)
        {
            // Draw the effect types first
            effectTypes?.Draw();

            if (cardData != null && cardData.EffectTypes != null && cardData.EffectTypes.Count > 0)
            {
                var categories = categorizer.GetCategoriesForCard(cardData);

                // Draw properties for each category
                foreach (var category in categories)
                {
                    SirenixEditorGUI.BeginBox();
                    SirenixEditorGUI.BeginBoxHeader();
                    EditorGUILayout.LabelField($"{category.Name} Properties");
                    SirenixEditorGUI.EndBoxHeader();

                    // Draw relevant properties for this category
                    foreach (var propName in category.RelevantProperties)
                    {
                        DrawChildProperty(property, propName);
                    }

                    SirenixEditorGUI.EndBox();
                }
            }
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

// Editor window to manage spell card categories
public class SpellCardCategorizerWindow : EditorWindow
{
    private SpellCardCategorizer categorizer;
    private Vector2 scrollPosition;

    [MenuItem("Card Game/Spell Card Categorizer")]
    public static void ShowWindow()
    {
        GetWindow<SpellCardCategorizerWindow>("Spell Card Categorizer");
    }

    private void OnEnable()
    {
        categorizer = AssetDatabase.FindAssets("t:SpellCardCategorizer")
            .Select(guid => AssetDatabase.LoadAssetAtPath<SpellCardCategorizer>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault();

        if (categorizer == null)
        {
            categorizer = ScriptableObject.CreateInstance<SpellCardCategorizer>();
            AssetDatabase.CreateAsset(categorizer, "Assets/Resources/SpellCardCategorizer.asset");
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Spell Card Categories", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var category in categorizer.Categories)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Category: {category.Name}", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Effect Types:");
            foreach (var effectType in category.EffectTypes)
            {
                EditorGUILayout.LabelField($"• {effectType}");
            }

            EditorGUILayout.LabelField("Relevant Properties:");
            foreach (var property in category.RelevantProperties)
            {
                EditorGUILayout.LabelField($"• {property}");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Edit Categories"))
        {
            Selection.activeObject = categorizer;
        }
    }
}
