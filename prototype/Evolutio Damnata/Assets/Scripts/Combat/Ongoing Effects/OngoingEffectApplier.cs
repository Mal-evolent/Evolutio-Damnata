using UnityEngine;

public class OngoingEffectApplier : IEffectApplier
{
    private readonly ICardManager _cardManager;

    public OngoingEffectApplier(ICardManager cardManager)
    {
        _cardManager = cardManager;
    }

    public void ApplyEffects(EntityManager entity)
    {
        if (entity == null || entity.dead) return;
        StackManager.Instance?.ProcessStackForEntity(entity);
    }

    public void AddEffect(IOngoingEffect effect, int duration)
    {
        if (effect == null || effect.TargetEntity == null || effect.TargetEntity.dead) return;

        string cardName = GetCardName();
        StackManager.Instance?.PushEffect(effect, duration, cardName);

        Debug.Log($"Added {effect.EffectType} effect from {cardName} to {effect.TargetEntity.name} " +
                 $"with value {effect.EffectValue} for {duration} turns");
    }

    private string GetCardName()
    {
        // Try to get card name through multiple potential paths
        if (_cardManager.CurrentSelectedCard == null) return "Unknown";

        // Check for Card component first
        var card = _cardManager.CurrentSelectedCard.GetComponent<Card>();
        if (card != null) return card.CardName;

        // Then check for SpellCard specifically
        var spellCard = _cardManager.CurrentSelectedCard.GetComponent<SpellCard>();
        if (spellCard != null) return spellCard.CardName;

        // Finally check for CardUI wrapper
        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        if (cardUI?.Card != null) return cardUI.Card.CardName;

        return "Unknown";
    }

    public void RemoveEffectsForEntity(EntityManager entity)
    {
        if (entity == null || entity.dead) return;
        StackManager.Instance?.RemoveEffectsForEntity(entity);
        Debug.Log($"Removed all ongoing effects from {entity.name}");
    }
}