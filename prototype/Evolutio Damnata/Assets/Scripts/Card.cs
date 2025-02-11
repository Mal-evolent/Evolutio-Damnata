using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

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
    [ShowIf(nameof(IsSpellCard))]
    public SpellEffect EffectType;

    [ShowIf(nameof(IsSpellCard))]
    public int EffectValue;

    [ShowIf(nameof(IsSpellCard))]
    public int Duration; // Duration for effects like burn

    [HideInInspector]
    public bool IsSpellCard = true;

    // Reference to the target entity
    public IDamageable targetEntity;

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
                ApplyDamageEffect();
                break;
            case SpellEffect.Heal:
                ApplyHealEffect();
                break;
            case SpellEffect.Buff:
                ApplyBuffEffect();
                break;
            case SpellEffect.Debuff:
                ApplyDebuffEffect();
                break;
            case SpellEffect.DoubleAttack:
                ApplyDoubleAttackEffect();
                break;
            case SpellEffect.Burn:
                ApplyBurnEffect();
                break;
        }
    }

    private void ApplyDamageEffect()
    {
        if (targetEntity != null)
        {
            targetEntity.takeDamage(EffectValue);
            Debug.Log("Applying " + EffectValue + " damage to target.");
        }
        else
        {
            Debug.LogError("No target entity set for damage effect.");
        }
    }

    private void ApplyHealEffect()
    {
        if (targetEntity != null)
        {
            targetEntity.heal(EffectValue);
            Debug.Log("Healing target for " + EffectValue + " health.");
        }
        else
        {
            Debug.LogError("No target entity set for heal effect.");
        }
    }

    private void ApplyBuffEffect()
    {
        // Apply buff logic here
        Debug.Log("Applying buff with value " + EffectValue + ".");
    }

    private void ApplyDebuffEffect()
    {
        // Apply debuff logic here
        Debug.Log("Applying debuff with value " + EffectValue + ".");
    }

    private void ApplyDoubleAttackEffect()
    {
        // Logic to allow a monster to attack twice
        Debug.Log("Double attack effect applied for " + Duration + " turns.");
    }

    private void ApplyBurnEffect()
    {
        // Logic to apply burn effect (damage over time)
        Debug.Log("Burn effect applied: " + EffectValue + " damage over " + Duration + " turns.");
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
