using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CardLibrary : MonoBehaviour
{
    [System.Serializable]
    public class CardData
    {
        public string CardName;
        public Sprite CardImage;
        public string Description;
        public int ManaCost;

        // AttackPower is now always visible
        [ShowIf(nameof(IsMonsterCard))]
        public int AttackPower;

        [ShowIf(nameof(IsMonsterCard))]
        public int Health;

        [ShowIf(nameof(IsMonsterCard))]
        public List<string> Keywords;

        [ShowIf(nameof(IsSpellCard))]
        public SpellEffect? EffectType;

        //[ShowIf(nameof(IsSpellCard))]
        public int? EffectValue;

        [ShowIf(nameof(IsSpellCard))]
        public int? Duration;

        //IsSpellCard is now public for debugging purposes
        public bool IsSpellCard;

        // IsMonsterCard is now a direct field instead of a computed property
        public bool IsMonsterCard;

        public CardData(
            string name, Sprite image, string description, int manaCost, int attackPower, int health,
            List<string> keywords = null, SpellEffect? effectType = null, int? effectValue = null, int? duration = null)
        {
            CardName = name;
            CardImage = image;
            Description = description;
            ManaCost = manaCost;
            AttackPower = attackPower;
            Health = health;
            Keywords = keywords ?? new List<string>();
            EffectType = effectType;
            EffectValue = effectValue;
            Duration = duration;

            // Explicitly setting IsSpellCard instead of relying on HasValue
            IsSpellCard = effectType != null;
            IsMonsterCard = !IsSpellCard; // Ensures IsMonsterCard is correctly assigned

            Debug.LogWarning($"CardData created: {CardName}, IsSpellCard: {IsSpellCard}, IsMonsterCard: {IsMonsterCard}, EffectType: {EffectType}");
        }
    }

    public Deck playerDeck;
    public Deck enemyDeck;

    // The list of all card data
    public List<CardData> cardDataList = new List<CardData>();

    [Header("Default Sprite for Cards")]
    public Sprite defaultCardSprite;

    // Dictionary for fast lookups
    private Dictionary<string, Sprite> cardImageDictionary = new Dictionary<string, Sprite>();

    // Use this method to create a card from the CardData
    public Card CreateCardFromData(CardData cardData)
    {
        Card newCard;
        if (cardData.IsSpellCard)
        {
            // Create a spell card
            SpellCard spellCard = new GameObject(cardData.CardName).AddComponent<SpellCard>();
            spellCard.CardName = cardData.CardName;
            spellCard.CardImage = cardData.CardImage ?? defaultCardSprite;
            spellCard.Description = cardData.Description;
            spellCard.ManaCost = cardData.ManaCost;
            spellCard.EffectType = cardData.EffectType.Value;
            spellCard.EffectValue = cardData.EffectValue ?? 0;
            spellCard.Duration = cardData.Duration ?? 0;
            newCard = spellCard;
        }
        else
        {
            // Create a monster card
            MonsterCard monsterCard = new GameObject(cardData.CardName).AddComponent<MonsterCard>();
            monsterCard.CardName = cardData.CardName;
            monsterCard.CardImage = cardData.CardImage ?? defaultCardSprite;
            monsterCard.Description = cardData.Description;
            monsterCard.ManaCost = cardData.ManaCost;
            monsterCard.AttackPower = cardData.AttackPower;
            monsterCard.Health = cardData.Health;
            monsterCard.Keywords = cardData.Keywords ?? new List<string>();
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

        // Example of adding cards with and without keywords
        cardDataList.Add(new CardData("Wizard", null, "A powerful wizard with fire spells", 5, 7, 10, new List<string> { "Spellcaster", "Fire" }));
        cardDataList.Add(new CardData("Warrior", null, "A brave warrior", 3, 6, 8, new List<string> { "Taunt" }));
        cardDataList.Add(new CardData("Archer", null, "An expert archer", 2, 4, 6, new List<string> { "Ranged" }));

        // Example of adding spell cards
        cardDataList.Add(new CardData("Fireball", null, "Deals damage to a single target", 4, 0, 0, null, SpellEffect.Damage, 10));
        cardDataList.Add(new CardData("Healing Light", null, "Heals a single target", 3, 0, 0, null, SpellEffect.Heal, 8));
        cardDataList.Add(new CardData("Burning Flames", null, "Applies burn effect to a single target", 5, 0, 0, null, SpellEffect.burn, 5, 3));
        cardDataList.Add(new CardData("Frenzy", null, "Allows a monster to attack twice", 6, 0, 0, null, SpellEffect.doubleAttack, 0, 2));

        // Iterate over cardDataList to validate each card and set default sprite if needed
        List<CardData> validCards = new List<CardData>();

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
