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

public class Deck : MonoBehaviour
{
    public List<Card> Cards = new List<Card>();
    public List<Card> Hand = new List<Card>();
    public int MaxDeckSize = 30;
    public int HandSize = 5;

    public void Shuffle()
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            Card temp = Cards[i];
            int randomIndex = Random.Range(i, Cards.Count);
            Cards[i] = Cards[randomIndex];
            Cards[randomIndex] = temp;
        }
        Debug.Log("Deck Shuffled");
    }

    public void DrawCard()
    {
        // Ensure the hand is filled up to the hand size limit
        while (Hand.Count < HandSize && Cards.Count > 0)
        {
            Card drawnCard = Cards[0];
            Cards.RemoveAt(0);
            Hand.Add(drawnCard);
            Debug.Log("Drew Card: " + drawnCard.CardName);
        }

        if (Hand.Count >= HandSize)
        {
            Debug.Log("Hand is full. Cannot draw more cards.");
        }
    }

    public void AddCard(Card card)
    {
        if (Cards.Count >= MaxDeckSize)
        {
            Debug.Log("Deck is full");
            return;
        }

        Cards.Add(card);
        Debug.Log("Added Card: " + card.CardName);
    }

    public void RemoveCard(Card card)
    {
        if (Cards.Remove(card))
        {
            Debug.Log("Removed Card: " + card.CardName);
        }
        else
        {
            Debug.Log("Card not found in deck");
        }
    }

    public void Reset()
    {
        Cards.Clear();
        Hand.Clear();
        Debug.Log("Deck Reset");
    }
}

// Card Manager for handling UI (requires Odin Inspector if used)
public class CardManager : MonoBehaviour
{
    public Deck playerDeck;
    public Transform cardUIContainer;

    // Displays deck in the UI
    public void DisplayDeck()
    {
        // Loop through deck and instantiate UI for each card
        foreach (Card card in playerDeck.Cards)
        {
            GameObject cardUI = new GameObject(card.CardName); // Placeholder for actual UI logic
            cardUI.transform.SetParent(cardUIContainer);
            Debug.Log($"Displayed {card.CardName} in the UI.");
        }
    }

    // Update UI elements (for use with Odin Inspector or similar)
    public void RefreshUI()
    {
        DisplayDeck(); // Reload and update UI display of cards
    }
}
