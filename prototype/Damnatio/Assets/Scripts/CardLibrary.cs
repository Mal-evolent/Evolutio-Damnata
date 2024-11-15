using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardLibrary;

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

    public Deck playerDeck;

    // The list of all card data
    public List<CardData> cardDataList = new List<CardData>();

    [Header("Default Sprite for Cards")]
    public Sprite defaultCardSprite;


    // Use this method to create a card from the CardData
    public Card CreateCardFromData(CardData cardData)
    {
        MonsterCard newCard = new GameObject(cardData.CardName).AddComponent<MonsterCard>();
        newCard.CardName = cardData.CardName;
        newCard.CardImage = cardData.CardImage ?? defaultCardSprite;
        newCard.Description = cardData.Description;
        newCard.ManaCost = cardData.ManaCost;
        newCard.AttackPower = cardData.AttackPower;
        newCard.Health = cardData.Health;

        return newCard;
    }

    public List<Card> CreateDeckFromLibrary()
    {
        List<Card> deck = new List<Card>();
        foreach (var cardData in cardDataList)
        {
            Card newCard = CreateCardFromData(cardData);
            deck.Add(newCard);
        }
        return deck;
    }

    void Start()
    {
        //playerDeck = GetComponent<Deck>();

        if (defaultCardSprite == null)
        {
            Debug.LogError("Default Card Sprite is not assigned! Please assign a sprite in the Inspector.");
            return;
        }

        // Example of adding cards
        cardDataList.Add(new CardData("Wizard", null, "A powerful wizard with fire spells", 5, 7, 10)); // Uses default sprite
        cardDataList.Add(new CardData("Warrior", null, "A brave warrior", 3, 6, 8)); // Uses default sprite
        cardDataList.Add(new CardData("Archer", null, "An expert archer", 2, 4, 6)); // Uses default sprite

        // Iterate over cardDataList to check for missing sprites
        foreach (var cardData in cardDataList)
        {
            if (cardData.CardImage == null)
            {
                Debug.LogWarning($"No specific sprite for {cardData.CardName}. Using default sprite: {defaultCardSprite.name}");
                cardData.CardImage = defaultCardSprite;
            }
        }

        Debug.Log($"Card Library initialized with {cardDataList.Count} cards.");

        if (playerDeck != null && playerDeck.Cards.Count == 0)
        {
            playerDeck.PopulateDeck();
        }
        else
        {
            Debug.LogWarning("Player Deck not assigned or already populated. Please assign a Player Deck.");
        }
    }
}
