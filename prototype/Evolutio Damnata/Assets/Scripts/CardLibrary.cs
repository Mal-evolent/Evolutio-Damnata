using JetBrains.Annotations;
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

    // Dictionary for fast lookups
    private Dictionary<string, Sprite> cardImageDictionary = new Dictionary<string, Sprite>();

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
        if (defaultCardSprite == null)
        {
            Debug.LogError("Default Card Sprite is not assigned! Please assign a sprite in the Inspector.");
            return;
        }

        // Example of adding cards
        cardDataList.Add(new CardData("Wizard", null, "A powerful wizard with fire spells", 5, 7, 10));
        cardDataList.Add(new CardData("Warrior", null, "A brave warrior", 3, 6, 8));
        cardDataList.Add(new CardData("Archer", null, "An expert archer", 2, 4, 6));
        cardDataList.Add(new CardData("Healer", null, "A healer with healing spells", 4, 3, 5));
        cardDataList.Add(new CardData("Rogue", null, "A sneaky rogue with stealth abilities", 3, 5, 7));
        cardDataList.Add(new CardData("Knight", null, "A noble knight with a strong shield", 4, 6, 9));
        cardDataList.Add(new CardData("Sorcerer", null, "A mysterious sorcerer with dark magic", 6, 8, 12));
        cardDataList.Add(new CardData("Berserker", null, "A raging berserker with unmatched strength", 5, 9, 8));
        cardDataList.Add(new CardData("Cleric", null, "A devoted cleric with divine powers", 4, 4, 6));
        cardDataList.Add(new CardData("Assassin", null, "A deadly assassin with lethal skills", 3, 7, 5));
        cardDataList.Add(new CardData("Druid", null, "A nature-loving druid with elemental magic", 4, 5, 7));
        cardDataList.Add(new CardData("Paladin", null, "A holy paladin with divine powers", 5, 6, 10));
        cardDataList.Add(new CardData("Barbarian", null, "A fierce barbarian with brute strength", 4, 8, 7));
        cardDataList.Add(new CardData("Necromancer", null, "A dark necromancer with undead minions", 6, 7, 8));
        cardDataList.Add(new CardData("Bard", null, "A musical bard with enchanting melodies", 3, 4, 5));
        cardDataList.Add(new CardData("Monk", null, "A disciplined monk with martial arts", 4, 5, 6));
        cardDataList.Add(new CardData("Alchemist", null, "A crafty alchemist with potion brewing", 3, 3, 4));
        cardDataList.Add(new CardData("Shaman", null, "A spiritual shaman with totemic magic", 4, 6, 7));
        cardDataList.Add(new CardData("Ranger", null, "A skilled ranger with tracking abilities", 3, 5, 6));
        cardDataList.Add(new CardData("Warlock", null, "A sinister warlock with dark pact", 5, 7, 9));
        cardDataList.Add(new CardData("Swashbuckler", null, "A daring swashbuckler with acrobatic skills", 4, 6, 8));
        cardDataList.Add(new CardData("Inquisitor", null, "A vigilant inquisitor with divine judgment", 5, 6, 7));
        cardDataList.Add(new CardData("Sorceress", null, "A powerful sorceress with arcane spells", 6, 8, 10));
        cardDataList.Add(new CardData("Beastmaster", null, "A wild beastmaster with animal companions", 4, 5, 6));
        cardDataList.Add(new CardData("Elementalist", null, "An elemental elementalist with elemental mastery", 5, 7, 8));
        cardDataList.Add(new CardData("Illusionist", null, "A tricky illusionist with deceptive magic", 3, 4, 5));
        cardDataList.Add(new CardData("Artificer", null, "A creative artificer with magical inventions", 4, 6, 7));
        cardDataList.Add(new CardData("Mystic", null, "A mysterious mystic with psychic powers", 5, 7, 9));


        // Iterate over cardDataList to validate each card and set default sprite if needed
        List <CardData> validCards = new List<CardData>();

        foreach (var cardData in cardDataList)
        {
            bool isValid = true;

            // Validate each field
            if (string.IsNullOrEmpty(cardData.CardName))
            {
                Debug.LogWarning("Card skipped due to missing name.");
                isValid = false;
            }
            if (string.IsNullOrEmpty(cardData.Description))
            {
                Debug.LogWarning($"Card {cardData.CardName} skipped due to missing description.");
                isValid = false;
            }
            if (cardData.ManaCost <= 0)
            {
                Debug.LogWarning($"Card {cardData.CardName} skipped due to invalid ManaCost.");
                isValid = false;
            }
            if (cardData.AttackPower <= 0)
            {
                Debug.LogWarning($"Card {cardData.CardName} skipped due to invalid AttackPower.");
                isValid = false;
            }
            if (cardData.Health <= 0)
            {
                Debug.LogWarning($"Card {cardData.CardName} skipped due to invalid Health.");
                isValid = false;
            }

            // Assign default sprite if the image is missing
            if (cardData.CardImage == null)
            {
                Debug.LogWarning($"No specific sprite for {cardData.CardName}. Using default sprite: {defaultCardSprite.name}");
                cardData.CardImage = defaultCardSprite;
            }

            // Add to validCards list only if all fields are valid
            if (isValid)
            {
                validCards.Add(cardData);
            }
        }

        // Replace cardDataList with the validated list
        cardDataList = validCards;

        Debug.Log($"Card Library initialized with {cardDataList.Count} valid cards.");

        // Populate the dictionary for fast lookups
        foreach (var cardData in cardDataList)
        {
            if (!cardImageDictionary.ContainsKey(cardData.CardName))
            {
                cardImageDictionary.Add(cardData.CardName, cardData.CardImage);
            }
        }

        // Populate the player's deck if it's assigned and empty
        if (playerDeck != null && playerDeck.Cards.Count == 0)
        {
            playerDeck.PopulateDeck();
        }
        else
        {
            Debug.LogWarning("Player Deck not assigned or already populated. Please assign a Player Deck.");
        }
    }

    // Method to get a card's image by name
    public Sprite cardImageGetter(string cardName)
    {
        if (cardImageDictionary.TryGetValue(cardName, out Sprite cardImage))
        {
            return cardImage;
        }
        else
        {
            Debug.LogWarning($"Card image for '{cardName}' not found in the library.");
            return defaultCardSprite;
        }
    }
}
