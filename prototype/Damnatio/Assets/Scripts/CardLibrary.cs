using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardLibrary : MonoBehaviour
{
    [System.Serializable]
    public class CardData
    {
        public string CardName;
        public Sprite CardImage;
        public string Description;
        public int ManaCost;
        public int AttackPower;
        public int Health;

        // Constructor to easily create a card from data
        public CardData(string name, Sprite image, string description, int manaCost, int attackPower, int health)
        {
            CardName = name;
            CardImage = image;
            Description = description;
            ManaCost = manaCost;
            AttackPower = attackPower;
            Health = health;
        }
    }

    // The list of all card data (exposed to Inspector via Odin)
    public List<CardData> cardDataList = new List<CardData>();

    // Use this method to create a card from the CardData
    public Card CreateCardFromData(CardData cardData)
    {
        // Instantiate a new Card object
        MonsterCard newCard = new GameObject(cardData.CardName).AddComponent<MonsterCard>();
        newCard.CardName = cardData.CardName;
        newCard.CardImage = cardData.CardImage;
        newCard.Description = cardData.Description;
        newCard.ManaCost = cardData.ManaCost;
        newCard.AttackPower = cardData.AttackPower;
        newCard.Health = cardData.Health;

        return newCard;
    }

    // You can call this method to create a deck from the card data
    public List<Card> CreateDeckFromLibrary()
    {
        List<Card> deck = new List<Card>();

        // Populate deck using the CardData
        foreach (var cardData in cardDataList)
        {
            Card newCard = CreateCardFromData(cardData);
            deck.Add(newCard);
        }

        return deck;
    }

    // Example of adding the wizard monster card to the card list at start
    void Start()
    {
        // Create a new wizard monster card data and add to list
        Sprite wizardSprite = null; // You will need to assign a sprite here in the Inspector
        CardData wizardCard = new CardData("Wizard", wizardSprite, "A powerful wizard with fire spells", 5, 7, 10);
        cardDataList.Add(wizardCard);
    }
}
