using System.Collections.Generic;
using UnityEngine;


public class SpellEffectApplier : ISpellEffectApplier
{
    private readonly ICardManager _cardManager;
    private readonly IEffectApplier _effectApplier;
    private readonly IDamageVisualizer _damageVisualizer;
    private readonly GameObject _damageNumberPrefab;
    private readonly ICardLibrary _cardLibrary;
    
    // Add direct reference to combat manager
    private ICombatManager _combatManager;

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
        
        // Try to get CombatManager reference
        _combatManager = _cardManager as ICombatManager;
        if (_combatManager == null)
        {
            _combatManager = FindCombatManager();
        }
    }
    
    // Helper method to find the combat manager
    private ICombatManager FindCombatManager()
    {
        var combatManager = GameObject.FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            Debug.Log("[SpellEffectApplier] Found CombatManager through FindObjectOfType");
            return combatManager;
        }
        Debug.LogWarning("[SpellEffectApplier] Could not find CombatManager in the scene");
        return null;
    }

    // Method to explicitly set the combat manager reference
    public void SetCombatManager(ICombatManager combatManager)
    {
        if (combatManager != null)
        {
            _combatManager = combatManager;
            Debug.Log("[SpellEffectApplier] Combat manager reference explicitly set");
        }
        else
        {
            Debug.LogWarning("[SpellEffectApplier] Attempted to set null combat manager");
        }
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
        if (_combatManager == null)
        {
            _combatManager = _cardManager as ICombatManager;
            if (_combatManager == null)
            {
                _combatManager = FindCombatManager();
            }
        }

        if (_combatManager != null)
        {
            Debug.Log($"[SpellEffectApplier] Attempting to record player spell card: {spellData.CardName} targeting {target.name}");
            if (CardHistory.Instance == null)
            {
                Debug.LogError("[SpellEffectApplier] CardHistory.Instance is null! Player spell card play won't be recorded.");
            }
            else
            {
                CardHistory.Instance.RecordCardPlay(
                    spellCard,
                    target,
                    _combatManager.TurnCount,
                    spellData.ManaCost
                );
                Debug.Log($"[SpellEffectApplier] Successfully recorded player spell card: {spellData.CardName}");
            }
        }
        else
        {
            Debug.LogError("[SpellEffectApplier] CombatManager reference is null, can't record player spell card history");
        }

        // IMPORTANT: Remove the card from the hand BEFORE applying effects
        // This ensures the hand has room for any cards that might be drawn
        _cardManager.RemoveCard(selectedCard);

        // Now apply effects after the card is removed
        ApplyEffectsToTarget(target, spellData);
    }

    // New overload for AI use that doesn't require a selected card
    public void ApplySpellEffectsAI(EntityManager target, CardData spellData, int positionIndex)
    {
        if (target == null || spellData == null)
        {
            Debug.LogError("Invalid spell effect parameters");
            return;
        }
        
        // Special handling for health icons, which are always considered "placed"
        bool isHealthIcon = target is HealthIconManager;
        
        // Only check placed status for normal entities, not health icons
        if (!isHealthIcon && !target.placed)
        {
            Debug.LogError($"Cannot apply spell effect to {target.name}: target is not placed on the board");
            return;
        }

        // Create a simple data wrapper for history tracking instead of a Card MonoBehaviour
        var cardDataWrapper = new CardDataWrapper(spellData);
        
        // Record the spell card play in history
        if (_combatManager == null)
        {
            _combatManager = _cardManager as ICombatManager;
            if (_combatManager == null)
            {
                _combatManager = FindCombatManager();
            }
        }
        
        if (_combatManager != null)
        {
            Debug.Log($"[SpellEffectApplier] Attempting to record AI spell card: {spellData.CardName} targeting {target.name}");
            if (CardHistory.Instance == null)
            {
                Debug.LogError("[SpellEffectApplier] CardHistory.Instance is null! AI spell card play won't be recorded.");
            }
            else
            {
                CardHistory.Instance.RecordCardPlay(
                    cardDataWrapper,
                    target,
                    _combatManager.TurnCount,
                    spellData.ManaCost
                );
                Debug.Log($"[SpellEffectApplier] Successfully recorded AI spell card: {spellData.CardName}");
            }
        }
        else
        {
            Debug.LogError("[SpellEffectApplier] CombatManager reference is null, can't record AI spell card history");
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
        // Check for bloodprice first to apply self-damage to the caster
        if (spellData.EffectTypes.Contains(SpellEffect.Bloodprice) && spellData.BloodpriceValue > 0)
        {
            // Get health icons
            var playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
            var enemyHealthIcon = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
            HealthIconManager casterHealthIcon = null;

            // Determine which health icon is the caster based on current combat phase
            if (_combatManager != null)
            {
                if (_combatManager.IsPlayerPrepPhase() || _combatManager.IsPlayerCombatPhase())
                {
                    // If in player phase, the player is the caster
                    casterHealthIcon = playerHealthIcon;
                    Debug.Log("[SpellEffectApplier] Player phase - Player is caster");
                }
                else if (_combatManager.IsEnemyPrepPhase() || _combatManager.IsEnemyCombatPhase())
                {
                    // If in enemy phase, the enemy is the caster
                    casterHealthIcon = enemyHealthIcon;
                    Debug.Log("[SpellEffectApplier] Enemy phase - Enemy is caster");
                }
                else
                {
                    Debug.LogWarning($"[SpellEffectApplier] Unexpected phase for bloodprice: {_combatManager.CurrentPhase}");
                }

                if (casterHealthIcon != null)
                {
                    // Use the specialized method for blood price
                    casterHealthIcon.ApplyBloodPriceDamage(spellData.BloodpriceValue);
                    Debug.Log($"Applied {spellData.BloodpriceValue} bloodprice damage to {casterHealthIcon.name}");
                }
                else
                {
                    Debug.LogError("Could not find caster's health icon for bloodprice effect");
                }
            }
            else
            {
                Debug.LogError("CombatManager is null, cannot determine whose turn it is for bloodprice effect");
            }
        }



        // Continue with the rest of the effects
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
                case SpellEffect.Draw:
                    ApplyDrawEffect(spellData.DrawValue);
                    break;
                case SpellEffect.Bloodprice:
                    // The bloodprice effect is handled at the beginning of this method
                    break;

                default:
                    Debug.LogWarning($"Unknown effect type: {effectType}");
                    break;
            }
        }
    }

    private void ApplyDrawEffect(int drawCount)
    {
        if (drawCount <= 0)
        {
            Debug.LogWarning("[SpellEffectApplier] Draw effect called with invalid draw count: " + drawCount);
            return;
        }

        if (_combatManager == null)
        {
            Debug.LogError("[SpellEffectApplier] CombatManager is null, cannot determine whose turn it is for draw effect");
            return;
        }

        // Determine which deck to draw from based on the current phase
        Deck deckToDraw = null;

        if (_combatManager.IsPlayerPrepPhase() || _combatManager.IsPlayerCombatPhase())
        {
            // If in player phase, draw from player deck
            deckToDraw = _combatManager.PlayerDeck;
            Debug.Log($"[SpellEffectApplier] Drawing {drawCount} cards for Player");
        }
        else if (_combatManager.IsEnemyPrepPhase() || _combatManager.IsEnemyCombatPhase())
        {
            // If in enemy phase, draw from enemy deck
            deckToDraw = _combatManager.EnemyDeck;
            Debug.Log($"[SpellEffectApplier] Drawing {drawCount} cards for Enemy");
        }
        else
        {
            Debug.LogWarning($"[SpellEffectApplier] Unexpected phase for draw effect: {_combatManager.CurrentPhase}");
            return;
        }

        if (deckToDraw == null)
        {
            Debug.LogError("[SpellEffectApplier] Could not determine which deck to draw from");
            return;
        }

        // Draw the specified number of cards
        for (int i = 0; i < drawCount; i++)
        {
            deckToDraw.DrawOneCard();
        }

        Debug.Log($"[SpellEffectApplier] Successfully drew {drawCount} cards");
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

    private void ApplyDamageEffect(EntityManager target, int damage, bool isBloodpriceEffect = false)
    {
        // Store health before damage to check if this kills the entity
        float healthBeforeDamage = target.GetHealth();

        // For Bloodprice, we don't need to do anything here since it's handled separately 
        // in the ApplyEffectsToTarget method with the ApplyBloodPriceDamage call
        if (!isBloodpriceEffect)
        {
            // Only apply damage for non-bloodprice effects
            target.TakeDamage(damage);
            _damageVisualizer?.CreateDamageNumber(target, damage, target.transform.position, _damageNumberPrefab);
        }

        // Check if the spell killed the entity
        if (healthBeforeDamage > 0 && target.GetHealth() <= 0)
        {
            if (GraveYard.Instance != null)
            {
                GraveYard.Instance.AddSpellKill(target, isBloodpriceEffect ? "Blood Price Effect" : "Direct Damage Spell", damage);
            }
        }
    }
}
