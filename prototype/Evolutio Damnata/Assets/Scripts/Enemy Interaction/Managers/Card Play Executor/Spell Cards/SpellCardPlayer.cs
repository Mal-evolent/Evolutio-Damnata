using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Models;
using EnemyInteraction.Services;
using System.Collections;
using UnityEngine;

namespace EnemyInteraction.Managers.Execution
{
    public class SpellCardPlayer
    {
        private readonly ICombatManager _combatManager;
        private readonly ISpellEffectApplier _spellEffectApplier;
        private readonly SpellTargetSelector _spellTargetSelector;
        private readonly EntityFadeWaiter _fadeWaiter;
        private readonly float _actionDelay;

        public SpellCardPlayer(
            ICombatManager combatManager,
            ISpellEffectApplier spellEffectApplier,
            SpellTargetSelector spellTargetSelector,
            EntityFadeWaiter fadeWaiter,
            float actionDelay)
        {
            _combatManager = combatManager;
            _spellEffectApplier = spellEffectApplier;
            _spellTargetSelector = spellTargetSelector;
            _fadeWaiter = fadeWaiter;
            _actionDelay = actionDelay;
        }

        public bool CanPlaySpellCard(Card card)
        {
            if (_spellEffectApplier == null)
            {
                Debug.LogError("[SpellCardPlayer] SpellEffectApplier is null");
                return false;
            }

            // Check if the card contains only Draw and/or Bloodprice effects
            if (ContainsOnlyDrawAndBloodpriceEffects(card.CardType))
            {
                // These effects don't require a target, so always return true
                return true;
            }

            // For other spell effects, find a valid target
            var target = _spellTargetSelector.GetBestSpellTarget(card.CardType);
            if (target == null)
            {
                Debug.Log($"[SpellCardPlayer] No target for spell {card.CardName}");
                return false;
            }

            // Check if this is a heal card and the target is already at full health
            if (card.CardType.EffectTypes != null &&
                card.CardType.EffectTypes.Contains(SpellEffect.Heal) &&
                Mathf.Approximately(target.GetHealth(), target.GetMaxHealth()))
            {
                Debug.Log($"[SpellCardPlayer] Target for heal spell {card.CardName} already has full health. Skipping to avoid wasting mana.");
                return false;
            }

            return true;
        }

        public IEnumerator PlaySpellCard(Card card)
        {
            // Implementation with fade check logic
            return PlaySpellCardWithFadeCheck(card);
        }

        public IEnumerator PlaySpellCardWithFadeCheck(Card card)
        {
            // Small pause before applying the effect
            yield return new WaitForSeconds(_actionDelay * 0.5f);

            // Check if it's a Draw/Bloodprice only card
            bool isUtilitySpell = ContainsOnlyDrawAndBloodpriceEffects(card.CardType);
            EntityManager target = GetSpellTarget(card, isUtilitySpell);

            if (target == null)
                yield break;

            if (!isUtilitySpell)
            {
                // Wait for any potential targets that might be fading out
                // Only do this for non-utility spells that target entities
                yield return _fadeWaiter.WaitForTargetsFadeOut(_spellTargetSelector, card.CardType);
            }

            // Now that we've waited, apply the spell effect
            ApplySpellEffect(card, target, isUtilitySpell);
        }

        public IEnumerator PlaySpellCardWithDelay(Card card)
        {
            // Small pause before applying the effect
            yield return new WaitForSeconds(_actionDelay * 0.5f);

            bool isUtilitySpell = ContainsOnlyDrawAndBloodpriceEffects(card.CardType);
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

        private EntityManager GetSpellTarget(Card card, bool isUtilitySpell)
        {
            EntityManager target;

            if (isUtilitySpell)
            {
                // For Draw/Bloodprice effects, we can use any valid entity as the target
                target = _spellTargetSelector.GetDummyTarget();
                if (target == null)
                {
                    Debug.LogError($"[SpellCardPlayer] Could not find any target for utility spell {card.CardName}");
                    return null;
                }
            }
            else
            {
                // For spells that target specific entities
                target = _spellTargetSelector.GetBestSpellTarget(card.CardType);
                if (target == null)
                {
                    Debug.LogError($"[SpellCardPlayer] Target was null for spell {card.CardName}");
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
                    Debug.Log($"[SpellCardPlayer] Casting utility spell {card.CardName}");
                }
                else
                {
                    Debug.Log($"[SpellCardPlayer] Casting {card.CardName} on {target.name}");
                }

                _spellEffectApplier.ApplySpellEffectsAI(target, card.CardType, 0);

                // Refresh entity cache after spell effect
                RefreshEntityCacheAfterAction();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SpellCardPlayer] Spell error: {e.Message}");
            }
        }

        private void RefreshEntityCacheAfterAction()
        {
            var entityCacheManager = AIServices.Instance?.EntityCacheManager as EntityCacheManager;
            entityCacheManager?.RefreshAfterAction();
        }
    }
}
