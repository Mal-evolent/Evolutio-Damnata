using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/* 
 * Card class is the base class for all card types. It contains common properties and methods that are shared by all card types.
 * MonsterCard, SpellCard, and RitualCard are derived classes that inherit from the Card class.
 * MonsterCard and SpellCard override the Play method to provide specific implementations for playing the card.
 */

public class Card : MonoBehaviour
{
    public string CardName;
    public Sprite CardImage;
    public string Description;
    public int AttackPower;
    public int Health;
    public int ManaCost;
    public CardData CardType;

    // Base play method. Can be overridden by derived classes
    public virtual void Play()
    {
        Debug.Log("Playing Card: " + CardName);
    }

    public string GetCardDetails()
    {
        return "Card Name: " + CardName + "\n" + "Description: " + Description + "\n" + "Mana Cost: " + ManaCost;
    }
}

// Monster Card (Derived Class from Card)
public class MonsterCard : Card
{
    public List<string> Keywords { get; set; }

    public override void Play()
    {
        Debug.Log("Playing Monster Card: " + CardName + "\n" + "Attack Power: " + AttackPower + "\n" + "Health: " + Health);
        // monster summoning logic here
    }
}

// Spell Card (Derived Class from Card)
public class SpellCard : Card
{
    // Keep these data fields
    [ShowIf(nameof(IsSpellCard))]
    public List<SpellEffect> EffectTypes = new List<SpellEffect>();

    [ShowIf(nameof(IsSpellCard))]
    public int EffectValue;

    [ShowIf(nameof(IsSpellCard))]
    public int Duration;

    [HideInInspector]
    public bool IsSpellCard = true;

    public IDamageable targetEntity { get; set; }

    public override void Play()
    {
        Debug.Log($"Spell card {CardName} triggered");
        // Effect logic moved to SpellEffectApplier
    }
}

// Ritual Card (Derived Class from Card)
public class RitualCard : Card
{
    public override void Play()
    {
        Debug.Log("Playing Ritual Card: " + CardName);
        // ritual logic here
    }
}
