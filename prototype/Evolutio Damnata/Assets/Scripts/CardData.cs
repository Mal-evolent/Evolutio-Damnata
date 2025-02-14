using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class CardData
{
    [SerializeField]
    public string CardName;
    [SerializeField]
    public Sprite CardImage;
    [SerializeField]
    public string Description;
    [SerializeField]
    public int ManaCost;

    [SerializeField, ShowIf(nameof(IsMonsterCard))]
    public int AttackPower;

    [SerializeField, ShowIf(nameof(IsMonsterCard))]
    public int Health;

    [SerializeField, ShowIf(nameof(IsMonsterCard))]
    public List<Keywords.MonsterKeyword> Keywords;

    [SerializeField, ShowIf(nameof(IsSpellCard))]
    public SpellEffect EffectType = SpellEffect.None; // Default value instead of nullable

    [SerializeField, ShowIf(nameof(IsSpellCard))]
    public int EffectValue = 0; // Use 0 instead of nullable int

    [SerializeField, ShowIf(nameof(IsSpellCard))]
    public int Duration = 0; // Use 0 instead of nullable int

    [SerializeField, OnValueChanged(nameof(OnIsSpellCardChanged))]
    public bool IsSpellCard;

    [SerializeField, OnValueChanged(nameof(OnIsMonsterCardChanged))]
    public bool IsMonsterCard;

    public CardData(
        string name, Sprite image, string description, int manaCost, int attackPower, int health,
        List<Keywords.MonsterKeyword> keywords = null, SpellEffect effectType = SpellEffect.None, int effectValue = 0, int duration = 0)
    {
        CardName = name;
        CardImage = image;
        Description = description;
        ManaCost = manaCost;
        AttackPower = attackPower;
        Health = health;
        Keywords = keywords ?? new List<Keywords.MonsterKeyword>();
        EffectType = effectType;
        EffectValue = effectValue;
        Duration = duration;

        IsSpellCard = effectType != SpellEffect.None; // Now properly detects spell cards
        IsMonsterCard = !IsSpellCard;
    }

    private void OnIsSpellCardChanged()
    {
        IsMonsterCard = !IsSpellCard;
    }

    private void OnIsMonsterCardChanged()
    {
        IsSpellCard = !IsMonsterCard;
    }
}
