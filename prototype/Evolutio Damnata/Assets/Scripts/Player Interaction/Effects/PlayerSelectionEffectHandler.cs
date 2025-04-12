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

        // Get the selected entity if it's a monster
        var selectedEntity = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
        bool isMonsterSelected = selectedEntity != null && selectedEntity.placed;

        // Get card data if it's a card from hand
        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        bool isSpellCard = cardUI != null && cardUI.Card != null && cardUI.Card.CardType != null && cardUI.Card.CardType.IsSpellCard;

        // Only apply effect if we have a valid selection
        if (!isMonsterSelected && !isSpellCard) return;

        foreach (var entity in _spritePositioning.PlayerEntities)
        {
            if (entity == null) continue;

            var image = entity.GetComponent<Image>();
            if (image != null)
            {
                // If it's a monster selection, only highlight the selected monster
                if (isMonsterSelected)
                {
                    image.color = (entity == _cardManager.CurrentSelectedCard) ? _selectionColor : Color.white;
                }
                // If it's a spell card, highlight all monsters
                else if (isSpellCard)
                {
                    image.color = isSelected ? _selectionColor : Color.white;
                }
            }
        }
    }
}