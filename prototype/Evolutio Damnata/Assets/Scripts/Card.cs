using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public string CardName;
    public Sprite CardImage;
    public string Description;
    public int AttackPower;
    public int Health;
    public int ManaCost;

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
    public enum SpellEffect
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        doubleAttack,
        burn
    }

    public SpellEffect EffectType;
    public int EffectValue;
    public int Duration; // Duration for effects like burn

    public override void Play()
    {
        Debug.Log("Playing Spell Card: " + CardName + "\n" + "Effect: " + EffectType + "\n" + "Value: " + EffectValue);
        ApplyEffect();
    }

    private void ApplyEffect()
    {
        switch (EffectType)
        {
            case SpellEffect.Damage:
                // Apply damage logic here
                Debug.Log("Applying " + EffectValue + " damage.");
                break;
            case SpellEffect.Heal:
                // Apply heal logic here
                Debug.Log("Healing for " + EffectValue + " health.");
                break;
            case SpellEffect.Buff:
                // Apply buff logic here
                Debug.Log("Applying buff with value " + EffectValue + ".");
                break;
            case SpellEffect.Debuff:
                // Apply debuff logic here
                Debug.Log("Applying debuff with value " + EffectValue + ".");
                break;
            case SpellEffect.doubleAttack:
                // Apply double attack logic here
                Debug.Log("Applying double attack effect.");
                ApplyDoubleAttackEffect();
                break;
            case SpellEffect.burn:
                // Apply burn logic here
                Debug.Log("Applying burn effect.");
                ApplyBurnEffect();
                break;
        }
    }

    private void ApplyBurnEffect()
    {
        // Logic to apply burn effect (damage over time)
        Debug.Log("Burn effect applied: " + EffectValue + " damage over " + Duration + " turns.");
    }

    private void ApplyDoubleAttackEffect()
    {
        // Logic to allow a monster to attack twice
        Debug.Log("Double attack effect applied for " + Duration + " turns.");
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
