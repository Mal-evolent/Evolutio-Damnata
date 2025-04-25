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
                    // For monster cards, use the coroutine to wait for any fading out entities
                    yield return PlayMonsterCardWithFadeCheck(card, enemyDeck, boardState);

                    // No need to handle mana and card removal here as it's done in the coroutine
                    playedSuccessfully = true;
                }
                else
                {
                    // For spell cards, check if we can play it
                    bool canPlay = CanPlaySpellCard(card);
                    if (canPlay)
                    {
                        // First deduct mana and remove the card
                        _combatManager.EnemyMana -= card.CardType.ManaCost;
                        enemyDeck.RemoveCard(card);

                        // Then play the spell effect with fade-out checking
                        yield return PlaySpellCardWithFadeCheck(card);
                        playedSuccessfully = true;
                    }
                }

                if (playedSuccessfully && _combatManager.EnemyMana < 1)
                    break;

                // Additional delay after the card effect is applied
                yield return new WaitForSeconds(_actionDelay * 0.5f);
            }
        }

        public IEnumerator PlayMonsterCardWithFadeCheck(Card card, Deck enemyDeck, BoardState boardState)
        {
            int position = _monsterPositionSelector.FindOptimalMonsterPosition(card, boardState);
            if (position < 0)
            {
                Debug.Log($"[CardPlayExecutor] No valid position for {card.CardName}");
                yield break;
            }

            // Check for entities that are fading out at the selected position
            yield return WaitForFadeOutAtPosition(position);

            // Now that we've waited for any fading out entities, play the card
            // Pass the CardData from the card to SpawnCard
            bool success = _combatStage.EnemyCardSpawner.SpawnCard(card.CardName, card.CardType, position);
            if (success)
            {
                // Deduct mana and remove card from hand on success
                _combatManager.EnemyMana -= card.CardType.ManaCost;
                enemyDeck.RemoveCard(card);
                Debug.Log($"[CardPlayExecutor] Played {card.CardName} at position {position}");

                // Refresh entity cache after monster placement
                var entityCacheManager = AIServices.Instance?.EntityCacheManager as EntityCacheManager;
                entityCacheManager?.RefreshAfterAction();
            }
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

            // Check if this is a heal card and the target is already at full health
            if (card.CardType.EffectTypes != null &&
                card.CardType.EffectTypes.Contains(SpellEffect.Heal) &&
                Mathf.Approximately(target.GetHealth(), target.GetMaxHealth()))
            {
                Debug.Log($"[CardPlayExecutor] Target for heal spell {card.CardName} already has full health. Skipping to avoid wasting mana.");
                return false;
            }

            return true;
        }

        private IEnumerator PlaySpellCardWithFadeCheck(Card card)
        {
            // Small pause before applying the effect
            yield return new WaitForSeconds(_actionDelay * 0.5f);

            // Check if it's a Draw/Bloodprice only card
            bool isUtilitySpell = _spellTargetSelector.ContainsOnlyDrawAndBloodpriceEffects(card.CardType);
            EntityManager target = GetSpellTarget(card, isUtilitySpell);

            if (target == null)
                yield break;

            if (!isUtilitySpell)
            {
                // Wait for any potential targets that might be fading out
                // Only do this for non-utility spells that target entities
                yield return WaitForTargetsFadeOut(card.CardType);
            }

            // Now that we've waited, apply the spell effect
            ApplySpellEffect(card, target, isUtilitySpell);
        }

        // Helper method to get the appropriate spell target
        private EntityManager GetSpellTarget(Card card, bool isUtilitySpell)
        {
            EntityManager target;

            if (isUtilitySpell)
            {
                // For Draw/Bloodprice effects, we can use any valid entity as the target
                target = _spellTargetSelector.GetDummyTarget();
                if (target == null)
                {
                    Debug.LogError($"[CardPlayExecutor] Could not find any target for utility spell {card.CardName}");
                    return null;
                }
            }
            else
            {
                // For spells that target specific entities
                target = _spellTargetSelector.GetBestSpellTarget(card.CardType);
                if (target == null)
                {
                    Debug.LogError($"[CardPlayExecutor] Target was null for spell {card.CardName}");
                    return null;
                }
            }

            return target;
        }

        private void ApplySpellEffect(Card card, EntityManager target, bool isUtilitySpell)
        {
            try
            {
                if (isUtilitySpell)
                {
                    Debug.Log($"[CardPlayExecutor] Casting utility spell {card.CardName}");
                }
                else
                {
                    Debug.Log($"[CardPlayExecutor] Casting {card.CardName} on {target.name}");
                }

                _spellEffectApplier.ApplySpellEffectsAI(target, card.CardType, 0);

                // Refresh entity cache after spell effect
                var entityCacheManager = AIServices.Instance?.EntityCacheManager as EntityCacheManager;
                entityCacheManager?.RefreshAfterAction();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardPlayExecutor] Spell error: {e.Message}");
            }
        }

        public IEnumerator PlaySpellCardWithDelay(Card card)
        {
            // Small pause before applying the effect
            yield return new WaitForSeconds(_actionDelay * 0.5f);

            bool isUtilitySpell = _spellTargetSelector.ContainsOnlyDrawAndBloodpriceEffects(card.CardType);
            EntityManager target = GetSpellTarget(card, isUtilitySpell);

            if (target == null)
                yield break;

            // Now apply the spell effect
            ApplySpellEffect(card, target, isUtilitySpell);
        }

        public bool ContainsOnlyDrawAndBloodpriceEffects(CardData cardData)
        {
            return _spellTargetSelector.ContainsOnlyDrawAndBloodpriceEffects(cardData);
        }

        private IEnumerator WaitForFadeOutAtPosition(int position)
        {
            if (_combatStage == null || position < 0)
                yield break;

            var enemyEntities = _combatStage.SpritePositioning.EnemyEntities;
            if (position < enemyEntities.Count)
            {
                var entityAtPosition = enemyEntities[position];
                if (entityAtPosition != null)
                {
                    var entityManager = entityAtPosition.GetComponent<EntityManager>();
                    if (entityManager != null && entityManager.IsFadingOut)
                    {
                        Debug.Log($"[CardPlayExecutor] Waiting for entity at position {position} to finish fading out");

                        // Wait until IsFadingOut becomes false
                        float maxWaitTime = 7.0f; // Slightly longer than fade duration (6.5s)
                        float elapsedTime = 0f;

                        while (entityManager.IsFadingOut && elapsedTime < maxWaitTime)
                        {
                            yield return null;
                            elapsedTime += Time.deltaTime;
                        }

                        Debug.Log($"[CardPlayExecutor] Entity at position {position} finished fading out (waited {elapsedTime:F2}s)");

                        // Add a small delay after fade completes for visual clarity
                        yield return new WaitForSeconds(0.2f);
                    }
                }
            }
        }

        private IEnumerator WaitForTargetsFadeOut(CardData spellCard)
        {
            if (spellCard.EffectTypes == null || spellCard.EffectTypes.Count == 0)
                yield break;

            // For area effects and multi-target spells, we need to wait for any potential target
            // that's currently fading out
            var allPotentialTargets = new List<EntityManager>();

            foreach (var effect in spellCard.EffectTypes)
            {
                var targetsForEffect = _spellTargetSelector.GetAllValidTargets(effect);
                if (targetsForEffect != null && targetsForEffect.Count > 0)
                {
                    allPotentialTargets.AddRange(targetsForEffect);
                }
            }

            // Remove duplicates
            allPotentialTargets = allPotentialTargets.Distinct().ToList();

            // Check if any targets are fading out
            bool anyFadingOut = allPotentialTargets.Any(t => t != null && t.IsFadingOut);

            if (anyFadingOut)
            {
                Debug.Log($"[CardPlayExecutor] Waiting for targets of spell to finish fading out");

                // Wait until all relevant targets finish fading out
                float maxWaitTime = 7.0f;
                float elapsedTime = 0f;

                while (allPotentialTargets.Any(t => t != null && t.IsFadingOut) && elapsedTime < maxWaitTime)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }

                Debug.Log($"[CardPlayExecutor] All spell targets finished fading out (waited {elapsedTime:F2}s)");

                // Add a small delay after fade completes for visual clarity
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
}
