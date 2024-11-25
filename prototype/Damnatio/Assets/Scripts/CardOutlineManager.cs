using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardOutlineManager : MonoBehaviour
{
    private GameObject currentlyHighlightedCard;

    //highlight card
    public void HighlightCard(GameObject cardObject)
    {
        //if there's already a highlighted card, remove its outline
        if (currentlyHighlightedCard != null && currentlyHighlightedCard != cardObject)
        {
            RemoveHighlight(currentlyHighlightedCard);
        }

        //add or enable the outline for a selected card
        Outline outline = cardObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = cardObject.AddComponent<Outline>();
            outline.effectColor = Color.red;
            outline.effectDistance = new Vector2(5f, 5f);
        }

        outline.enabled = true;
        currentlyHighlightedCard = cardObject;
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
    }
}
