using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Card Manager for handling UI (requires Odin Inspector if used)
public class CardManager : MonoBehaviour
{
    public Deck playerDeck;
    public Transform cardUIContainer;
    public Sprite cardTemplate;

    // Displays deck in the UI
    public void DisplayDeck()
    {
        // Loop through deck and instantiate UI for each card
        foreach (Card card in playerDeck.Cards)
        {
            // Parent card to cardUIContainer
            GameObject cardUI = new GameObject(card.CardName);
            cardUI.transform.SetParent(cardUIContainer);

            // Set card sprite
            SpriteRenderer cardSprite = cardUI.AddComponent<SpriteRenderer>();
            cardSprite.sprite = cardTemplate;

            Debug.Log($"Displayed {card.CardName} in the UI.");
        }
    }

    // Update UI elements (for use with Odin Inspector or similar)
    public void RefreshUI()
    {
        DisplayDeck(); // Reload and update UI display of cards
    }
}
