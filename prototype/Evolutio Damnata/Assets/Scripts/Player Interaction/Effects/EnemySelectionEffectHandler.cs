using UnityEngine;

public class EnemySelectionEffectHandler : ISelectionEffectHandler
{
    private readonly SpritePositioning _spritePositioning;
    private readonly Color _selectionColor = new Color(1f, 0.5f, 0.5f, 1f); // Light red tint

    public EnemySelectionEffectHandler(SpritePositioning spritePositioning)
    {
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
    }

    public void ApplyEffect(bool isSelected = true)
    {
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