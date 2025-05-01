using UnityEngine;
using EnemyInteraction.Utilities;
using System.Linq;
using System.Collections.Generic;

public class EnemyCardSelectionHandler : IEnemyCardHandler
{
    private readonly ICardManager _cardManager;
    private readonly ICombatManager _combatManager;
    private readonly ICardOutlineManager _cardOutlineManager;
    private readonly ISpritePositioning _spritePositioning;
    private readonly ICombatStage _combatStage;
    private readonly IManaChecker _manaChecker;
    private readonly ISpellEffectApplier _spellEffectApplier;

    private readonly Dictionary<GameObject, EntityManager> _entityManagerCache;

    public EnemyCardSelectionHandler(
        ICardManager cardManager,
        ICombatManager combatManager,
        ICardOutlineManager cardOutlineManager,
        ISpritePositioning spritePositioning,
        ICombatStage combatStage,
        IManaChecker manaChecker,
        ISpellEffectApplier spellEffectApplier,
        Dictionary<GameObject, EntityManager> entityManagerCache)
    {
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
        _combatManager = combatManager ?? throw new System.ArgumentNullException(nameof(combatManager));
        _cardOutlineManager = cardOutlineManager ?? throw new System.ArgumentNullException(nameof(cardOutlineManager));
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
        _combatStage = combatStage ?? throw new System.ArgumentNullException(nameof(combatStage));
        _manaChecker = manaChecker ?? throw new System.ArgumentNullException(nameof(manaChecker));
        _spellEffectApplier = spellEffectApplier ?? throw new System.ArgumentNullException(nameof(spellEffectApplier));
        _entityManagerCache = entityManagerCache ?? throw new System.ArgumentNullException(nameof(entityManagerCache));
    }

    public void HandleEnemyCardSelection(int index, EntityManager entityManager)
    {
        if (!ValidateSelection(entityManager) || _cardManager.CurrentSelectedCard == null)
        {
            return;
        }

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();

        if (cardUI != null && cardUI.Card != null && cardUI.Card.CardType != null)
        {
            if (cardUI.Card.CardType.IsSpellCard)
            {
                HandleSpellCard(index, entityManager, cardUI.Card.CardType);
                _cardManager.CurrentSelectedCard = null;
                return;
            }
        }
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
            Debug.LogError("EntityManager is null!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if there are any entities on the specified side of the field
    /// </summary>
    /// <param name="isPlayerSide">True to check player side, false to check enemy side</param>
    /// <returns>True if valid entities are present</returns>
    private bool HasEntitiesOnField(bool isPlayerSide)
    {
        if (_spritePositioning == null)
            return false;

        var entities = isPlayerSide ? _spritePositioning.PlayerEntities : _spritePositioning.EnemyEntities;

        // Convert to EntityManager list for AIUtilities
        var entityManagers = entities
            .Where(e => e != null && _entityManagerCache.TryGetValue(e, out _))
            .Select(e => _entityManagerCache[e])
            .ToList();

        // Use inverse of AIUtilities.CanTargetHealthIcon since that returns true when NO entities are on field
        return !AIUtilities.CanTargetHealthIcon(entityManagers);
    }

    private void HandleSpellCard(int index, EntityManager entityManager, CardData cardData)
    {
        if (!_manaChecker.HasEnoughPlayerMana(cardData))
            return;

        // If this is a health icon and there are entities on the field, prevent targeting
        if (entityManager is HealthIconManager)
        {
            bool enemyEntitiesPresent = HasEntitiesOnField(false);

            if (enemyEntitiesPresent)
            {
                Debug.Log("Cannot target enemy health icon with spells while enemy monsters are on the field!");
                ResetSelection();
                return;
            }
        }

        // Check if the card is a spell with only Draw and/or Blood Price effects
        bool isDrawOrBloodPriceOnlySpell = false;
        if (cardData.IsSpellCard && entityManager is HealthIconManager)
        {
            isDrawOrBloodPriceOnlySpell = IsDrawOrBloodPriceOnlySpell(cardData);
        }

        // Check for taunt targeting restrictions
        // Only apply for enemy units and when it's not a utility-only spell
        if (cardData.IsSpellCard && !isDrawOrBloodPriceOnlySpell &&
            entityManager.GetMonsterType() == EntityManager.MonsterType.Enemy)
        {
            if (HasEnemyTauntUnits() && !entityManager.HasKeyword(Keywords.MonsterKeyword.Taunt))
            {
                Debug.LogWarning("You must target an enemy taunt unit with your spell!");
                ResetSelection();
                return;
            }
        }

        _spellEffectApplier.ApplySpellEffects(entityManager, cardData, index);
        _manaChecker.DeductPlayerMana(cardData);
        RemoveCardFromHand();
        ResetSelection();
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

    private bool IsDrawOrBloodPriceOnlySpell(CardData cardData)
    {
        if (cardData.EffectTypes == null)
            return false;

        bool hasDrawOrBloodPrice = false;
        bool hasOtherEffects = false;

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

        // If it only has Draw and/or Blood Price effects
        return hasDrawOrBloodPrice && !hasOtherEffects;
    }

    private void RemoveCardFromHand()
    {
        var selectedCard = _cardManager.CurrentSelectedCard;
        if (selectedCard != null)
        {
            _cardManager.RemoveCard(selectedCard);
            Debug.Log("Removed spell card from hand.");
        }
    }

    private void ResetSelection()
    {
        _cardManager.CurrentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();
    }
}
