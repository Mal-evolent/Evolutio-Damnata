using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyInteraction.Managers.Execution
{
    public class CardPlayExecutor : ICardPlayExecutor
    {
        private readonly ICombatManager _combatManager;
        private readonly CombatStage _combatStage;
        private readonly ISpellEffectApplier _spellEffectApplier;
        private readonly float _actionDelay;
        private readonly MonsterPositionSelector _monsterPositionSelector;
        private readonly SpellTargetSelector _spellTargetSelector;

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
            _spellEffectApplier = spellEffectApplier;
            _actionDelay = actionDelay;
            _monsterPositionSelector = monsterPositionSelector;
            _spellTargetSelector = spellTargetSelector;
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
                    playedSuccessfully = PlayMonsterCard(card, boardState);

                    if (playedSuccessfully)
                    {
                        _combatManager.EnemyMana -= card.CardType.ManaCost;
                        enemyDeck.RemoveCard(card);
                    }
                }
                else
                {
                    // For spell cards, check if we can play it
                    playedSuccessfully = CanPlaySpellCard(card);
                    if (playedSuccessfully)
                    {
                        // First remove the card and deduct mana
                        _combatManager.EnemyMana -= card.CardType.ManaCost;
                        enemyDeck.RemoveCard(card);

                        // Then play the spell effect
                        yield return PlaySpellCardWithDelay(card);
                    }
                }

                if (playedSuccessfully)
                {
                    // Update board state after successful play
                    // We'll need to update this via callback since we're in a separate class now
                    if (_combatManager.EnemyMana < 1) break;
                }

                // Additional delay after the card effect is applied
                yield return new WaitForSeconds(_actionDelay * 0.5f);
            }
        }

        public bool PlayMonsterCard(Card card, BoardState boardState)
        {
            int position = _monsterPositionSelector.FindOptimalMonsterPosition(card, boardState);
            if (position < 0)
            {
                Debug.Log($"[CardPlayExecutor] No valid position for {card.CardName}");
                return false;
            }

            bool success = _combatStage.EnemyCardSpawner.SpawnCard(card.CardName, position);
            if (success)
            {
                Debug.Log($"[CardPlayExecutor] Played {card.CardName} at position {position}");
            }
            return success;
        }

        public bool CanPlaySpellCard(Card card)
        {
            if (_spellEffectApplier == null)
            {
                Debug.LogError("[CardPlayExecutor] SpellEffectApplier is null");
                return false;
            }

            // Check if the card contains only Draw and/or Bloodprice effects
            if (_spellTargetSelector.ContainsOnlyDrawAndBloodpriceEffects(card.CardType))
            {
                // These effects don't require a target, so always return true
                return true;
            }

            // For other spell effects, find a valid target
            var target = _spellTargetSelector.GetBestSpellTarget(card.CardType);
            if (target == null)
            {
                Debug.Log($"[CardPlayExecutor] No target for spell {card.CardName}");
                return false;
            }

            return true;
        }

        public IEnumerator PlaySpellCardWithDelay(Card card)
        {
            // Small pause before applying the effect
            yield return new WaitForSeconds(_actionDelay * 0.5f);

            try
            {
                // Check if it's a Draw/Bloodprice only card
                if (_spellTargetSelector.ContainsOnlyDrawAndBloodpriceEffects(card.CardType))
                {
                    // For Draw/Bloodprice effects, we can use any valid entity as the target
                    // since the SpellEffectApplier will handle these effects appropriately
                    var dummyTarget = _spellTargetSelector.GetDummyTarget();
                    if (dummyTarget != null)
                    {
                        Debug.Log($"[CardPlayExecutor] Casting utility spell {card.CardName}");
                        _spellEffectApplier.ApplySpellEffectsAI(dummyTarget, card.CardType, 0);
                    }
                    else
                    {
                        Debug.LogError($"[CardPlayExecutor] Could not find any target for utility spell {card.CardName}");
                    }
                }
                else
                {
                    // For spells that target specific entities
                    var target = _spellTargetSelector.GetBestSpellTarget(card.CardType);
                    if (target != null)
                    {
                        Debug.Log($"[CardPlayExecutor] Casting {card.CardName} on {target.name}");
                        _spellEffectApplier.ApplySpellEffectsAI(target, card.CardType, 0);
                    }
                    else
                    {
                        Debug.LogError($"[CardPlayExecutor] Target was null for spell {card.CardName}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardPlayExecutor] Spell error: {e.Message}");
            }
        }

        public bool ContainsOnlyDrawAndBloodpriceEffects(CardData cardData)
        {
            return _spellTargetSelector.ContainsOnlyDrawAndBloodpriceEffects(cardData);
        }
    }
}
