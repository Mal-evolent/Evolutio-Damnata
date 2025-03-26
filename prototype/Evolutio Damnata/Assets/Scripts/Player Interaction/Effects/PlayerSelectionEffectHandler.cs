using UnityEngine;
using UnityEngine.UI;


public class PlayerSelectionEffectHandler : ISelectionEffectHandler
{
    private readonly SpritePositioning _spritePositioning;
    private readonly CardManager _cardManager;
    private readonly Color _selectionColor = new Color(0.5f, 1f, 0.5f, 1f); // Light green tint

    public PlayerSelectionEffectHandler(SpritePositioning spritePositioning, CardManager cardManager)
    {
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
    }

    public void ApplyEffect(bool isSelected = true)
    {
        if (_cardManager.CurrentSelectedCard == null) return;

        foreach (var entity in _spritePositioning.PlayerEntities)
        {
            if (entity == null) continue;

            var image = entity.GetComponent<Image>();
            if (image != null)
            {
                if (entity == _cardManager.CurrentSelectedCard && isSelected)
                {
                    image.color = _selectionColor;
                }
                else
                {
                    image.color = Color.white;
                }
            }
        }
    }
}