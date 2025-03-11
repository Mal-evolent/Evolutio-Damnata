using UnityEngine;

/*
 * The PlayerSelectionEffectHandler class is responsible for applying the selection effect to the player.
 * It keeps track of the player entities and the current selected card.
 */

public class PlayerSelectionEffectHandler
{
    private SpritePositioning spritePositioning;
    private CardManager cardManager;

    public PlayerSelectionEffectHandler(SpritePositioning spritePositioning, CardManager cardManager)
    {
        this.spritePositioning = spritePositioning;
        this.cardManager = cardManager;
    }

    public void ApplyEffect()
    {
        for (int i = 0; i < spritePositioning.playerEntities.Count; i++)
        {
            if (spritePositioning.playerEntities[i] == cardManager.currentSelectedCard)
            {
                //apply selection effect here
                break;
            }
        }
    }
}
