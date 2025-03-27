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
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private CardOutlineManager _cardOutlineManager;
    [SerializeField] private CombatManager _combatManager;

    private List<GameObject> _deckCardObjects = new List<GameObject>();
    private List<GameObject> _handCardObjects = new List<GameObject>();

    public GameObject CurrentSelectedCard { get; set; }
    public Deck PlayerDeck => _playerDeck;
    public List<GameObject> DeckCardObjects => _deckCardObjects;
    public List<GameObject> HandCardObjects => _handCardObjects;

    public void DisplayDeck()
    {
        foreach (Transform t in _deckPanelRect.transform)
            Destroy(t.gameObject);
        _deckCardObjects.Clear();

        for (int i = _playerDeck.Cards.Count - 1; i >= 0; i--)
        {
            Card card = _playerDeck.Cards[i];
            GameObject cardObject = CreateCardUI(card, _deckPanelRect);
            _deckCardObjects.Add(cardObject);
        }
    }

    public void DisplayHand()
    {
        foreach (Card card in _playerDeck.Hand)
        {
            GameObject cardObject = CreateCardUI(card, _cardUIContainer);
            SetupCardInteraction(cardObject);
            _handCardObjects.Add(cardObject);
        }
    }

    private GameObject CreateCardUI(Card card, Transform parent)
    {
        GameObject cardObject = Instantiate(_cardPrefab, parent);
        cardObject.name = card.CardName;

        CardUI cardUI = cardObject.AddComponent<CardUI>();
        cardUI.card = card;

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

        return cardObject;
    }

    private void SetupCardInteraction(GameObject cardObject)
    {
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

        _cardOutlineManager.HighlightCard(cardObject);
        CurrentSelectedCard = _cardOutlineManager.CardIsHighlighted ? cardObject : null;
        Debug.Log(CurrentSelectedCard != null
            ? $"Selected Card: {CurrentSelectedCard.name}"
            : "Deselected Card");
    }

    public void RemoveCard(GameObject cardObject)
    {
        if (!_handCardObjects.Contains(cardObject))
        {
            Debug.LogWarning($"Card {cardObject.name} not found in hand");
            return;
        }

        _handCardObjects.Remove(cardObject);
        if (cardObject.GetComponent<CardUI>()?.card is Card card)
            _playerDeck.Hand.Remove(card);

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
        foreach (Transform t in container)
            Destroy(t.gameObject);
    }
}