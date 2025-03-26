using System.Collections.Generic;

public class OngoingEffectApplier : IEffectApplier
{
    private readonly List<IOngoingEffect> _ongoingEffects;

    public OngoingEffectApplier()
    {
        _ongoingEffects = new List<IOngoingEffect>();
    }

    public void ApplyEffects(EntityManager entity)
    {
        for (int i = _ongoingEffects.Count - 1; i >= 0; i--)
        {
            var effect = _ongoingEffects[i];
            effect.ApplyEffect(entity);
            effect.DecreaseDuration();

            if (effect.IsExpired())
            {
                _ongoingEffects.RemoveAt(i);
            }
        }
    }

    public void RemoveEffectsForEntity(EntityManager entity)
    {
        _ongoingEffects.RemoveAll(e => e.TargetEntity == entity);
    }

    public void AddEffect(IOngoingEffect effect)
    {
        _ongoingEffects.Add(effect);
    }
}