using UnityEngine;
using UnityEngine.UI;

/*
 * This class is responsible for handling the enemy selection effect.
 */

public class EnemySelectionEffectHandler
{
    private SpritePositioning spritePositioning;

    public EnemySelectionEffectHandler(SpritePositioning spritePositioning)
    {
        this.spritePositioning = spritePositioning;
    }

    public void ApplyEffect(bool active)
    {
        for (int i = 0; i < spritePositioning.enemyEntities.Count; i++)
        {
            if (spritePositioning.enemyEntities[i] != null)
            {
                Image placeholderImage = spritePositioning.enemyEntities[i].GetComponent<Image>();
                if (placeholderImage != null && placeholderImage.sprite != null)
                {
                    if (placeholderImage.sprite.name != "wizard_outline")
                    {
                        //apply effect here
                    }
                }
            }
        }
    }
}
