using System.Collections;
using UnityEngine;

public class EnemyActions : IEnemyActions
{
    private readonly ICombatManager _combatManager;
    private readonly SpritePositioning _spritePositioning;
    private readonly Deck _enemyDeck;
    private readonly CardLibrary _cardLibrary;
    private readonly CombatStage _combatStage;

    public EnemyActions(
        ICombatManager combatManager,
        SpritePositioning spritePositioning,
        Deck enemyDeck,
        CardLibrary cardLibrary,
        CombatStage combatStage)
    {
        _combatManager = combatManager;
        _spritePositioning = spritePositioning;
        _enemyDeck = enemyDeck;
        _cardLibrary = cardLibrary;
        _combatStage = combatStage;
    }

    public void InitializeDeck()
    {
        if (_enemyDeck == null)
        {
            Debug.LogError("Enemy deck is not assigned!");
            return;
        }

        _enemyDeck.CardLibrary = _cardLibrary;
        _enemyDeck.PopulateDeck();
        Debug.Log("Enemy deck initialized and shuffled.");
    }

    public IEnumerator PlayCards()
    {
        Debug.Log("Enemy Playing Cards");

        if (!ValidateCombatState())
        {
            Debug.LogWarning("Cannot play cards in current combat state");
            yield break;
        }

        // Phase 1: Find playable card (no yielding)
        var (card, index) = FindPlayableCard();
        if (card == null)
        {
            Debug.Log("Enemy has no playable cards");
            yield return new WaitForSeconds(2);
            yield break;
        }

        // Phase 2: Execute card play (with yield)
        yield return ExecuteCardPlay(card, index);

        yield return new WaitForSeconds(2);
    }

    public IEnumerator Attack()
    {
        Debug.Log("Enemy Attacks");

        if (!ValidateCombatState())
        {
            Debug.LogWarning("Cannot attack in current combat state");
            yield break;
        }

        // Simple attack delay
        yield return new WaitForSeconds(2);
    }

    private (Card card, int index) FindPlayableCard()
    {
        LogCardsInHand();

        for (int i = 0; i < _combatManager.EnemyDeck.Hand.Count; i++)
        {
            Card card = _combatManager.EnemyDeck.Hand[i];
            if (IsPlayableCard(card))
            {
                return (card, i);
            }
        }
        return (null, -1);
    }

    private void LogCardsInHand()
    {
        Debug.Log("Enemy's cards in hand:");
        for (int i = 0; i < _combatManager.EnemyDeck.Hand.Count; i++)
        {
            Card card = _combatManager.EnemyDeck.Hand[i];
            Debug.Log($"Card {i}: {card.CardName}, Mana Cost: {card.CardType.ManaCost}, IsMonsterCard: {card.CardType.IsMonsterCard}");
        }
    }

    private IEnumerator ExecuteCardPlay(Card card, int index)
    {
        Debug.Log($"Enemy attempting to play: {card.CardName}");

        try
        {
            bool success = _combatStage.EnemyCardSpawner.SpawnCard(card.CardName, index);

            if (success)
            {
                _combatManager.EnemyDeck.Hand.Remove(card);
                Debug.Log($"Enemy successfully played: {card.CardName}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to play enemy card: {ex.Message}");
        }

        yield return null;
    }

    private bool ValidateCombatState()
    {
        return _combatManager != null &&
               _combatStage != null &&
               _combatManager.EnemyDeck != null;
    }

    private bool IsPlayableCard(Card card)
    {
        if (card == null)
        {
            Debug.Log("Card is null");
            return false;
        }

        if (card.CardType == null)
        {
            Debug.Log($"Card {card.CardName} has no CardType");
            return false;
        }

        if (!card.CardType.IsMonsterCard)
        {
            Debug.Log($"Card {card.CardName} is not a Monster Card");
            return false;
        }

        if (_combatManager.EnemyMana < card.CardType.ManaCost)
        {
            Debug.Log($"Not enough mana to play card {card.CardName}. Required: {card.CardType.ManaCost}, Available: {_combatManager.EnemyMana}");
            return false;
        }

        return true;
    }
}
