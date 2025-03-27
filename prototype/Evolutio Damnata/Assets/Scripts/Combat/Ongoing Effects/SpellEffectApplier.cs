using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellEffectApplier : ISpellEffectApplier
{
    private readonly CardManager _cardManager;
    private readonly OngoingEffectApplier _ongoingEffectApplier;
    private readonly DamageVisualizer _damageVisualizer;
    private readonly GameObject _damageNumberPrefab;

    public SpellEffectApplier(
        CardManager cardManager,
        OngoingEffectApplier effectApplier,
        DamageVisualizer damageVisualizer,
        GameObject damageNumberPrefab)
    {
        _cardManager = cardManager;
        _ongoingEffectApplier = effectApplier;
        _damageVisualizer = damageVisualizer;
        _damageNumberPrefab = damageNumberPrefab;
    }

    public void ApplySpellEffects(EntityManager target, CardData spellData, int positionIndex)
    {
        // Get or create SpellCard component
        SpellCard spellCard = _cardManager.CurrentSelectedCard.GetComponent<SpellCard>();
        if (spellCard == null)
        {
            spellCard = _cardManager.CurrentSelectedCard.AddComponent<SpellCard>();
            spellCard.CardType = spellData;
            spellCard.CardName = spellData.CardName;
            spellCard.EffectTypes = spellData.EffectTypes;
            spellCard.EffectValue = spellData.EffectValue;
            spellCard.Duration = spellData.Duration;
        }

        // Process all effects
        foreach (var effectType in spellData.EffectTypes)
        {
            switch (effectType)
            {
                case SpellEffect.Damage:
                    ApplyDamageEffect(target, spellData.EffectValue);
                    break;

                case SpellEffect.Heal:
                    target.Heal(spellData.EffectValue);
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
                    _ongoingEffectApplier.AddEffect(burnEffect);
                    break;
            }
        }

        _cardManager.RemoveCard(_cardManager.CurrentSelectedCard);
    }

    private void ApplyDamageEffect(EntityManager target, int damage)
    {
        target.takeDamage(damage);
        if (_damageVisualizer != null && _damageNumberPrefab != null)
        {
            Vector3 position = target.transform.position;
            _damageVisualizer.CreateDamageNumber(target, damage, position, _damageNumberPrefab);
        }
    }

    private void ApplyBuff(EntityManager target, int value, int duration)
    {
        target.ModifyAttack(value); // Assuming this exists
        Debug.Log($"Buffed {target.name}'s attack by {value} for {duration} turns");
    }

    private void ApplyDebuff(EntityManager target, int value, int duration)
    {
        target.ModifyAttack(-value); // Assuming this exists
        Debug.Log($"Debuffed {target.name}'s attack by {value} for {duration} turns");
    }

    private void ApplyDoubleAttack(EntityManager target, int duration)
    {
        target.SetDoubleAttack(duration); // Assuming this exists
        Debug.Log($"{target.name} gains double attack for {duration} turns");
    }
}