using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectionEffectHandler : ISelectionEffectHandler
{
    private readonly ISpritePositioning _spritePositioning;
    private readonly ICardManager _cardManager;
    private readonly Color _selectionColor;

    public PlayerSelectionEffectHandler(
        ISpritePositioning spritePositioning,
        ICardManager cardManager,
        Color selectionColor)
    {
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
        _selectionColor = selectionColor;
    }

    public void ApplyEffect(bool isSelected = true)
    {
        if (_cardManager.CurrentSelectedCard == null || _spritePositioning.PlayerEntities == null) return;

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        if (cardUI == null || cardUI.Card == null || cardUI.Card.CardType == null) return;

        // Only apply effect if it's a spell card
        if (!cardUI.Card.CardType.IsSpellCard) return;

        foreach (var entity in _spritePositioning.PlayerEntities)
        {
            if (entity == null) continue;

            var image = entity.GetComponent<Image>();
            if (image != null)
            {
                image.color = isSelected ? _selectionColor : Color.white;
            }
        }
    }
}