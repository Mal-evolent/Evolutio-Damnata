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

    private void HandleSpellCard(int index, EntityManager entityManager, CardData cardData)
    {
        if (!_manaChecker.HasEnoughPlayerMana(cardData))
            return;

        _spellEffectApplier.ApplySpellEffects(entityManager, cardData, index);
        _manaChecker.DeductPlayerMana(cardData);
        RemoveCardFromHand();
        ResetSelection();
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