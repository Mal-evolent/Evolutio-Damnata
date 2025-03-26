using UnityEngine;

public class PlayerCardSelectionHandler : IPlayerCardHandler
{
    private readonly ICardValidator _cardValidator;
    private readonly ICardRemover _cardRemover;
    private readonly CardOutlineManager _cardOutlineManager;
    private readonly ICardSpawner _cardSpawner;
    private readonly IManaProvider _manaProvider;
    private readonly ISpellEffectApplier _spellEffectApplier;
    private readonly CardManager _cardManager;
    private readonly ICombatManager _combatManager;

    public PlayerCardSelectionHandler(
        CardManager cardManager,
        ICombatManager combatManager,
        ICardValidator cardValidator,
        ICardRemover cardRemover,
        CardOutlineManager cardOutlineManager,
        ICardSpawner cardSpawner,
        IManaProvider manaProvider,
        ISpellEffectApplier spellEffectApplier)
    {
        _cardManager = cardManager;
        _combatManager = combatManager;
        _cardValidator = cardValidator;
        _cardRemover = cardRemover;
        _cardOutlineManager = cardOutlineManager;
        _cardSpawner = cardSpawner;
        _manaProvider = manaProvider;
        _spellEffectApplier = spellEffectApplier;
    }

    public void HandlePlayerCardSelection(int index, EntityManager entityManager)
    {
        if (!ValidateSelection(entityManager))
            return;

        var cardUI = _cardManager.currentSelectedCard.GetComponent<CardUI>();
        var cardData = cardUI?.card?.CardType;

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
        if (_cardManager.currentSelectedCard != null)
            return true;

        if (!entityManager.placed)
            Debug.LogError("No card selected!");

        return false;
    }

    private void ProcessValidCard(int index, EntityManager entityManager, CardData cardData)
    {
        if (_manaProvider.PlayerMana < cardData.ManaCost)
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
        _cardSpawner.SpawnCards(_cardManager.currentSelectedCard.name, index);
    }

    private void HandleSpellCard(int index, EntityManager entityManager, CardData cardData)
    {
        _spellEffectApplier.ApplySpellEffects(entityManager, cardData, index);
    }

    private void FinalizeCardPlay(CardData cardData)
    {
        _manaProvider.PlayerMana -= cardData.ManaCost;
        _cardRemover.RemoveCardFromHand(_cardManager.currentSelectedCard);
        ResetCardSelection();
    }

    private void ResetCardSelection()
    {
        _cardManager.currentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();
    }
}