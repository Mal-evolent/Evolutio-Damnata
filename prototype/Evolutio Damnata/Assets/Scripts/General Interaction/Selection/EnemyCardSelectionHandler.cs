using UnityEngine;

public class EnemyCardSelectionHandler : IEnemyCardHandler
{
    private readonly ICardManager _cardManager;
    private readonly ICombatManager _combatManager;
    private readonly ICardOutlineManager _cardOutlineManager;
    private readonly ISpritePositioning _spritePositioning;
    private readonly ICombatStage _combatStage;
    private readonly IManaChecker _manaChecker;
    private readonly ISpellEffectApplier _spellEffectApplier;

    public EnemyCardSelectionHandler(
        ICardManager cardManager,
        ICombatManager combatManager,
        ICardOutlineManager cardOutlineManager,
        ISpritePositioning spritePositioning,
        ICombatStage combatStage,
        IManaChecker manaChecker,
        ISpellEffectApplier spellEffectApplier)
    {
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
        _combatManager = combatManager ?? throw new System.ArgumentNullException(nameof(combatManager));
        _cardOutlineManager = cardOutlineManager ?? throw new System.ArgumentNullException(nameof(cardOutlineManager));
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
        _combatStage = combatStage ?? throw new System.ArgumentNullException(nameof(combatStage));
        _manaChecker = manaChecker ?? throw new System.ArgumentNullException(nameof(manaChecker));
        _spellEffectApplier = spellEffectApplier ?? throw new System.ArgumentNullException(nameof(spellEffectApplier));
    }

    public void HandleEnemyCardSelection(int index, EntityManager entityManager)
    {
        if (!ValidateSelection(entityManager))
            return;

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        var cardData = cardUI?.card?.CardType;

        if (!ValidateCardData(cardData, entityManager))
        {
            Debug.LogError("Invalid card data!");
            ResetSelection();
            return;
        }

        if (cardData.IsSpellCard)
        {
            HandleSpellCard(index, entityManager, cardData);
        }
        else if (!entityManager.placed)
        {
            Debug.LogError("Invalid card type for enemy selection!");
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

    private bool ValidateCardData(CardData cardData, EntityManager entityManager)
    {
        if (cardData == null && !entityManager.placed)
        {
            Debug.LogError("Card data is null!");
            return false;
        }

        return true;
    }

    private void HandleSpellCard(int index, EntityManager entityManager, CardData cardData)
    {
        if (!_manaChecker.HasEnoughPlayerMana(cardData))
            return;

        _spellEffectApplier.ApplySpellEffect(entityManager, cardData, index);
        _manaChecker.DeductPlayerMana(cardData);
        RemoveCardFromHand();
        ResetSelection();
    }

    private void RemoveCardFromHand()
    {
        var handCardObjects = _cardManager.GetHandCardObjects();
        var selectedCard = _cardManager.CurrentSelectedCard;

        if (handCardObjects.Contains(selectedCard))
        {
            handCardObjects.Remove(selectedCard);
            Object.Destroy(selectedCard);
            Debug.Log("Removed spell card from hand.");
        }
    }

    private void ResetSelection()
    {
        _cardManager.CurrentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();
    }
}