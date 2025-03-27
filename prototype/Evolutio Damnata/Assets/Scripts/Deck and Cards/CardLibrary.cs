using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardLibrary : MonoBehaviour, ICardLibrary
{
    [Header("Player Deck")]
    [SerializeField] private Deck _playerDeck;

    [Header("Card Data")]
    [SerializeField] private List<CardData> _cardDataList = new List<CardData>();

    [Header("Default Sprite")]
    [SerializeField] private Sprite _defaultCardSprite;

    private Dictionary<string, Sprite> _cardImageDictionary = new Dictionary<string, Sprite>();

    // ICardLibrary implementation
    public Sprite DefaultCardSprite => _defaultCardSprite;
    public Deck PlayerDeck { get => _playerDeck; set => _playerDeck = value; }
    public IReadOnlyList<CardData> CardDataList => _cardDataList.AsReadOnly();

    public Card CreateCardFromData(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("CardData is null.");
            return null;
        }

        Card newCard = cardData.IsSpellCard ? CreateSpellCard(cardData) : CreateMonsterCard(cardData);
        newCard.name = cardData.CardName;
        Debug.Log($"Created {(cardData.IsSpellCard ? "Spell" : "Monster")}Card: {newCard.name}");

        return newCard;
    }

    private SpellCard CreateSpellCard(CardData cardData)
    {
        SpellCard spellCard = new GameObject(cardData.CardName).AddComponent<SpellCard>();
        if (spellCard == null)
        {
            Debug.LogError("Failed to create SpellCard component.");
            return null;
        }

        spellCard.Initialize(
            cardData.CardName,
            cardData.CardImage ?? _defaultCardSprite,
            cardData.Description,
            cardData.ManaCost,
            cardData.EffectTypes,
            cardData.EffectValue,
            cardData.Duration,
            cardData
        );

        return spellCard;
    }

    private MonsterCard CreateMonsterCard(CardData cardData)
    {
        MonsterCard monsterCard = new GameObject(cardData.CardName).AddComponent<MonsterCard>();
        if (monsterCard == null)
        {
            Debug.LogError("Failed to create MonsterCard component.");
            return null;
        }

        monsterCard.Initialize(
            cardData.CardName,
            cardData.CardImage ?? _defaultCardSprite,
            cardData.Description,
            cardData.ManaCost,
            cardData.AttackPower,
            cardData.Health,
            cardData.Keywords?.Select(k => k.ToString()).ToList() ?? new List<string>(),
            cardData
        );

        return monsterCard;
    }

    public List<Card> CreateDeckFromLibrary()
    {
        return _cardDataList.Select(cardData => CreateCardFromData(cardData)).ToList();
    }

    public Sprite GetCardImage(string cardName)
    {
        if (_cardImageDictionary.TryGetValue(cardName, out Sprite cardImage))
        {
            return cardImage;
        }

        Debug.LogWarning($"Card image for '{cardName}' not found in the library.");
        return _defaultCardSprite;
    }

    private void Start()
    {
        InitializeLibrary();
    }

    private void InitializeLibrary()
    {
        if (_defaultCardSprite == null)
        {
            Debug.LogError("Default Card Sprite is not assigned!");
            return;
        }

        AddDefaultCards();
        ValidateCards();
        BuildImageDictionary();
        InitializePlayerDeck();
    }

    private void AddDefaultCards()
    {
        _cardDataList.Add(new CardData("Wizard", null, "A powerful wizard with fire spells", 5, 7, 10,
            new List<Keywords.MonsterKeyword> { Keywords.MonsterKeyword.Spellcaster, Keywords.MonsterKeyword.Fire }));

        _cardDataList.Add(new CardData("Warrior", null, "A brave warrior", 3, 6, 8,
            new List<Keywords.MonsterKeyword> { Keywords.MonsterKeyword.Taunt }));

        // Add other default cards...
    }

    private void ValidateCards()
    {
        _cardDataList = _cardDataList.Where(cardData =>
        {
            if (string.IsNullOrEmpty(cardData.CardName))
            {
                Debug.LogWarning("Card skipped due to missing name.");
                return false;
            }

            // Add other validation checks...
            return true;
        }).ToList();

        Debug.Log($"Card Library initialized with {_cardDataList.Count} valid cards.");
    }

    private void BuildImageDictionary()
    {
        foreach (var cardData in _cardDataList)
        {
            if (!_cardImageDictionary.ContainsKey(cardData.CardName))
            {
                _cardImageDictionary.Add(cardData.CardName, cardData.CardImage ?? _defaultCardSprite);
            }
        }
    }

    private void InitializePlayerDeck()
    {
        if (_playerDeck != null)
        {
            _playerDeck.CardLibrary = this;
            _playerDeck.PopulateDeck();
        }
    }
}