using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public List<Card> Cards = new List<Card>();
    public List<Card> Hand = new List<Card>();
    public int MaxDeckSize = 30;
    public int HandSize = 5;

    public CardLibrary cardLibrary;

    void Start()
    {

    }

    public void PopulateDeck()
    {
        Cards.Clear();
        Hand.Clear();

        List<Card> newDeck = cardLibrary.CreateDeckFromLibrary();
        foreach (Card card in newDeck)
        {
            AddCard(card);
        }

        Debug.Log("Deck Populated");
    }

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
