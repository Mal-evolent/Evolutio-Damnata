using UnityEngine;

public class CardRemover : ICardRemover
{
    private readonly ICardManager _cardManager;

    public CardRemover(ICardManager cardManager)
    {
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
    }

    public void RemoveCardFromHand(GameObject cardObject)
    {
        if (cardObject == null)
        {
            Debug.LogError("Cannot remove null card object");
            return;
        }

        _cardManager.RemoveCard(cardObject);
        GameObject.Destroy(cardObject);

        var cardComponent = cardObject.GetComponent<CardUI>()?.card;
        if (cardComponent != null)
        {
            _cardManager.PlayerDeck.Hand.Remove(cardComponent);
        }
    }
}