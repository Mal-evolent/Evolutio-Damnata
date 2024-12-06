using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardOutlineManager : MonoBehaviour
{
    private GameObject currentlyHighlightedCard;
    public bool cardIsHighlighted;

    //highlight card
    public void HighlightCard(GameObject cardObject)
    {
        //if there's already a highlighted card, remove its outline
        if (currentlyHighlightedCard != null && currentlyHighlightedCard != cardObject)
        {
            RemoveHighlight(currentlyHighlightedCard);
        }

        //remove the outline for the card if it's already highlighted
        if(currentlyHighlightedCard == cardObject)
        {
            RemoveHighlight(cardObject);
            return;
        }

        //add or enable the outline for a selected card
        Outline outline = cardObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = cardObject.AddComponent<Outline>();
            outline.effectColor = Color.green;
            outline.effectDistance = new Vector2(6f, 6f);
        }

        outline.enabled = true;
        currentlyHighlightedCard = cardObject;
        cardIsHighlighted = true;
    }

    public void RemoveHighlight(GameObject cardObject)
    {
        Outline outline = cardObject.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        //clear the highlighted card reference if it matches the one being unhighlighted
        if (currentlyHighlightedCard == cardObject)
        {
            currentlyHighlightedCard = null;
        }
        cardIsHighlighted = false;
    }

    // Overloaded function to remove highlight from currentlyHighlightedCard
    public void RemoveHighlight()
    {
        Outline outline = currentlyHighlightedCard.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        //clear the highlighted card reference if it matches the one being unhighlighted
        currentlyHighlightedCard = null;
        cardIsHighlighted = false;
    }
}
