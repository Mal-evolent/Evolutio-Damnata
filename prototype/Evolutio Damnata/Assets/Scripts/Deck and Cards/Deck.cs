using System.Collections.Generic;
using UnityEngine;


public class Deck : MonoBehaviour
{
    public List<Card> Cards = new List<Card>();
    public List<Card> Hand = new List<Card>();
    public int MaxDeckSize;
    public int HandSize;

    [SerializeField] private CardLibrary _cardLibrary;
    public CardManager cardManager;

    public ICardLibrary CardLibrary
    {
        get => _cardLibrary;
        set => _cardLibrary = value as CardLibrary;
    }

    public void PopulateDeck()
    {
        Cards.Clear();
        Hand.Clear();

        if (_cardLibrary == null)
        {
            Debug.LogError("CardLibrary reference is not set!");
            return;
        }

        List<Card> newDeck = _cardLibrary.CreateDeckFromLibrary();
        foreach (Card card in newDeck)
        {
            AddCard(card);
        }

        Debug.Log("Deck Populated");
        Shuffle();
        DrawCard();
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
            if (Cards.Count == 0)
            {
                Debug.LogWarning("Deck is empty. Cannot draw more cards.");
                return;
            }

            Card drawnCard = Cards[0];
            Cards.RemoveAt(0);
            Hand.Add(drawnCard);
            Debug.Log("Drew Card: " + drawnCard.CardName);
            cardManager.RefreshUI();
        }

        if (Hand.Count == HandSize)
        {
            Debug.Log("Hand is full. Cannot draw more cards.");
        }
    }

    public void DrawOneCard()
    {
        // Ensure the hand is filled up to the hand size limit
        if (Hand.Count <= HandSize)
        {
            if (Cards.Count == 0)
            {
                Debug.LogWarning("Deck is empty. Cannot draw more cards.");
                return;
            }
            if (Hand.Count >= HandSize)
            {
                Debug.Log("Hand is full. Cannot draw more cards.");
                return;
            }

            Card drawnCard = Cards[0];
            Cards.RemoveAt(0);
            Hand.Add(drawnCard);
            Debug.Log("Drew Card: " + drawnCard.CardName);
            cardManager.RefreshUI();
        }
        else
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

    public bool TryRemoveCardAt(int index, out Card removedCard)
    {
        removedCard = null;

        if (index < 0 || index >= Hand.Count)
        {
            Debug.LogWarning($"Invalid card index {index} for removal");
            return false;
        }

        removedCard = Hand[index];
        Hand.RemoveAt(index);

        if (cardManager != null)
        {
            cardManager.RefreshUI();
        }

        return true;
    }

    public void RemoveCard(Card card)
    {
        int index = Hand.IndexOf(card);
        if (index >= 0)
        {
            TryRemoveCardAt(index, out _);
        }
        else
        {
            Debug.Log("Card not found in hand");
        }
    }

    public void Reset()
    {
        Cards.Clear();
        Hand.Clear();
        Debug.Log("Deck Reset");
        cardManager.RefreshUI();
    }
}
