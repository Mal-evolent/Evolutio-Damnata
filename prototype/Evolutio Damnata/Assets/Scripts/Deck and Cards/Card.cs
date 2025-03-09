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
    [ShowIf(nameof(IsSpellCard))]
    public List<SpellEffect> EffectTypes = new List<SpellEffect>(); // Changed to list

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
        Debug.Log("Playing Spell Card: " + CardName + "\n" + "Effects: " + string.Join(", ", EffectTypes) + "\n" + "Value: " + EffectValue);
        ApplyEffects();
    }

    private void ApplyEffects()
    {
        foreach (var effect in EffectTypes)
        {
            switch (effect)
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
    }

    private void ApplyDamageEffect()
    {
        Debug.Log($"Applying Damage Effect for SpellCard: {name}");

        if (targetEntity != null)
        {
            targetEntity.takeDamage(EffectValue);
            Debug.Log($"Applying {EffectValue} damage to target for SpellCard: {name}");

            // Create and animate the damage number using EffectManager
            MonoBehaviour targetMonoBehaviour = targetEntity as MonoBehaviour;
            if (targetMonoBehaviour != null)
            {
                Vector3 targetPosition = targetMonoBehaviour.transform.position;
                Debug.Log($"Damage number created and animated for SpellCard: {name}");
            }
            else
            {
                Debug.LogError($"targetEntity is not a MonoBehaviour for SpellCard: {name}");
            }
        }
        else
        {
            Debug.LogError($"No target entity set for damage effect for SpellCard: {name}");
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
        if (targetEntity != null)
        {
            EntityManager entityManager = targetEntity as EntityManager;
            if (entityManager != null)
            {
                OngoingEffect burnEffect = new OngoingEffect(SpellEffect.Burn, EffectValue, Duration, entityManager);
                entityManager.AddNewOngoingEffect(burnEffect);
                Debug.Log($"Burn effect applied: {EffectValue} damage over {Duration} turns to {entityManager.name}");
            }
            else
            {
                Debug.LogError("Target entity is not an EntityManager.");
            }
        }
        else
        {
            Debug.LogError("No target entity set for burn effect.");
        }
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
