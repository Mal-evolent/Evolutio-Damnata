using System.Collections.Generic;
using UnityEngine;

public interface ICardLibrary
{
    // Properties
    Sprite DefaultCardSprite { get; }
    Deck PlayerDeck { get; set; }
    Deck EnemyDeck { get; set; }
    IReadOnlyList<CardData> CardDataList { get; }

    // Methods
    Card CreateCardFromData(CardData cardData);
    List<Card> CreateDeckFromLibrary();
    Sprite GetCardImage(string cardName);
}