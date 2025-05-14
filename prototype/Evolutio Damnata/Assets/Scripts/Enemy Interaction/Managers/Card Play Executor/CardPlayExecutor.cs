using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EnemyInteraction.Services;

namespace EnemyInteraction.Managers.Execution
{
    public class CardPlayExecutor : ICardPlayExecutor
    {
        // Core dependencies
        private readonly ICombatManager _combatManager;
        private readonly CombatStage _combatStage;
        private readonly float _actionDelay;

        // Component handlers
        private readonly MonsterCardPlayer _monsterCardPlayer;
        private readonly SpellCardPlayer _spellCardPlayer;

        public CardPlayExecutor(
            ICombatManager combatManager,
            CombatStage combatStage,
            ISpellEffectApplier spellEffectApplier,
            MonsterPositionSelector monsterPositionSelector,
            SpellTargetSelector spellTargetSelector,
            float actionDelay = 0.5f)
        {
            _combatManager = combatManager;
            _combatStage = combatStage;
            _actionDelay = actionDelay;

            // Create specialized components
            var fadeWaiter = new EntityFadeWaiter();

            _monsterCardPlayer = new MonsterCardPlayer(
                combatManager,
                combatStage,
                monsterPositionSelector,
                fadeWaiter);

            _spellCardPlayer = new SpellCardPlayer(
                combatManager,
                spellEffectApplier,
                spellTargetSelector,
                fadeWaiter,
                actionDelay);
        }

        public IEnumerator PlayCardsInOrder(List<Card> cardsToPlay, Deck enemyDeck, BoardState boardState)
        {
            foreach (var card in cardsToPlay)
            {
                if (_combatManager.EnemyMana < card.CardType.ManaCost)
                {
                    Debug.Log($"[CardPlayExecutor] Not enough mana for {card.CardName}");
                    continue;
                }

                // Consistent delay before each card play
                yield return new WaitForSeconds(_actionDelay);

                Debug.Log($"[CardPlayExecutor] Attempting to play {card.CardName}");

                bool playedSuccessfully = false;

                if (card.CardType.IsMonsterCard)
                {
                    yield return _monsterCardPlayer.PlayMonsterCard(card, enemyDeck, boardState);
                    playedSuccessfully = true;
                }
                else
                {
                    // For spell cards, check if we can play it
                    bool canPlay = _spellCardPlayer.CanPlaySpellCard(card);
                    if (canPlay)
                    {
                        // Deduct mana and remove the card first
                        _combatManager.EnemyMana -= card.CardType.ManaCost;
                        enemyDeck.RemoveCard(card);

                        // Then play the spell
                        yield return _spellCardPlayer.PlaySpellCard(card);
                        playedSuccessfully = true;
                    }
                }

                if (playedSuccessfully && _combatManager.EnemyMana < 1)
                    break;

                // Additional delay after the card effect is applied
                yield return new WaitForSeconds(_actionDelay * 0.5f);
            }
        }

        // Proxy methods that delegate to the specialized components
        public IEnumerator PlayMonsterCardWithFadeCheck(Card card, Deck enemyDeck, BoardState boardState)
        {
            return _monsterCardPlayer.PlayMonsterCard(card, enemyDeck, boardState);
        }

        public bool CanPlaySpellCard(Card card)
        {
            return _spellCardPlayer.CanPlaySpellCard(card);
        }

        public IEnumerator PlaySpellCardWithDelay(Card card)
        {
            return _spellCardPlayer.PlaySpellCardWithDelay(card);
        }

        public bool ContainsOnlyDrawAndBloodpriceEffects(CardData cardData)
        {
            return _spellCardPlayer.ContainsOnlyDrawAndBloodpriceEffects(cardData);
        }
    }
}
