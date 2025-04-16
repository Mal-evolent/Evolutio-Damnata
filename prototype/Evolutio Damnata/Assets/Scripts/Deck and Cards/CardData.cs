using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class CardData
{
    [SerializeField]
    public string CardName;
    [SerializeField]
    private Sprite _cardImage;
    [ShowInInspector]
    public Sprite CardImage
    {
        get => _cardImage;
        set => _cardImage = value ?? CardLibrary.DefaultSprite;
    }
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
    public List<SpellEffect> EffectTypes = new List<SpellEffect>();

    [SerializeField, ShowIf(nameof(IsSpellCard))]
    public int EffectValue = 0;

    [SerializeField, ShowIf(nameof(IsSpellCard))]
    public int Duration = 0;

    [SerializeField, ShowIf(nameof(IsSpellCard))]
    public int EffectValuePerRound = 0;

    [SerializeField, ShowIf("HasDrawEffect")]
    public int DrawValue = 0;

    [SerializeField, ShowIf("HasBloodpriceEffect")]
    public int BloodpriceValue = 0;

    [SerializeField, OnValueChanged(nameof(OnIsSpellCardChanged))]
    public bool IsSpellCard;

    [SerializeField, OnValueChanged(nameof(OnIsMonsterCardChanged))]
    public bool IsMonsterCard;

    public CardData(
        string name, Sprite image, string description, int manaCost, int attackPower, int health,
        List<Keywords.MonsterKeyword> keywords = null, List<SpellEffect> effectTypes = null,
        int effectValue = 0, int duration = 0, int damagePerRound = 0, int drawValue = 0, int bloodpriceValue = 0)
    {
        CardName = name;
        CardImage = image;
        Description = description;
        ManaCost = manaCost;
        AttackPower = attackPower;
        Health = health;
        Keywords = keywords ?? new List<Keywords.MonsterKeyword>();
        EffectTypes = effectTypes ?? new List<SpellEffect>();
        EffectValue = effectValue;
        Duration = duration;
        EffectValuePerRound = damagePerRound;
        DrawValue = drawValue;
        BloodpriceValue = bloodpriceValue;

        IsSpellCard = EffectTypes.Count > 0;
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

    // This property is used by ShowIf attribute to determine whether to display DrawValue
    private bool HasDrawEffect()
    {
        return IsSpellCard && EffectTypes != null && EffectTypes.Contains(SpellEffect.Draw);
    }

    // This property is used by ShowIf attribute to determine whether to display BloodpriceValue
    private bool HasBloodpriceEffect()
    {
        return IsSpellCard && EffectTypes != null && EffectTypes.Contains(SpellEffect.Bloodprice);
    }
}
