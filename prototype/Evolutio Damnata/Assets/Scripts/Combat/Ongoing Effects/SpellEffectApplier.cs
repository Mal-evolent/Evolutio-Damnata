using UnityEngine;

public class SpellEffectApplier : ISpellEffectApplier
{
    private readonly ICardManager _cardManager;
    private readonly IEffectApplier _effectApplier;
    private readonly IDamageVisualizer _damageVisualizer;
    private readonly GameObject _damageNumberPrefab;

    public SpellEffectApplier(
        ICardManager cardManager,
        IEffectApplier effectApplier,
        IDamageVisualizer damageVisualizer,
        GameObject damageNumberPrefab)
    {
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
        _effectApplier = effectApplier ?? throw new System.ArgumentNullException(nameof(effectApplier));
        _damageVisualizer = damageVisualizer ?? throw new System.ArgumentNullException(nameof(damageVisualizer));
        _damageNumberPrefab = damageNumberPrefab ?? throw new System.ArgumentNullException(nameof(damageNumberPrefab));
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

    private void InitializeSpellCard(SpellCard spellCard, CardData spellData)
    {
        spellCard.CardType = spellData;
        spellCard.CardName = spellData.CardName;
        spellCard.EffectTypes = spellData.EffectTypes;
        spellCard.EffectValue = spellData.EffectValue;
        spellCard.Duration = spellData.Duration;
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

                case SpellEffect.Buff:
                    ApplyBuff(target, spellData.EffectValue, spellData.Duration);
                    break;

                case SpellEffect.Debuff:
                    ApplyDebuff(target, spellData.EffectValue, spellData.Duration);
                    break;

                case SpellEffect.DoubleAttack:
                    ApplyDoubleAttack(target, spellData.Duration);
                    break;

                case SpellEffect.Burn:
                    var burnEffect = new OngoingEffectManager(
                        SpellEffect.Burn,
                        spellData.EffectValue,
                        spellData.Duration,
                        target);
                    _effectApplier.AddEffect(burnEffect, spellData.Duration);
                    break;

                default:
                    Debug.LogWarning($"Unknown effect type: {effectType}");
                    break;
            }
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