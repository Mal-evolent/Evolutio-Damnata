using System.Collections.Generic;

/*
 * The OngoingEffectApplier class is responsible for managing and applying ongoing effects to an entity in a game. 
 * Here's a detailed breakdown of its functionality:
 */

public class OngoingEffectApplier
{
    private List<OngoingEffectManager> ongoingEffects;

    public OngoingEffectApplier(List<OngoingEffectManager> ongoingEffects)
    {
        this.ongoingEffects = ongoingEffects;
    }

    public void ApplyEffects(EntityManager entity)
    {
        for (int i = ongoingEffects.Count - 1; i >= 0; i--)
        {
            OngoingEffectManager effect = ongoingEffects[i];
            effect.ApplyEffect(entity);
            effect.DecreaseDuration();
            if (effect.IsExpired())
            {
                ongoingEffects.RemoveAt(i);
            }
        }
    }
}
