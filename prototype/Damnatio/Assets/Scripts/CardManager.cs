using System.Collections;
using System.Collections.Generic;
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
            // Parent card to cardUIContainer
            GameObject cardUI = new GameObject(card.CardName);
            RectTransform cardRectTransform = cardUI.AddComponent<RectTransform>();
            cardRectTransform.SetParent(cardUIContainer, false);

            // Set card sprite
            Image cardImage = cardUI.AddComponent<Image>();
            cardImage.preserveAspect = true;
            cardImage.sprite = cardTemplate;
            cardUI.AddComponent<CanvasRenderer>();

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
