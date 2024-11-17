using PlasticPipe.PlasticProtocol.Messages;
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
    public GameObject cardPrefab;

    // Displays deck in the UI
    public void DisplayDeck()
    {
        // Loop through deck and instantiate UI for each card
        for (int i = playerDeck.Cards.Count - 1; i >= 0; i--) // i is more useful for ordering
        {
            Card card = playerDeck.Cards[i];

            // CARD
            GameObject cardObject = Instantiate(cardPrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
            RectTransform cardRectTransform = cardObject.GetComponent<RectTransform>();
            cardRectTransform.SetParent(deckPanelRect, false);
            cardObject.name = card.CardName;
            cardRectTransform.localPosition = new Vector3(20f, -10f, 0f);

            // IMAGE
            Image image = cardObject.transform.GetChild(0).GetComponent<Image>();
            image.sprite = card.CardImage;

            // NAME
            TextMeshProUGUI nameText = cardObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            nameText.text = card.CardName;

            // DESCRIPTION
            TextMeshProUGUI descText = cardObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            descText.text = card.Description;

            // MANA COST
            TextMeshProUGUI manaText = cardObject.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            manaText.text = card.ManaCost.ToString();

            Debug.Log($"Displayed {card.CardName} in the UI.");
        }
    }
    // Displays hand in the UI
    public void DisplayHand()
    {
        // Loop through deck and instantiate UI for each card
        foreach (Card card in playerDeck.Hand)
        {
            // CARD
            GameObject cardObject = Instantiate(cardPrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
            RectTransform cardRectTransform = cardObject.GetComponent<RectTransform>();
            cardRectTransform.SetParent(cardUIContainer, false);
            cardObject.name = card.CardName;

            // IMAGE
            Image image = cardObject.transform.GetChild(0).GetComponent<Image>();
            image.sprite = card.CardImage;

            // NAME
            TextMeshProUGUI nameText = cardObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            nameText.text = card.CardName;

            // DESCRIPTION
            TextMeshProUGUI descText = cardObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            descText.text = card.Description;

            // MANA COST
            TextMeshProUGUI manaText = cardObject.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            manaText.text = card.ManaCost.ToString();

            Debug.Log($"Displayed {card.CardName} in the UI.");
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

        DisplayDeck(); // Reload and update UI display of cards
        DisplayHand(); // Reload and update UI display of cards
    }
}
