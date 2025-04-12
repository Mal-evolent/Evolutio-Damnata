using System.Collections;
using UnityEngine;
using System.Linq;

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

            // Keep playing cards until we can't play anymore
            while (true)
            {
                // Get all playable cards sorted by mana cost (highest first to optimize mana usage)
                var playableCards = _combatManager.EnemyDeck.Hand
                    .Select((card, index) => new { Card = card, Index = index })
                    .Where(item => item.Card != null && IsPlayableCard(item.Card))
                    .OrderByDescending(item => item.Card.CardType.ManaCost)
                    .ToList();

                if (playableCards.Count == 0)
                {
                    Debug.Log("No more playable cards");
                    break;
                }

                // Find first available position
                int availablePosition = -1;
                for (int i = 0; i < _spritePositioning.EnemyEntities.Count; i++)
                {
                    if (_spritePositioning.EnemyEntities[i] == null) continue;
                    
                    var entity = _spritePositioning.EnemyEntities[i].GetComponent<EntityManager>();
                    if (entity != null && !entity.placed)
                    {
                        availablePosition = i;
                        break;
                    }
                }

                if (availablePosition == -1)
                {
                    Debug.Log("No more available positions on the board");
                    break;
                }

                // Play the card
                var cardToPlay = playableCards[0];
                if (!_combatManager.EnemyDeck.TryRemoveCardAt(cardToPlay.Index, out Card removedCard))
                {
                    Debug.LogError($"Failed to remove card {cardToPlay.Card.CardName} from hand");
                    break;
                }

                bool success = _combatStage.EnemyCardSpawner.SpawnCard(cardToPlay.Card.CardName, availablePosition);
                if (!success)
                {
                    Debug.LogError($"Failed to spawn card {cardToPlay.Card.CardName}");
                    _combatManager.EnemyDeck.Hand.Insert(cardToPlay.Index, removedCard);
                    break;
                }
                else
                {
                    Debug.Log($"Enemy successfully played: {cardToPlay.Card.CardName} (Mana Cost: {cardToPlay.Card.CardType.ManaCost}, Remaining Mana: {_combatManager.EnemyMana})");
                    LogCardsInHand();
                }

                // Wait between card plays
                yield return new WaitForSeconds(1f);
            }

            yield return new WaitForSeconds(1f);
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

        // Get all placed enemy monsters that still have attacks remaining
        var enemyMonsters = _spritePositioning.EnemyEntities
            .Where(entity => entity != null && entity.GetComponent<EntityManager>()?.placed == true)
            .Select(entity => entity.GetComponent<EntityManager>())
            .Where(entity => entity != null && entity.GetRemainingAttacks() > 0)
            .ToList();

        if (enemyMonsters.Count == 0)
        {
            Debug.Log("No enemy monsters available to attack or all monsters have used their attacks");
            yield break;
        }

        foreach (var attacker in enemyMonsters)
        {
            // Add a small delay between each monster's attack
            yield return new WaitForSeconds(0.5f);

            // Double check the monster still has attacks left
            if (attacker.GetRemainingAttacks() <= 0)
            {
                Debug.Log($"Monster {attacker.name} has no attacks left this turn, skipping");
                continue;
            }

            // Get current alive player monsters (recheck each time as previous attacks might have killed some)
            var playerMonsters = _spritePositioning.PlayerEntities
                .Where(entity => entity != null && entity.GetComponent<EntityManager>()?.placed == true)
                .Select(entity => entity.GetComponent<EntityManager>())
                .Where(entity => entity != null && !entity.dead) // Only target alive monsters
                .ToList();

            if (playerMonsters.Count == 0)
            {
                // If no player monsters or all are dead, attack the player health icon
                var playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
                if (playerHealthIcon != null)
                {
                    Debug.Log($"Enemy monster {attacker.name} attacking player health icon (Attacks remaining: {attacker.GetRemainingAttacks()})");
                    _combatStage.HandleMonsterAttack(attacker, playerHealthIcon);
                }
                else
                {
                    Debug.LogError("Could not find player health icon!");
                }
            }
            else
            {
                // Check for taunt units first
                if (CombatRulesEngine.HasTauntUnits(_spritePositioning.PlayerEntities))
                {
                    var tauntUnits = CombatRulesEngine.GetAllTauntUnits(_spritePositioning.PlayerEntities);
                    if (tauntUnits.Count > 0)
                    {
                        // Attack a random taunt unit
                        int tauntIndex = Random.Range(0, tauntUnits.Count);
                        var tauntUnit = tauntUnits[tauntIndex];
                        Debug.Log($"Enemy monster {attacker.name} attacking taunt unit {tauntUnit.name} (Attacks remaining: {attacker.GetRemainingAttacks()})");
                        _combatStage.HandleMonsterAttack(attacker, tauntUnit);
                        continue;
                    }
                }

                // If no taunt units or they're dead, attack a random player monster that's still alive
                int randomIndex = Random.Range(0, playerMonsters.Count);
                var target = playerMonsters[randomIndex];
                Debug.Log($"Enemy monster {attacker.name} attacking player monster {target.name} (Attacks remaining: {attacker.GetRemainingAttacks()})");
                _combatStage.HandleMonsterAttack(attacker, target);
            }
        }

        yield return new WaitForSeconds(1f);
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

        // Check if there's an available position on the board
        bool hasAvailablePosition = _spritePositioning.EnemyEntities
            .Any(entity => entity != null && 
                 entity.GetComponent<EntityManager>() != null && 
                 !entity.GetComponent<EntityManager>().placed);

        if (!hasAvailablePosition)
        {
            Debug.Log("No available positions on the board");
            return false;
        }

        return true;
    }
}
