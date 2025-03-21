using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * CardManager is responsible for managing the player's deck and hand of cards.
 * It displays the deck and hand in the UI and handles card selection.
 */

public class CardManager : MonoBehaviour
{
    public Deck playerDeck;
    public RectTransform cardUIContainer;
    public RectTransform deckPanelRect;
    public Sprite cardTemplate;
    public GameObject cardPrefab;

    public CardOutlineManager cardOutlineManager;

    private List<GameObject> deckCardObjects = new List<GameObject>();
    private List<GameObject> handCardObjects = new List<GameObject>();
    [SerializeField]
    CombatManager combatManager;

    public GameObject currentSelectedCard;

    public List<GameObject> getDeckCardObjects() { return deckCardObjects; }
    public List<GameObject> getHandCardObjects() { return handCardObjects; }

    // Displays deck in the UI
    public void DisplayDeck()
    {
        // Loop through deck and instantiate UI for each card
        for (int i = playerDeck.Cards.Count - 1; i >= 0; i--) // i is useful for ordering
        {
            Card card = playerDeck.Cards[i];

            // CARD
            GameObject cardObject = Instantiate(cardPrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
            RectTransform cardRectTransform = cardObject.GetComponent<RectTransform>();
            cardRectTransform.SetParent(deckPanelRect, false);
            cardObject.name = card.CardName;

            // Assign the Card reference to the CardUI component
            CardUI cardUI = cardObject.AddComponent<CardUI>();
            cardUI.card = card;

            // IMAGE
            Image image = cardObject.transform.GetChild(0).GetComponent<Image>();
            image.sprite = card.CardImage;

            // NAME
            TextMeshProUGUI nameText = cardObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            nameText.text = card.CardName;

            // DESCRIPTION
            TextMeshProUGUI descText = cardObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            descText.text = card.Description;

            // CARD ATTRIBUTES
            TextMeshProUGUI attrText = cardObject.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            if (card is MonsterCard monsterCard)
            {
                attrText.text = $"Health: {monsterCard.Health}\nAttack: {monsterCard.AttackPower}\nCost: {monsterCard.ManaCost}";
            }
            else if (card is SpellCard spellCard)
            {
                attrText.text = $"Effect: {string.Join(", ", spellCard.EffectTypes)}\nValue: {spellCard.EffectValue}\nDuration: {spellCard.Duration}\nCost: {spellCard.ManaCost}";
            }

            Debug.Log($"Displayed {card.CardName} in the UI.");
            deckCardObjects.Add(cardObject);
        }
    }

    // Displays hand in the UI
    public void DisplayHand()
    {
        // Loop through hand and instantiate UI for each card
        foreach (Card card in playerDeck.Hand)
        {
            // CARD
            GameObject cardObject = Instantiate(cardPrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
            RectTransform cardRectTransform = cardObject.GetComponent<RectTransform>();
            cardRectTransform.SetParent(cardUIContainer, false);
            cardObject.name = card.CardName;

            // Assign the Card reference to the CardUI component
            CardUI cardUI = cardObject.AddComponent<CardUI>();
            cardUI.card = card;

            // IMAGE
            Image image = cardObject.transform.GetChild(0).GetComponent<Image>();
            image.sprite = card.CardImage;

            // NAME
            TextMeshProUGUI nameText = cardObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            nameText.text = card.CardName;

            // DESCRIPTION
            TextMeshProUGUI descText = cardObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            descText.text = card.Description;

            // CARD ATTRIBUTES
            TextMeshProUGUI attrText = cardObject.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            if (card is MonsterCard monsterCard)
            {
                attrText.text = $"Health: {monsterCard.Health}\nAttack: {monsterCard.AttackPower}\nCost: {monsterCard.ManaCost}";
            }
            else if (card is SpellCard spellCard)
            {
                attrText.text = $"Effect: {string.Join(", ", spellCard.EffectTypes)}\nValue: {spellCard.EffectValue}\nDuration: {spellCard.Duration}\nCost: {spellCard.ManaCost}";
            }

            Debug.Log($"Displayed {card.CardName} in the UI.");

            // Add Button or Event Trigger
            Button cardButton = cardObject.GetComponent<Button>();
            if (cardButton == null)
            {
                cardButton = cardObject.AddComponent<Button>();
            }

            // Use the assigned CardOutlineManager to highlight the card
            cardButton.onClick.AddListener(() =>
            {
                if (cardOutlineManager != null)
                {
                    cardOutlineManager.HighlightCard(cardObject);
                }
                else
                {
                    Debug.LogError("CardOutlineManager is not assigned!");
                }

                if (cardOutlineManager.cardIsHighlighted)
                {
                    currentSelectedCard = cardObject;
                    Debug.Log($"Selected Card: {currentSelectedCard.name}");
                }

                if (!cardOutlineManager.cardIsHighlighted)
                {
                    currentSelectedCard = null;
                    Debug.Log("Deselected Card.");
                }
            });

            handCardObjects.Add(cardObject);
        }
    }

    // Update UI elements (for use with Odin Inspector or similar)
    public void RefreshUI()
    {
        foreach (Transform t in cardUIContainer.transform)
        {
            Destroy(t.gameObject);
        }
        foreach (Transform t in deckPanelRect.transform)
        {
            Destroy(t.gameObject);
        }
        deckCardObjects.Clear();
        handCardObjects.Clear();

        DisplayDeck();
        DisplayHand();
    }
}
