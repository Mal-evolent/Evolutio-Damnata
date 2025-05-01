using UnityEngine;

public class PlayerCardSelectionHandler : IPlayerCardHandler
{
    private readonly ICardManager _cardManager;
    private readonly ICardValidator _cardValidator;
    private readonly ICardRemover _cardRemover;
    private readonly ICardOutlineManager _cardOutlineManager;
    private readonly ICardSpawner _cardSpawner;
    private readonly ISpellEffectApplier _spellEffectApplier;
    private readonly ICombatManager _combatManager;
    private readonly ISpritePositioning _spritePositioning;

    public PlayerCardSelectionHandler(
        ICardManager cardManager,
        ICombatManager combatManager,
        ICardValidator cardValidator,
        ICardRemover cardRemover,
        ICardOutlineManager cardOutlineManager,
        ICardSpawner cardSpawner,
        ISpellEffectApplier spellEffectApplier,
        ISpritePositioning spritePositioning)
    {
        _cardManager = cardManager;
        _combatManager = combatManager;
        _cardValidator = cardValidator;
        _cardRemover = cardRemover;
        _cardOutlineManager = cardOutlineManager;
        _cardSpawner = cardSpawner;
        _spellEffectApplier = spellEffectApplier;
        _spritePositioning = spritePositioning;
    }

    public void HandlePlayerCardSelection(int index, EntityManager entityManager)
    {
        if (!ValidateSelection(entityManager))
        {
            ResetCardSelection();
            return;
        }

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        if (cardUI == null)
        {
            Debug.LogError("Selected card has no CardUI component");
            ResetCardSelection();
            return;
        }

        var cardData = cardUI.Card?.CardType;
        if (cardData == null)
        {
            Debug.LogError("Selected card has no valid CardData");
            ResetCardSelection();
            return;
        }

        // Early check for mana availability
        if (_combatManager.PlayerMana < cardData.ManaCost)
        {
            Debug.Log($"Not enough mana. Required: {cardData.ManaCost}, Available: {_combatManager.PlayerMana}");
            ResetCardSelection();
            return;
        }

        // Check if the card is a spell with only Draw and/or Blood Price effects
        bool isDrawOrBloodPriceOnlySpell = false;
        if (cardData.IsSpellCard && !entityManager.placed)
        {
            bool hasOtherEffects = false;
            bool hasDrawOrBloodPrice = false;

            if (cardData.EffectTypes != null)
            {
                foreach (var effect in cardData.EffectTypes)
                {
                    if (effect == SpellEffect.Draw || effect == SpellEffect.Bloodprice)
                    {
                        hasDrawOrBloodPrice = true;
                    }
                    else
                    {
                        hasOtherEffects = true;
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("Spell card has no effect types defined");
                ResetCardSelection();
                return;
            }

            // If it only has Draw and/or Blood Price effects, allow placement on empty spots
            isDrawOrBloodPriceOnlySpell = hasDrawOrBloodPrice && !hasOtherEffects;
        }

        // Standard validations for monster cards and spell cards
        if (cardData.IsMonsterCard && entityManager.placed)
        {
            Debug.LogWarning("Cannot place a monster on an already occupied space!");
            ResetCardSelection();
            return;
        }
        // Add exception for Draw/Blood Price only spell cards
        else if (!cardData.IsMonsterCard && !entityManager.placed && !isDrawOrBloodPriceOnlySpell)
        {
            Debug.LogWarning("Cannot cast a spell on an unoccupied space!");
            ResetCardSelection();
            return;
        }

        // Check for taunt targeting restrictions
        if (cardData.IsSpellCard && !isDrawOrBloodPriceOnlySpell &&
            entityManager.GetMonsterType() == EntityManager.MonsterType.Enemy)
        {
            if (HasEnemyTauntUnits() && !entityManager.HasKeyword(Keywords.MonsterKeyword.Taunt))
            {
                Debug.LogWarning("You must target an enemy taunt unit with your spell!");
                ResetCardSelection();
                return;
            }
        }

        if (!_cardValidator.ValidateCardPlay(cardData, _combatManager.CurrentPhase, entityManager.placed))
        {
            Debug.LogError("Invalid card play conditions");
            ResetCardSelection();
            return;
        }

        ProcessValidCard(index, entityManager, cardData);
    }

    private bool ValidateSelection(EntityManager entityManager)
    {
        if (_cardManager.CurrentSelectedCard == null)
        {
            Debug.LogError("No card selected!");
            return false;
        }

        if (entityManager == null)
        {
            Debug.LogError("Target entity is null");
            return false;
        }

        return true;
    }

    private bool HasEnemyTauntUnits()
    {
        if (_spritePositioning == null || _spritePositioning.EnemyEntities == null)
            return false;

        foreach (var entity in _spritePositioning.EnemyEntities)
        {
            if (entity == null) continue;

            var entityManager = entity.GetComponent<EntityManager>();
            if (entityManager != null &&
                entityManager.placed &&
                !entityManager.dead &&
                !entityManager.IsFadingOut &&
                entityManager.HasKeyword(Keywords.MonsterKeyword.Taunt))
            {
                return true;
            }
        }
        return false;
    }

    private void ProcessValidCard(int index, EntityManager entityManager, CardData cardData)
    {
        // Double-check mana availability (in case it changed)
        if (_combatManager.PlayerMana < cardData.ManaCost)
        {
            Debug.Log($"Not enough mana. Required: {cardData.ManaCost}, Available: {_combatManager.PlayerMana}");
            ResetCardSelection();
            return;
        }

        try
        {
            if (cardData.IsMonsterCard)
                HandleMonsterCard(index, cardData);
            else
                HandleSpellCard(index, entityManager, cardData);

            FinalizeCardPlay(cardData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to process card: {ex.Message}");
            ResetCardSelection();
        }
    }

    private void HandleMonsterCard(int index, CardData cardData)
    {
        bool success = _cardSpawner.SpawnCard(_cardManager.CurrentSelectedCard.name, cardData, index);
        if (!success)
        {
            Debug.LogError("Failed to spawn monster card");
            ResetCardSelection();
            throw new System.Exception("Card spawning failed");
        }
    }

    private void HandleSpellCard(int index, EntityManager entityManager, CardData cardData)
    {
        try
        {
            _spellEffectApplier.ApplySpellEffects(entityManager, cardData, index);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to apply spell effects: {ex.Message}");
            ResetCardSelection();
            throw;
        }
    }

    private void FinalizeCardPlay(CardData cardData)
    {
        _combatManager.PlayerMana -= cardData.ManaCost;
        _cardRemover.RemoveCardFromHand(_cardManager.CurrentSelectedCard);
        ResetCardSelection();
    }

    private void ResetCardSelection()
    {
        _cardManager.CurrentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();
    }
}
