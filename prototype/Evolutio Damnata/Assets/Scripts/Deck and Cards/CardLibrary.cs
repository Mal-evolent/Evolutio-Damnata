using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/* 
 * CardLibrary class is used to manage the card library in the game.
 * It contains a list of CardData objects that represent the cards in the game.
 * It also contains methods to create cards from the CardData objects and populate the player's deck.
 */

public class CardLibrary : MonoBehaviour
{
    public Deck playerDeck;
    public List<CardData> cardDataList = new List<CardData>();

    [Header("Default Sprite for Cards")]
    public Sprite defaultCardSprite;

    private Dictionary<string, Sprite> cardImageDictionary = new Dictionary<string, Sprite>();

    public Card CreateCardFromData(CardData cardData)
    {
        Card newCard;

        if (cardData.IsSpellCard)
        {
            SpellCard spellCard = new GameObject(cardData.CardName).AddComponent<SpellCard>();
            if (spellCard != null)
            {
                spellCard.CardName = cardData.CardName;
                spellCard.CardImage = cardData.CardImage ?? defaultCardSprite;
                spellCard.Description = cardData.Description;
                spellCard.ManaCost = cardData.ManaCost;
                spellCard.EffectTypes = cardData.EffectTypes;
                spellCard.EffectValue = cardData.EffectValue;
                spellCard.Duration = cardData.Duration;
                spellCard.CardType = cardData;

                // Add a readable unique identifier
                spellCard.name = $"{cardData.CardName}";
                Debug.Log($"Created SpellCard: {spellCard.name}");
            }
            else
            {
                Debug.LogError("Failed to create SpellCard component.");
            }

            newCard = spellCard;
        }
        else
        {
            MonsterCard monsterCard = new GameObject(cardData.CardName).AddComponent<MonsterCard>();
            if (monsterCard != null)
            {
                monsterCard.CardName = cardData.CardName;
                monsterCard.CardImage = cardData.CardImage ?? defaultCardSprite;
                monsterCard.Description = cardData.Description;
                monsterCard.ManaCost = cardData.ManaCost;
                monsterCard.AttackPower = cardData.AttackPower;
                monsterCard.Health = cardData.Health;
                monsterCard.Keywords = cardData.Keywords?.Select(k => k.ToString()).ToList() ?? new List<string>();
                monsterCard.CardType = cardData;

                // Add a readable unique identifier
                monsterCard.name = $"{cardData.CardName}";
                Debug.Log($"Created MonsterCard: {monsterCard.name}");
            }
            else
            {
                Debug.LogError("Failed to create MonsterCard component.");
            }

            newCard = monsterCard;
        }

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

        cardDataList.Add(new CardData("Wizard", null, "A powerful wizard with fire spells", 5, 7, 10, new List<Keywords.MonsterKeyword> { Keywords.MonsterKeyword.Spellcaster, Keywords.MonsterKeyword.Fire }));
        cardDataList.Add(new CardData("Warrior", null, "A brave warrior", 3, 6, 8, new List<Keywords.MonsterKeyword> { Keywords.MonsterKeyword.Taunt }));
        cardDataList.Add(new CardData("Archer", null, "An expert archer", 2, 4, 6, new List<Keywords.MonsterKeyword> { Keywords.MonsterKeyword.Ranged }));

        cardDataList.Add(new CardData("Fireball", null, "Deals damage to a single target", 4, 0, 0, null, new List<SpellEffect> { SpellEffect.Damage }, 2));
        cardDataList.Add(new CardData("Healing Light", null, "Heals a single target", 3, 0, 0, null, new List<SpellEffect> { SpellEffect.Heal }, 8));
        cardDataList.Add(new CardData("Burning Flames", null, "Applies burn effect to a single target", 5, 0, 0, null, new List<SpellEffect> { SpellEffect.Burn }, 5, 3));
        cardDataList.Add(new CardData("Frenzy", null, "Allows a monster to attack twice", 6, 0, 0, null, new List<SpellEffect> { SpellEffect.DoubleAttack }, 0, 2));

        List<CardData> validCards = new List<CardData>();

        foreach (var cardData in cardDataList)
        {
            bool isValid = true;

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
            if (cardData.AttackPower < 0)
            {
                Debug.LogWarning($"Card {cardData.CardName} skipped due to invalid AttackPower.");
                isValid = false;
            }
            if (cardData.Health < 0)
            {
                Debug.LogWarning($"Card {cardData.CardName} skipped due to invalid Health.");
                isValid = false;
            }

            if (cardData.CardImage == null)
            {
                Debug.LogWarning($"No specific sprite for {cardData.CardName}. Using default sprite: {defaultCardSprite.name}");
                cardData.CardImage = defaultCardSprite;
            }

            if (isValid)
            {
                validCards.Add(cardData);
            }
        }

        cardDataList = validCards;

        Debug.Log($"Card Library initialized with {cardDataList.Count} valid cards.");

        foreach (var cardData in cardDataList)
        {
            if (!cardImageDictionary.ContainsKey(cardData.CardName))
            {
                cardImageDictionary.Add(cardData.CardName, cardData.CardImage);
            }
        }

        // Populate the player's deck using the Deck class methods
        if (playerDeck != null)
        {
            playerDeck.cardLibrary = this;
            playerDeck.PopulateDeck();
        }
    }

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
