using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public string CardName;
    public Sprite CardImage;
    public string Description;
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
    public int AttackPower;
    public int Health;

    public override void Play()
    {
        Debug.Log("Playing Monster Card: " + CardName + "\n" + "Attack Power: " + AttackPower + "\n" + "Health: " + Health);
        // monster summoning logic here
    }
}

// Spell Card (Derived Class from Card)
public class SpellCard : Card
{
    public override void Play()
    {
        Debug.Log("Playing Spell Card: " + CardName);
        // spell logic here
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
