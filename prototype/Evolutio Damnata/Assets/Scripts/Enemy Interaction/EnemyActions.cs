using System.Collections;
using UnityEngine;

public class EnemyActions : IEnemyActions
{
    private readonly ICombatManager _combatManager;
    private readonly SpritePositioning _spritePositioning;
    private readonly Deck _enemyDeck;
    private readonly CardLibrary _cardLibrary;
    private readonly CombatStage _combatStage;
    private bool _isProcessingCard = false;

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

    public IEnumerator PlayCards()
    {
        if (_isProcessingCard) yield break;
        _isProcessingCard = true;

        try
        {
            Debug.Log("Enemy Playing Cards");

            if (!ValidateCombatState())
            {
                Debug.LogWarning("Cannot play cards in current combat state");
                yield break;
            }

            var (card, index) = FindPlayableCard();
            if (card == null)
            {
                Debug.Log("Enemy has no playable cards");
                yield return new WaitForSeconds(2);
                yield break;
            }

            yield return ExecuteCardPlay(card, index);
            yield return new WaitForSeconds(2);
        }
        finally
        {
            _isProcessingCard = false;
        }
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
            if (card == null) continue;

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
        if (card == null || _combatManager.EnemyDeck.Hand.Count <= index ||
            _combatManager.EnemyDeck.Hand[index] != card)
        {
            Debug.LogWarning($"Card {card?.CardName} not found at index {index}");
            yield break;
        }

        if (_spritePositioning == null)
        {
            Debug.LogError("[EnemyActions] SpritePositioning is null");
            yield break;
        }

        if (_spritePositioning.EnemyEntities == null)
        {
            Debug.LogError("[EnemyActions] EnemyEntities list is null");
            yield break;
        }

        // Find an available position
        EntityManager targetSprite = null;
        int availableIndex = -1;
        for (int i = 0; i < _spritePositioning.EnemyEntities.Count; i++)
        {
            if (_spritePositioning.EnemyEntities[i] == null) continue;
            
            var entity = _spritePositioning.EnemyEntities[i].GetComponent<EntityManager>();
            if (entity != null && !entity.placed)
            {
                targetSprite = entity;
                availableIndex = i;
                break;
            }
        }

        if (targetSprite == null)
        {
            Debug.Log("[===ENEMY ACTIONS===]No available positions to play the card");
            yield break;
        }

        if (!_combatManager.EnemyDeck.TryRemoveCardAt(index, out Card removedCard))
        {
            Debug.LogError($"Failed to remove card {card.CardName} from hand");
            yield break;
        }

        bool success = _combatStage.EnemyCardSpawner.SpawnCard(card.CardName, availableIndex);
        if (!success)
        {
            Debug.LogError($"Failed to spawn card {card.CardName}");
            _combatManager.EnemyDeck.Hand.Insert(index, removedCard);
        }
        else
        {
            Debug.Log($"Enemy successfully played and removed: {card.CardName}");
            LogCardsInHand();
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
        if (card == null || !_combatManager.EnemyDeck.Hand.Contains(card))
        {
            Debug.Log("Card is null or not in hand");
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
