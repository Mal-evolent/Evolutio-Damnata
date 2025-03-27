using UnityEngine;
using UnityEngine.UI;

public class EnemySelectionEffectHandler : ISelectionEffectHandler
{
    private readonly ISpritePositioning _spritePositioning;
    private readonly Color _selectionColor;

    public EnemySelectionEffectHandler(ISpritePositioning spritePositioning, Color selectionColor)
    {
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
        _selectionColor = selectionColor;
    }

    public void ApplyEffect(bool isSelected = true)
    {
        if (_spritePositioning.EnemyEntities == null) return;

        foreach (var entity in _spritePositioning.EnemyEntities)
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