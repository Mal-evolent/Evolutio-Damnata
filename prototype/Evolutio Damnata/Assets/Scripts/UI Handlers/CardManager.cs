using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour, ICardManager
{
    [Header("References")]
    [SerializeField] private Deck _playerDeck;
    [SerializeField] private RectTransform _cardUIContainer;
    [SerializeField] private RectTransform _deckPanelRect;
    [SerializeField] private Sprite _cardTemplate;

    [Header("Prefab References")]
    [SerializeField] private GameObject _cardPrefab;
    private static GameObject _staticCardPrefab;

    [SerializeField] private CardOutlineManager _cardOutlineManager;
    [SerializeField] private CombatManager _combatManager;

    private List<GameObject> _deckCardObjects = new List<GameObject>();
    private List<GameObject> _handCardObjects = new List<GameObject>();

    [SerializeField] private GameObject _currentSelectedCard;
    public GameObject CurrentSelectedCard
    {
        get => _currentSelectedCard;
        set => _currentSelectedCard = value;
    }

    public Deck PlayerDeck => _playerDeck;
    public List<GameObject> DeckCardObjects => _deckCardObjects;
    public List<GameObject> HandCardObjects => _handCardObjects;

    private void Awake()
    {
        EnsurePrefabReference();
        ValidateReferences();
    }

    private void EnsurePrefabReference()
    {
        _cardPrefab = Resources.Load<GameObject>("Prefabs/Test Card");
        if (_cardPrefab != null)
        {
            _staticCardPrefab = _cardPrefab;
            return;
        }
    }

    private void ValidateReferences()
    {
        if (_cardUIContainer == null)
            Debug.LogError("Card UI Container not assigned in CardManager!");

        if (_deckPanelRect == null)
            Debug.LogError("Deck Panel Rect not assigned in CardManager!");

        if (_cardOutlineManager == null)
            Debug.LogWarning("CardOutlineManager not assigned - card highlighting won't work");
    }

    public void DisplayDeck()
    {
        ClearContainer(_deckPanelRect);
        _deckCardObjects.Clear();

        if (_playerDeck == null || _playerDeck.Cards == null)
        {
            Debug.LogWarning("Player deck or cards list is null");
            return;
        }

        for (int i = _playerDeck.Cards.Count - 1; i >= 0; i--)
        {
            Card card = _playerDeck.Cards[i];
            GameObject cardObject = CreateCardUI(card, _deckPanelRect);
            if (cardObject != null)
            {
                _deckCardObjects.Add(cardObject);
            }
        }
    }

    public void DisplayHand()
    {
        ClearContainer(_cardUIContainer);
        _handCardObjects.Clear();

        if (_playerDeck?.Hand == null)
        {
            Debug.LogWarning("Player deck or hand list is null");
            return;
        }

        foreach (Card card in _playerDeck.Hand)
        {
            GameObject cardObject = CreateCardUI(card, _cardUIContainer);
            if (cardObject != null)
            {
                SetupCardInteraction(cardObject);
                _handCardObjects.Add(cardObject);
            }
        }
    }

    private GameObject CreateCardUI(Card card, Transform parent)
    {
        if (_cardPrefab == null)
        {
            Debug.LogError("Cannot create card UI - prefab is missing");
            return null;
        }

        if (card == null)
        {
            Debug.LogWarning("Attempted to create UI for null card");
            return null;
        }

        Debug.Log($"Creating UI for card: {card.CardName}");
        GameObject cardObject = Instantiate(_cardPrefab, parent);
        cardObject.name = card.CardName;

        try
        {
            CardUI cardUI = cardObject.GetComponent<CardUI>() ?? cardObject.AddComponent<CardUI>();
            cardUI.Card = card;

            Transform cardTransform = cardObject.transform;
            Image cardImage = cardTransform.GetChild(0).GetComponent<Image>();
            Debug.Log($"Card {card.CardName} - Original Sprite: {(card.CardImage != null ? "Valid" : "NULL")}");
            cardImage.sprite = card.CardImage ?? CardLibrary.DefaultSprite;
            Debug.Log($"Card {card.CardName} - Final Sprite: {(cardImage.sprite != null ? "Valid" : "NULL")}");

            if (cardImage.sprite == null)
            {
                Debug.LogWarning($"No sprite found for card {card.CardName} and no default sprite available");
            }
            cardTransform.GetChild(1).GetComponent<TextMeshProUGUI>().text = card.CardName;
            cardTransform.GetChild(2).GetComponent<TextMeshProUGUI>().text = card.Description;

            // Generate attributes text based on card type with only non-zero values
            string attributes = GenerateCardAttributes(card);
            cardTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = attributes;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize card UI for {card.CardName}: {e.Message}");
            Destroy(cardObject);
            return null;
        }

        return cardObject;
    }

    private string GenerateCardAttributes(Card card)
    {
        var attributesList = new List<string>();

        // Show mana cost for all card types
        attributesList.Add($"Cost: {card.ManaCost}");

        // Monster card attributes
        if (card is MonsterCard monsterCard)
        {
            // Always show Health and Attack for monster cards
            attributesList.Add($"Health: {monsterCard.Health}");
            attributesList.Add($"Attack: {monsterCard.AttackPower}");

            // Only show keywords if present
            if (monsterCard.Keywords != null && monsterCard.Keywords.Count > 0)
            {
                attributesList.Add($"Keywords: {string.Join(", ", monsterCard.Keywords)}");
            }
        }
        // Spell card attributes
        else if (card is SpellCard spellCard)
        {
            // Get the CardData which contains all the effect information
            CardData cardData = card.CardType;

            if (cardData != null)
            {
                // Show effect types if present
                if (cardData.EffectTypes != null && cardData.EffectTypes.Count > 0)
                {
                    attributesList.Add($"Effect: {string.Join(", ", cardData.EffectTypes)}");
                }

                // Only show non-zero values
                if (cardData.EffectValue > 0)
                {
                    attributesList.Add($"Value: {cardData.EffectValue}");
                }

                if (cardData.Duration > 0)
                {
                    attributesList.Add($"Duration: {cardData.Duration}");
                }

                if (cardData.EffectValuePerRound > 0)
                {
                    attributesList.Add($"Damage/Round: {cardData.EffectValuePerRound}");
                }

                // Add Draw Value if the card has a Draw effect
                if (cardData.EffectTypes.Contains(SpellEffect.Draw) && cardData.DrawValue > 0)
                {
                    attributesList.Add($"Draw: {cardData.DrawValue}");
                }

                // Add Bloodprice Value if the card has a Bloodprice effect
                if (cardData.EffectTypes.Contains(SpellEffect.Bloodprice) && cardData.BloodpriceValue > 0)
                {
                    attributesList.Add($"Blood Price: {cardData.BloodpriceValue}");
                }
            }
            else
            {
                // Fallback to using SpellCard properties if CardData is not available
                if (spellCard.EffectTypes != null && spellCard.EffectTypes.Count > 0)
                {
                    attributesList.Add($"Effect: {string.Join(", ", spellCard.EffectTypes)}");
                }

                if (spellCard.EffectValue > 0)
                {
                    attributesList.Add($"Value: {spellCard.EffectValue}");
                }

                if (spellCard.Duration > 0)
                {
                    attributesList.Add($"Duration: {spellCard.Duration}");
                }

                if (spellCard.DamagePerRound > 0)
                {
                    attributesList.Add($"Damage/Round: {spellCard.DamagePerRound}");
                }
            }
        }

        return string.Join("\n", attributesList);
    }

    private void SetupCardInteraction(GameObject cardObject)
    {
        if (cardObject == null) return;

        Button cardButton = cardObject.GetComponent<Button>() ?? cardObject.AddComponent<Button>();
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => ToggleCardSelection(cardObject));
    }

    private void ToggleCardSelection(GameObject cardObject)
    {
        if (_cardOutlineManager == null)
        {
            Debug.LogError("CardOutlineManager is not assigned!");
            return;
        }

        if (cardObject == null) return;

        // If clicking the same card, toggle its selection off
        if (cardObject == _currentSelectedCard)
        {
            _cardOutlineManager.RemoveHighlight();
            _currentSelectedCard = null;
            return;
        }

        // Always deselect current selection first
        if (_currentSelectedCard != null)
        {
            _cardOutlineManager.RemoveHighlight();
            _currentSelectedCard = null;
        }

        // Remove any monster highlights before selecting a card
        var spritePositioning = FindObjectOfType<SpritePositioning>();
        if (spritePositioning != null && spritePositioning.PlayerEntities != null)
        {
            foreach (var entity in spritePositioning.PlayerEntities)
            {
                if (entity != null)
                {
                    var image = entity.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = Color.white;
                    }
                }
            }
        }

        // Now handle the new card selection
        _cardOutlineManager.HighlightCard(cardObject);
        CurrentSelectedCard = _cardOutlineManager.CardIsHighlighted ? cardObject : null;
        Debug.Log(CurrentSelectedCard != null
            ? $"Selected Card: {CurrentSelectedCard.name}"
            : "Deselected Card");
    }

    public void RemoveCard(GameObject cardObject)
    {
        if (cardObject == null) return;

        // Remove from hand objects list if present
        if (_handCardObjects.Contains(cardObject))
        {
            _handCardObjects.Remove(cardObject);
        }

        // Remove from deck hand if present
        if (cardObject.TryGetComponent<CardUI>(out var cardUI) && cardUI.Card != null)
        {
            _playerDeck.Hand.Remove(cardUI.Card);
        }

        Destroy(cardObject);
        RefreshUI();
    }

    public void RefreshUI()
    {
        ClearContainer(_cardUIContainer);
        ClearContainer(_deckPanelRect);
        _deckCardObjects.Clear();
        _handCardObjects.Clear();

        DisplayDeck();
        DisplayHand();
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        foreach (Transform t in container)
        {
            if (t != null && t.gameObject != null)
            {
                Destroy(t.gameObject);
            }
        }
    }
}
