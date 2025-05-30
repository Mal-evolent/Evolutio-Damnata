using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardLibrary : MonoBehaviour, ICardLibrary
{
    private static CardLibrary _instance;
    public static CardLibrary Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CardLibrary>();
                if (_instance == null)
                {
                    Debug.LogError("No CardLibrary found in scene!");
                }
            }
            return _instance;
        }
    }

    [Header("Player Deck")]
    [SerializeField] private Deck _playerDeck;

    [Header("Enemy Deck")]
    [SerializeField] private Deck _enemyDeck;

    [Header("Card Data")]
    [SerializeField] private List<CardData> _cardDataList = new List<CardData>();

    [Header("Default Sprite")]
    [SerializeField] private Sprite _defaultCardSprite;
    public static Sprite DefaultSprite => Instance?._defaultCardSprite;

    private Dictionary<string, Sprite> _cardImageDictionary = new Dictionary<string, Sprite>();

    // ICardLibrary implementation
    public Sprite DefaultCardSprite => _defaultCardSprite;
    public Deck PlayerDeck { get => _playerDeck; set => _playerDeck = value; }
    public Deck EnemyDeck { get => _enemyDeck; set => _enemyDeck = value; }
    public IReadOnlyList<CardData> CardDataList => _cardDataList.AsReadOnly();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        if (_defaultCardSprite == null)
        {
            Debug.LogError("Default Card Sprite is not assigned!");
            return;
        }
        
        Debug.Log($"CardLibrary Awake - Default Sprite: {(_defaultCardSprite != null ? "Assigned" : "NULL")}");
    }

    private void Start()
    {
        Debug.Log($"CardLibrary Start - Default Sprite: {(_defaultCardSprite != null ? "Assigned" : "NULL")}");
        InitializeLibrary();
    }

    private void InitializeLibrary()
    {
        Debug.Log("Initializing Card Library...");
        
        // First validate and build the image dictionary
        ValidateCards();
        BuildImageDictionary();
        
        // Then initialize the decks
        InitializePlayerDeck();
        InitializeEnemyDeck();
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
        _cardImageDictionary.Clear();
        Debug.Log($"Building image dictionary with {_cardDataList.Count} cards");
        
        foreach (var cardData in _cardDataList)
        {
            if (cardData.CardImage == null)
            {
                Debug.Log($"Card {cardData.CardName} has no sprite, using default");
                cardData.CardImage = _defaultCardSprite;
            }
            
            if (!_cardImageDictionary.ContainsKey(cardData.CardName))
            {
                _cardImageDictionary.Add(cardData.CardName, cardData.CardImage);
            }
        }
        Debug.Log($"Image dictionary built with {_cardImageDictionary.Count} entries");
    }

    private void InitializePlayerDeck()
    {
        if (_playerDeck != null)
        {
            _playerDeck.CardLibrary = this;
            _playerDeck.PopulateDeck();
        }
    }

    private void InitializeEnemyDeck()
    {
        if (_enemyDeck != null)
        {
            _enemyDeck.CardLibrary = this;
            _enemyDeck.PopulateDeck();
            Debug.Log("Enemy deck initialized with library cards");
        }
        else
        {
            Debug.LogWarning("No enemy deck assigned in CardLibrary");
        }
    }

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
            cardData.EffectValuePerRound,
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
            cardData.Keywords ?? new List<Keywords.MonsterKeyword>(),
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
        if (string.IsNullOrEmpty(cardName))
        {
            Debug.LogWarning("Card name is null or empty");
            return _defaultCardSprite;
        }

        if (_cardImageDictionary.TryGetValue(cardName, out Sprite cardImage))
        {
            Debug.Log($"Found image for {cardName} - Sprite: {(cardImage != null ? "Valid" : "NULL")}");
            return cardImage ?? _defaultCardSprite;
        }

        Debug.LogWarning($"Card image for '{cardName}' not found in the library. Default Sprite: {(_defaultCardSprite != null ? "Valid" : "NULL")}");
        return _defaultCardSprite;
    }
}
