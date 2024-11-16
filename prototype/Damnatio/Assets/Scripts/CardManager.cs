using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Card Manager for handling UI (requires Odin Inspector if used)
public class CardManager : MonoBehaviour
{
    public Deck playerDeck;
    public Transform cardUIContainer;

    // Displays deck in the UI
    public void DisplayDeck()
    {
        // Loop through deck and instantiate UI for each card
        foreach (Card card in playerDeck.Cards)
        {
            GameObject cardUI = new GameObject(card.CardName); // Placeholder for actual UI logic
            cardUI.transform.SetParent(cardUIContainer);
            Debug.Log($"Displayed {card.CardName} in the UI.");
        }
    }

    // Update UI elements (for use with Odin Inspector or similar)
    public void RefreshUI()
    {
        DisplayDeck(); // Reload and update UI display of cards
    }
}
