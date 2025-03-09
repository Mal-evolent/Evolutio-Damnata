using UnityEngine;
using UnityEngine.UI;

/*
 * This script is used to manage the outline of a card when it is selected by the player.
 * It allows the player to select a card and have it highlighted with a green outline.
 * If the player selects a different card, the outline will be removed from the previously selected card.
 * If the player selects the same card again, the outline will be removed.
 * 
 * This script is attached to the GameManager object in the scene.
 */
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

        // Player select SFX
        AudioSource select = GetComponent<AudioSource>();
        if (select.isPlaying)
        {
            select.Stop();
        }
        select.Play();
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
        if(currentlyHighlightedCard != null)
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
}
