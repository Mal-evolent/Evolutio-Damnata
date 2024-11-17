using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Card Manager for handling UI (requires Odin Inspector if used)
public class CardManager : MonoBehaviour
{
    public Deck playerDeck;
    public RectTransform cardUIContainer;
    public RectTransform deckPanelRect;
    public Sprite cardTemplate;

    // Displays deck in the UI
    public void DisplayDeck()
    {
        // Loop through deck and instantiate UI for each card
        for (int i = playerDeck.Cards.Count - 1; i >= 0; i--) // i is more useful for ordering
        {
            Card card = playerDeck.Cards[i];

            // Parent card to cardUIContainer
            GameObject cardUI = new GameObject(card.CardName);
            RectTransform cardRectTransform = cardUI.AddComponent<RectTransform>();
            cardRectTransform.SetParent(deckPanelRect, false);

            // Set card template sprite
            Image cardImage = cardUI.AddComponent<Image>();
            cardImage.preserveAspect = true;
            cardImage.sprite = cardTemplate;
            cardUI.AddComponent<CanvasRenderer>();

            Debug.Log($"Displayed {card.CardName} in the UI.");
        }
    }
    // Displays hand in the UI
    public void DisplayHand()
    {
        // Loop through deck and instantiate UI for each card
        foreach (Card card in playerDeck.Hand)
        {
            // Can probably simplify below code using prefab...
            // CARD
            // Create new Card object and parent to cardUIContainer
            GameObject cardObject = new GameObject(card.CardName);
            RectTransform cardRectTransform = cardObject.AddComponent<RectTransform>();
            cardRectTransform.SetParent(cardUIContainer, false);

            // Add Image, Canvas Renderer components and set card sprite
            Image cardImage = cardObject.AddComponent<Image>();
            cardObject.AddComponent<CanvasRenderer>();
            cardImage.preserveAspect = true;
            cardImage.sprite = cardTemplate;

            // IMAGE
            // Add Image child object
            GameObject imageObject = new GameObject("Image");
            RectTransform imageRectTransform = imageObject.AddComponent<RectTransform>();
            imageRectTransform.SetParent(cardRectTransform, false);

            // Set image object position
            imageRectTransform.localPosition = new Vector3(-0.05f, 27.7f, 0);
            imageRectTransform.sizeDelta = new Vector2(72, 42f);

            // Add Image, Canvas Renderer components
            imageObject.AddComponent<CanvasRenderer>();
            Image image = imageObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.sprite = card.CardImage;

            // NAME
            // Add Name child object
            GameObject nameObject = new GameObject("Name");
            RectTransform nameRectTransform = nameObject.AddComponent<RectTransform>();
            nameRectTransform.SetParent(cardRectTransform, false);

            // Set name object position
            nameRectTransform.localPosition = new Vector3(-0.05f, 2.775f, 0);
            nameRectTransform.sizeDelta = new Vector2(72, 5.55f);

            // Add TextMeshPro Text, Canvas Renderer components
            nameObject.AddComponent<CanvasRenderer>();
            TextMeshProUGUI nameText = nameObject.AddComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Top;
            nameText.text = card.CardName;
            nameText.fontSize = 6;

            // DESCRIPTION
            // Add Description child object
            GameObject descriptionObject = new GameObject("Description");
            RectTransform descriptionRectTransform = descriptionObject.AddComponent<RectTransform>();
            descriptionRectTransform.SetParent(cardRectTransform, false);

            // Set description object position
            descriptionRectTransform.localPosition = new Vector3(-0.05f, -24.22f, 0);
            descriptionRectTransform.sizeDelta = new Vector2(72, 48);

            // Add TextMeshPro Text, Canvas Renderer components
            descriptionObject.AddComponent<CanvasRenderer>();
            TextMeshProUGUI descriptionText = descriptionObject.AddComponent<TextMeshProUGUI>();
            descriptionText.alignment = TextAlignmentOptions.Top;
            descriptionText.text = card.Description;
            descriptionText.fontSize = 6;

            // MANA COST
            // Add manaCost child object
            GameObject manaObject = new GameObject("Mana Cost");
            RectTransform manaRectTransform = manaObject.AddComponent<RectTransform>();
            manaRectTransform.SetParent(cardRectTransform, false);

            // Set description object position
            manaRectTransform.localPosition = new Vector3(-31.05f, -43.45f, 0);
            manaRectTransform.sizeDelta = new Vector2(10, 10);

            // Add TextMeshPro Text, Canvas Renderer components
            manaObject.AddComponent<CanvasRenderer>();
            TextMeshProUGUI manaText = manaObject.AddComponent<TextMeshProUGUI>();
            manaText.alignment = TextAlignmentOptions.BottomLeft;
            manaText.text = card.ManaCost.ToString();
            manaText.fontSize = 6;

            Debug.Log($"Displayed {card.CardName} in the UI.");
        }
    }

    // Update UI elements (for use with Odin Inspector or similar)
    public void RefreshUI()
    {
        DisplayDeck(); // Reload and update UI display of cards
        DisplayHand(); // Reload and update UI display of cards
    }
}
