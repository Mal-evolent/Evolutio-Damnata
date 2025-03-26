using System.Collections.Generic;
using UnityEngine;

public class PlayerCardSelectionHandler
{
    private readonly CardManager _cardManager;
    private readonly ICombatManager _combatManager;
    private readonly CardOutlineManager _cardOutlineManager;
    private readonly SpritePositioning _spritePositioning;
    private readonly CombatStage _combatStage;
    private readonly GeneralEntities _playerCardSpawner;
    private readonly ManaChecker _manaChecker;
    private readonly SpellEffectApplier _spellEffectApplier;

    public PlayerCardSelectionHandler(
        CardManager cardManager,
        ICombatManager combatManager,
        CardOutlineManager cardOutlineManager,
        SpritePositioning spritePositioning,
        CombatStage combatStage,
        GeneralEntities playerCardSpawner)
    {
        _cardManager = cardManager;
        _combatManager = combatManager;
        _cardOutlineManager = cardOutlineManager;
        _spritePositioning = spritePositioning;
        _combatStage = combatStage;
        _playerCardSpawner = playerCardSpawner;
        _manaChecker = new ManaChecker(combatStage, cardOutlineManager, cardManager);
        _spellEffectApplier = new SpellEffectApplier(cardManager);
    }

    public void HandlePlayerCardSelection(int index, EntityManager entityManager)
    {
        if (_cardManager.currentSelectedCard == null)
        {
            if (!entityManager.placed)
            {
                Debug.LogError("No card selected!");
            }
            return;
        }

        CardUI cardUI = _cardManager.currentSelectedCard.GetComponent<CardUI>();
        Card cardComponent = cardUI?.card;
        CardData cardData = cardComponent?.CardType;

        if (!ValidateCardComponents(cardUI, cardComponent, cardData, entityManager))
        {
            return;
        }

        if (cardData.IsMonsterCard)
        {
            HandleMonsterCardSelection(index, cardUI, cardComponent, cardData);
        }
        else if (cardData.IsSpellCard)
        {
            HandleSpellCardSelection(index, entityManager, cardUI, cardComponent, cardData);
        }
        else if (!entityManager.placed)
        {
            Debug.LogError("Unsupported card type!");
        }
    }

    private bool ValidateCardComponents(CardUI cardUI, Card cardComponent, CardData cardData, EntityManager entityManager)
    {
        if (cardUI == null && !entityManager.placed)
        {
            Debug.LogError("CardUI component not found!");
            return false;
        }

        if (cardComponent == null && !entityManager.placed)
        {
            Debug.LogError("Card component not found!");
            return false;
        }

        if (cardData == null && !entityManager.placed)
        {
            Debug.LogError("CardData is null!");
            return false;
        }

        return true;
    }

    private void HandleMonsterCardSelection(int index, CardUI cardUI, Card cardComponent, CardData cardData)
    {
        if (_combatManager.CurrentPhase != CombatPhase.PlayerPrep)
        {
            Debug.LogError("Can only play monsters in Prep phase!");
            ResetCardSelection();
            return;
        }

        if (!_manaChecker.HasEnoughPlayerMana(cardData))
        {
            return;
        }

        _playerCardSpawner.SpawnCards(_cardManager.currentSelectedCard.name, index);
        _manaChecker.DeductPlayerMana(cardData);
        RemoveCardFromHand(cardComponent);
        ResetCardSelection();
        _combatStage.placeHolderActiveState(false);
    }

    private void HandleSpellCardSelection(int index, EntityManager entityManager, CardUI cardUI, Card cardComponent, CardData cardData)
    {
        if (_combatManager.CurrentPhase == CombatPhase.CleanUp)
        {
            Debug.LogError("Cannot cast spells in CleanUp phase!");
            ResetCardSelection();
            return;
        }

        if (!_manaChecker.HasEnoughPlayerMana(cardData))
        {
            return;
        }

        _spellEffectApplier.ApplySpellEffect(entityManager, cardData, index);
        _manaChecker.DeductPlayerMana(cardData);
        RemoveCardFromHand(cardComponent);
        ResetCardSelection();
    }

    private void RemoveCardFromHand(Card cardComponent)
    {
        List<GameObject> handCardObjects = _cardManager.getHandCardObjects();
        GameObject cardToRemove = _cardManager.currentSelectedCard;

        if (handCardObjects.Contains(cardToRemove))
        {
            handCardObjects.Remove(cardToRemove);
            GameObject.Destroy(cardToRemove);
            _cardManager.playerDeck.Hand.Remove(cardComponent);
            Debug.Log("Card removed from hand.");
        }
    }

    private void ResetCardSelection()
    {
        _cardManager.currentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();
    }
}