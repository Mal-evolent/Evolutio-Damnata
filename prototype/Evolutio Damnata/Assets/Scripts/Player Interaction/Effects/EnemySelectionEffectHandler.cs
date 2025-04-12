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

        // Check if there are any taunt units
        bool hasTauntUnits = CombatRulesEngine.HasTauntUnits(_spritePositioning.EnemyEntities);
        var tauntUnits = hasTauntUnits ? CombatRulesEngine.GetAllTauntUnits(_spritePositioning.EnemyEntities) : null;

        foreach (var entity in _spritePositioning.EnemyEntities)
        {
            if (entity == null) continue;

            var image = entity.GetComponent<Image>();
            if (image != null)
            {
                // Only highlight if:
                // 1. There are no taunt units, or
                // 2. This entity is a taunt unit
                bool shouldHighlight = !hasTauntUnits || 
                    (tauntUnits != null && tauntUnits.Contains(entity.GetComponent<EntityManager>()));
                
                image.color = isSelected && shouldHighlight ? _selectionColor : Color.white;
            }
        }
    }
}