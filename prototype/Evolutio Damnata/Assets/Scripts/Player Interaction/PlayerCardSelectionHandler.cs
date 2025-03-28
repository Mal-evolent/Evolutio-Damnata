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

    public PlayerCardSelectionHandler(
        ICardManager cardManager,
        ICombatManager combatManager,
        ICardValidator cardValidator,
        ICardRemover cardRemover,
        ICardOutlineManager cardOutlineManager,
        ICardSpawner cardSpawner,
        ISpellEffectApplier spellEffectApplier)
    {
        _cardManager = cardManager;
        _combatManager = combatManager;
        _cardValidator = cardValidator;
        _cardRemover = cardRemover;
        _cardOutlineManager = cardOutlineManager;
        _cardSpawner = cardSpawner;
        _spellEffectApplier = spellEffectApplier;
    }

    public void HandlePlayerCardSelection(int index, EntityManager entityManager)
    {
        if (!ValidateSelection(entityManager))
            return;

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        var cardData = cardUI?.Card?.CardType;

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
        if (_cardManager.CurrentSelectedCard != null)
            return true;

        if (!entityManager.placed)
            Debug.LogError("No card selected!");

        return false;
    }

    private void ProcessValidCard(int index, EntityManager entityManager, CardData cardData)
    {
        if (_combatManager.PlayerMana < cardData.ManaCost)
        {
            Debug.Log($"Not enough mana. Required: {cardData.ManaCost}");
            return;
        }

        if (cardData.IsMonsterCard)
            HandleMonsterCard(index, cardData);
        else
            HandleSpellCard(index, entityManager, cardData);

        FinalizeCardPlay(cardData);
    }

    private void HandleMonsterCard(int index, CardData cardData)
    {
        _cardSpawner.SpawnCard(_cardManager.CurrentSelectedCard.name, index);
    }

    private void HandleSpellCard(int index, EntityManager entityManager, CardData cardData)
    {
        _spellEffectApplier.ApplySpellEffects(entityManager, cardData, index);
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