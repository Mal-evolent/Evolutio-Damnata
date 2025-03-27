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

        foreach (var entity in _spritePositioning.PlayerEntities)
        {
            if (entity == null) continue;

            var image = entity.GetComponent<Image>();
            if (image != null)
            {
                image.color = (entity == _cardManager.CurrentSelectedCard && isSelected)
                    ? _selectionColor
                    : Color.white;
            }
        }
    }
}