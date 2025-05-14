using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Models;
using EnemyInteraction.Services;
using System.Collections;
using UnityEngine;

namespace EnemyInteraction.Managers.Execution
{
    public class MonsterCardPlayer
    {
        private readonly ICombatManager _combatManager;
        private readonly CombatStage _combatStage;
        private readonly MonsterPositionSelector _monsterPositionSelector;
        private readonly EntityFadeWaiter _fadeWaiter;

        public MonsterCardPlayer(
            ICombatManager combatManager,
            CombatStage combatStage,
            MonsterPositionSelector monsterPositionSelector,
            EntityFadeWaiter fadeWaiter)
        {
            _combatManager = combatManager;
            _combatStage = combatStage;
            _monsterPositionSelector = monsterPositionSelector;
            _fadeWaiter = fadeWaiter;
        }

        public IEnumerator PlayMonsterCard(Card card, Deck enemyDeck, BoardState boardState)
        {
            int position = _monsterPositionSelector.FindOptimalMonsterPosition(card, boardState);
            if (position < 0)
            {
                Debug.Log($"[MonsterCardPlayer] No valid position for {card.CardName}");
                yield break;
            }

            // Check for entities that are fading out at the selected position
            yield return _fadeWaiter.WaitForFadeOutAtPosition(_combatStage, position);

            // Now that we've waited for any fading out entities, play the card
            bool success = _combatStage.EnemyCardSpawner.SpawnCard(card.CardName, card.CardType, position);
            if (success)
            {
                // Deduct mana and remove card from hand on success
                _combatManager.EnemyMana -= card.CardType.ManaCost;
                enemyDeck.RemoveCard(card);
                Debug.Log($"[MonsterCardPlayer] Played {card.CardName} at position {position}");

                // Refresh entity cache after monster placement
                RefreshEntityCacheAfterAction();
            }
        }

        private void RefreshEntityCacheAfterAction()
        {
            var entityCacheManager = AIServices.Instance?.EntityCacheManager as EntityCacheManager;
            entityCacheManager?.RefreshAfterAction();
        }
    }
}
