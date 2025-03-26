using UnityEngine;

public class CardRemover : ICardRemover
{
    private readonly CardManager _cardManager;

    public CardRemover(CardManager cardManager)
    {
        _cardManager = cardManager;
    }

    public void RemoveCardFromHand(GameObject cardObject)
    {
        var handCards = _cardManager.getHandCardObjects();
        if (handCards.Contains(cardObject))
        {
            handCards.Remove(cardObject);
            GameObject.Destroy(cardObject);

            var cardComponent = cardObject.GetComponent<CardUI>()?.card;
            if (cardComponent != null)
            {
                _cardManager.playerDeck.Hand.Remove(cardComponent);
            }
        }
    }
}