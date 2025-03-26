using System.Collections.Generic;
using UnityEngine;

public interface ICardManager
{
    GameObject CurrentSelectedCard { get; set; }
    Deck PlayerDeck { get; }
    List<GameObject> DeckCardObjects { get; }
    List<GameObject> HandCardObjects { get; }

    void DisplayDeck();
    void DisplayHand();
    void RemoveCard(GameObject cardObject);
    void RefreshUI();
}