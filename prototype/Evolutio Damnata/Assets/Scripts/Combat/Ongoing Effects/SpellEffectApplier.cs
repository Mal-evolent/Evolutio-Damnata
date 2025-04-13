using UnityEngine;

// Simple data wrapper class for tracking card usage in CardHistory
// Not a MonoBehaviour so it can be created with 'new'
public class CardDataWrapper
{
    public string CardName { get; set; }
    public string Description { get; set; }

    public CardDataWrapper(CardData cardData)
    {
        CardName = cardData.CardName;
        Description = cardData.Description;
    }
}

public class SpellEffectApplier : ISpellEffectApplier
{
    private readonly ICardManager _cardManager;
    private readonly IEffectApplier _effectApplier;
    private readonly IDamageVisualizer _damageVisualizer;
    private readonly GameObject _damageNumberPrefab;
    private readonly ICardLibrary _cardLibrary;

    public SpellEffectApplier(
        ICardManager cardManager,
        IEffectApplier effectApplier,
        IDamageVisualizer damageVisualizer,
        GameObject damageNumberPrefab,
        ICardLibrary cardLibrary)
    {
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
        _effectApplier = effectApplier ?? throw new System.ArgumentNullException(nameof(effectApplier));
        _damageVisualizer = damageVisualizer ?? throw new System.ArgumentNullException(nameof(damageVisualizer));
        _damageNumberPrefab = damageNumberPrefab ?? throw new System.ArgumentNullException(nameof(damageNumberPrefab));
        _cardLibrary = cardLibrary ?? throw new System.ArgumentNullException(nameof(cardLibrary));
    }

    public void ApplySpellEffects(EntityManager target, CardData spellData, int positionIndex)
    {
        if (target == null || spellData == null)
        {
            Debug.LogError("Invalid spell effect parameters");
            return;
        }

        var selectedCard = _cardManager.CurrentSelectedCard;
        if (selectedCard == null)
        {
            Debug.LogError("No card selected for spell effect");
            return;
        }

        // Get or create SpellCard component
        var spellCard = selectedCard.GetComponent<SpellCard>();
        if (spellCard == null)
        {
            spellCard = selectedCard.AddComponent<SpellCard>();
            InitializeSpellCard(spellCard, spellData);
        }

        // Record the spell card play in history
        var combatManager = _cardManager as ICombatManager;
        if (combatManager != null)
        {
            CardHistory.Instance?.RecordCardPlay(
                spellCard,
                target,
                combatManager.TurnCount,
                spellData.ManaCost
            );
        }

        ApplyEffectsToTarget(target, spellData);
        _cardManager.RemoveCard(selectedCard);
    }

    // New overload for AI use that doesn't require a selected card
    public void ApplySpellEffectsAI(EntityManager target, CardData spellData, int positionIndex)
    {
        if (target == null || spellData == null)
        {
            Debug.LogError("Invalid spell effect parameters");
            return;
        }

        // Create a simple data wrapper for history tracking instead of a Card MonoBehaviour
        var cardDataWrapper = new CardDataWrapper(spellData);
        
        // Record the spell card play in history
        var combatManager = _cardManager as ICombatManager;
        if (combatManager != null)
        {
            CardHistory.Instance?.RecordCardPlay(
                cardDataWrapper,
                target,
                combatManager.TurnCount,
                spellData.ManaCost
            );
        }

        ApplyEffectsToTarget(target, spellData);
        // Note: We don't remove any card here since AI handles that separately
    }

    private void InitializeSpellCard(SpellCard spellCard, CardData spellData)
    {
        spellCard.CardType = spellData;
        spellCard.CardName = spellData.CardName;
        spellCard.EffectTypes = spellData.EffectTypes;
        spellCard.EffectValue = spellData.EffectValue;
        spellCard.Duration = spellData.Duration;
        spellCard.DamagePerRound = spellData.EffectValuePerRound;
    }

    private void ApplyEffectsToTarget(EntityManager target, CardData spellData)
    {
        foreach (var effectType in spellData.EffectTypes)
        {
            switch (effectType)
            {
                case SpellEffect.Damage:
                    ApplyDamageEffect(target, spellData.EffectValue);
                    Debug.Log($"Dealt {spellData.EffectValue} damage to {target.name}");
                    break;

                case SpellEffect.Heal:
                    target.Heal(spellData.EffectValue);
                    Debug.Log($"Healed {target.name} for {spellData.EffectValue}");
                    break;
                case SpellEffect.Burn:
                    ApplyBurnEffect(target, spellData);
                    break;

                default:
                    Debug.LogWarning($"Unknown effect type: {effectType}");
                    break;
            }
        }
    }

    private void ApplyBurnEffect(EntityManager target, CardData spellData)
    {
        // Apply initial damage if EffectValue is set
        if (spellData.EffectValue > 0)
        {
            ApplyDamageEffect(target, spellData.EffectValue);
        }

        // Only apply ongoing effect if duration is greater than 0 and has damage per round
        if (spellData.Duration > 0 && spellData.EffectValuePerRound > 0)
        {
            var burnEffect = new OngoingEffectManager(
                SpellEffect.Burn,
                spellData.EffectValuePerRound,
                spellData.Duration,
                target);
            _effectApplier.AddEffect(burnEffect, spellData.Duration);

            Debug.Log($"Applied Burn to {target.name} for {spellData.EffectValuePerRound} damage per round for {spellData.Duration} rounds");
        }
    }

    private void ApplyDamageEffect(EntityManager target, int damage)
    {
        // Store health before damage to check if this kills the entity
        float healthBeforeDamage = target.GetHealth();

        target.TakeDamage(damage);
        _damageVisualizer?.CreateDamageNumber(target, damage, target.transform.position, _damageNumberPrefab);

        // Check if the spell killed the entity
        if (healthBeforeDamage > 0 && target.GetHealth() <= 0)
        {
            if (GraveYard.Instance != null)
            {
                GraveYard.Instance.AddSpellKill(target, "Direct Damage Spell", damage);
            }
        }
    }

    private void ApplyBuff(EntityManager target, int value, int duration)
    {
        target.ModifyAttack(value);
        Debug.Log($"Buffed {target.name}'s attack by {value} for {duration} turns");
    }

    private void ApplyDebuff(EntityManager target, int value, int duration)
    {
        target.ModifyAttack(-value);
        Debug.Log($"Debuffed {target.name}'s attack by {value} for {duration} turns");
    }

    private void ApplyDoubleAttack(EntityManager target, int duration)
    {
        target.SetDoubleAttack(duration);
        Debug.Log($"{target.name} gains double attack for {duration} turns");
    }
}
