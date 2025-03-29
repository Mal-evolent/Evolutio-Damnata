using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour, ICardManager
{
    [Header("Deck Type")]
    [SerializeField] private bool _isEnemy = false;

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
        // Automatically detect if this is the enemy deck manager
        if (gameObject.name == "Enemy Deck Manager")
        {
            _isEnemy = true;
            Debug.Log("Automatically detected Enemy Deck Manager - disabling UI");
        }

        EnsurePrefabReference();
        ValidateReferences();

        // Disable UI components if this is an enemy deck
        if (_isEnemy)
        {
            DisableUIComponents();
        }
    }

    private void DisableUIComponents()
    {
        if (_cardUIContainer != null) _cardUIContainer.gameObject.SetActive(false);
        if (_deckPanelRect != null) _deckPanelRect.gameObject.SetActive(false);
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
        if (!_isEnemy) // Only validate UI references for player deck
        {
            if (_cardUIContainer == null)
                Debug.LogError("Card UI Container not assigned in CardManager!");

            if (_deckPanelRect == null)
                Debug.LogError("Deck Panel Rect not assigned in CardManager!");
        }

        if (_cardOutlineManager == null)
            Debug.LogWarning("CardOutlineManager not assigned - card highlighting won't work");
    }

    public void DisplayDeck()
    {
        if (_isEnemy) return;

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
        if (_isEnemy) return;

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

        GameObject cardObject = Instantiate(_cardPrefab, parent);
        cardObject.name = card.CardName;

        try
        {
            CardUI cardUI = cardObject.GetComponent<CardUI>() ?? cardObject.AddComponent<CardUI>();
            cardUI.Card = card;

            if (!_isEnemy)
            {
                Transform cardTransform = cardObject.transform;
                cardTransform.GetChild(0).GetComponent<Image>().sprite = card.CardImage;
                cardTransform.GetChild(1).GetComponent<TextMeshProUGUI>().text = card.CardName;
                cardTransform.GetChild(2).GetComponent<TextMeshProUGUI>().text = card.Description;

                string attributes = card switch
                {
                    MonsterCard monster => $"Health: {monster.Health}\nAttack: {monster.AttackPower}\nCost: {monster.ManaCost}",
                    SpellCard spell => $"Effect: {string.Join(", ", spell.EffectTypes)}\nValue: {spell.EffectValue}\nDuration: {spell.Duration}\nCost: {spell.ManaCost}",
                    _ => string.Empty
                };
                cardTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = attributes;
            }
            else
            {
                // Disable all UI components for enemy cards
                foreach (Transform child in cardObject.transform)
                {
                    if (child.TryGetComponent<Graphic>(out var graphic))
                    {
                        graphic.enabled = false;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize card UI for {card.CardName}: {e.Message}");
            Destroy(cardObject);
            return null;
        }

        return cardObject;
    }

    private void SetupCardInteraction(GameObject cardObject)
    {
        if (_isEnemy || cardObject == null) return;

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

        _cardOutlineManager.HighlightCard(cardObject);
        CurrentSelectedCard = _cardOutlineManager.CardIsHighlighted ? cardObject : null;
        Debug.Log(CurrentSelectedCard != null
            ? $"Selected Card: {CurrentSelectedCard.name}"
            : "Deselected Card");
    }

    public void RemoveCard(GameObject cardObject)
    {
        if (cardObject == null) return;

        if (!_handCardObjects.Contains(cardObject))
        {
            Debug.LogWarning($"Card {cardObject.name} not found in hand");
            return;
        }

        _handCardObjects.Remove(cardObject);
        if (cardObject.TryGetComponent<CardUI>(out var cardUI) && cardUI.Card != null)
        {
            _playerDeck.Hand.Remove(cardUI.Card);
        }

        Destroy(cardObject);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_isEnemy) return;

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
