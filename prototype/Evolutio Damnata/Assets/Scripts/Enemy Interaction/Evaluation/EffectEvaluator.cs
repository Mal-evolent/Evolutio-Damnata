using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Extensions;
using EnemyInteraction.Utilities;
using System;


namespace EnemyInteraction.Evaluation
{
    public class EffectEvaluator : MonoBehaviour, IEffectEvaluator
    {
        private Dictionary<SpellEffect, SpellEffectEvaluation> _effectEvaluations;

        private void Awake()
        {
            InitializeEvaluations();
        }

        private void InitializeEvaluations()
        {
            _effectEvaluations = new Dictionary<SpellEffect, SpellEffectEvaluation>
                {
                    {
                        SpellEffect.Damage,
                        new SpellEffectEvaluation
                        {
                            BaseScore = 35f,
                            IsPositive = false,
                            IsStackable = false,
                            RequiresTarget = true,
                            IsDamaging = true
                        }
                    },
                    {
                        SpellEffect.Burn,
                        new SpellEffectEvaluation
                        {
                            BaseScore = 30f,
                            IsPositive = false,
                            IsStackable = true,
                            RequiresTarget = true,
                            IsDamaging = true
                        }
                    },
                    {
                        SpellEffect.Heal,
                        new SpellEffectEvaluation
                        {
                            BaseScore = 25f,
                            IsPositive = true,
                            IsStackable = false,
                            RequiresTarget = true,
                            IsDamaging = false
                        }
                    },
                    {
                        SpellEffect.Draw,
                        new SpellEffectEvaluation
                        {
                            BaseScore = 40f,
                            IsPositive = true,
                            IsStackable = false,
                            RequiresTarget = false,
                            IsDamaging = false
                        }
                    },
                    {
                        SpellEffect.Bloodprice,
                        new SpellEffectEvaluation
                        {
                            BaseScore = -15f,
                            IsPositive = false,
                            IsStackable = false,
                            RequiresTarget = false,
                            IsDamaging = true
                        }
                    }
                };
        }

        public float EvaluateEffect(SpellEffect effect, bool isOwnCard, EntityManager target, BoardState boardState)
        {
            return EvaluateEffect(effect, isOwnCard, target, boardState, null);
        }

        public float EvaluateEffect(SpellEffect effect, bool isOwnCard, EntityManager target, BoardState boardState, CardData cardData)
        {
            if (!_effectEvaluations.ContainsKey(effect))
            {
                Debug.LogWarning($"Unknown effect: {effect}. Add it to _effectEvaluations for proper AI handling.");
                return 0f;
            }

            var evaluation = _effectEvaluations[effect];
            float score = evaluation.BaseScore;
            float additionalScore = 0f; // Using additive bonus instead of multiplicative

            if (isOwnCard)
            {
                // Stackable effects bonus (reduced from 1.4x multiplier)
                if (evaluation.IsStackable && HasOngoingEffect(target, effect))
                {
                    additionalScore += score * 0.25f;
                }

                if (evaluation.IsDamaging)
                {
                    // Check if target is a health icon
                    if (target is HealthIconManager healthIcon)
                    {
                        // Player health icon - prioritize if player health is low
                        if (healthIcon.IsPlayerIcon)
                        {
                            float healthRatio = healthIcon.CurrentHealth / healthIcon.MaxHealth;

                            // Cap the health-based bonus to avoid excessive prioritization
                            if (healthRatio <= 0.3f)
                                additionalScore += score * 0.6f; // Reduced from 2.5x
                            else if (healthRatio <= 0.5f)
                                additionalScore += score * 0.4f; // Reduced from 1.8x

                            // Late game bonus (capped)
                            if (boardState.TurnCount >= 10)
                                additionalScore += score * 0.3f; // Reduced from 1.5x
                        }
                        // Own (enemy) health icon - don't target with damage
                        else
                        {
                            score = 0f;
                        }
                    }
                    else if (target != null)
                    {
                        // Normal entity damage evaluation
                        float damageRatio = target.GetHealth() / target.GetMaxHealth();
                        if (damageRatio < 0.5f)
                            additionalScore += score * 0.2f; // Reduced from 1.3x
                    }
                }

                // Non-damaging effects are slightly more valuable when at health disadvantage
                if (boardState.HealthAdvantage < 0 && !evaluation.IsDamaging)
                {
                    additionalScore += score * 0.1f; // Reduced from 1.2x
                }

                // Special handling for Draw effect
                if (effect == SpellEffect.Draw)
                {
                    float drawBonus = 0f;
                    int handSize = boardState.EnemyHandSize;

                    // Hand size considerations - use additive bonuses instead of multipliers
                    if (handSize <= 1)
                        drawBonus += score * 0.5f; // Reduced from 2.0x
                    else if (handSize <= 2)
                        drawBonus += score * 0.3f; // Reduced from 1.5x

                    // Early game bonus (capped)
                    if (boardState.TurnCount < 3)
                        drawBonus += score * 0.2f; // Reduced from 1.3x

                    // Card advantage bonus (normalized)
                    if (boardState.CardAdvantage < 0)
                    {
                        float cardAdvantageBonus = Mathf.Min(Mathf.Abs(boardState.CardAdvantage) * 5f, 20f);
                        drawBonus += cardAdvantageBonus;
                    }

                    // Mana consideration (reduced)
                    if (boardState.EnemyMana >= 3)
                        drawBonus += 10f; // Fixed bonus instead of multiplier

                    // Board control bonus (reduced)
                    if (boardState.BoardControlDifference < 0)
                        drawBonus += 10f; // Fixed bonus instead of multiplier

                    // Fix hand overflow calculation
                    if (cardData != null && cardData.DrawValue > 0)
                    {
                        // Fix the bug in maxHandSize calculation
                        int maxHandSize = 10; // Typical card game max hand size
                        int availableSlots = maxHandSize - boardState.EnemyHandSize;

                        // Severe penalty for overdrawing
                        if (cardData.DrawValue > availableSlots)
                        {
                            // Apply a severe, additive penalty instead of a multiplier
                            drawBonus -= (cardData.DrawValue - availableSlots) * 20f;
                            Debug.Log($"[EffectEvaluator] Applied draw overflow penalty: {(cardData.DrawValue - availableSlots) * -20f} for drawing {cardData.DrawValue} cards with only {availableSlots} slots");
                        }
                    }

                    // Add the capped draw bonus to score
                    additionalScore += Mathf.Min(drawBonus, score * 0.8f);

                    Debug.Log($"[EffectEvaluator] Draw effect: base={score}, bonus={additionalScore} (hand size: {handSize}, card advantage: {boardState.CardAdvantage})");
                }
                // Special handling for Blood Price effect
                else if (effect == SpellEffect.Bloodprice)
                {
                    float healthPercentage = boardState.EnemyHealth / boardState.EnemyMaxHealth;
                    float bloodPriceModifier = 1.0f;

                    // Adjust the penalty based on health
                    if (healthPercentage > 0.7f)
                        bloodPriceModifier = 0.7f;  // Less negative when health is high
                    else if (healthPercentage > 0.4f)
                        bloodPriceModifier = 0.9f;  // Slightly less negative
                    else if (healthPercentage < 0.2f)
                        bloodPriceModifier = 1.5f;  // More negative when at low health

                    // Health advantage consideration
                    if (boardState.HealthAdvantage > 10)
                        bloodPriceModifier -= 0.2f;  // Less negative with health advantage

                    // Late game consideration
                    if (boardState.TurnCount > 8)
                        bloodPriceModifier += 0.1f;  // Slightly more negative in late game

                    // Apply the modifier to score directly, not as a multiplier
                    score *= bloodPriceModifier;

                    Debug.Log($"[EffectEvaluator] Blood Price score adjusted by {bloodPriceModifier} based on health and game state");
                }

                // Add the additional score instead of using multipliers
                score += additionalScore;
            }
            else
            {
                // Evaluating opponent's effects
                score *= evaluation.IsPositive ? -1 : 1;
            }

            // Apply a maximum cap to prevent extreme values
            float scoreCap = evaluation.BaseScore * 3.0f;
            return Mathf.Clamp(score, -scoreCap, scoreCap);
        }

        public EntityManager GetBestTargetForEffect(SpellEffect effect, bool isOwnCard, BoardState boardState)
        {
            if (!_effectEvaluations.ContainsKey(effect))
            {
                Debug.LogWarning($"[EffectEvaluator] Unknown effect: {effect}. Cannot find target.");
                return null;
            }

            if (boardState == null)
            {
                Debug.LogError("[EffectEvaluator] Board state is null in GetBestTargetForEffect!");
                return null;
            }

            var evaluation = _effectEvaluations[effect];
            bool isDamagingEffect = evaluation.IsDamaging;

            EntityManager selectedTarget = null;
            List<EntityManager> potentialTargets;

            if (evaluation.IsPositive)
            {
                // For positive effects (like healing), target enemy monsters
                potentialTargets = boardState.EnemyMonsters?
                    .Where(m => m != null && !m.dead && m.placed)
                    .ToList() ?? new List<EntityManager>();

                // If no valid enemy monsters and we can target health icon, add enemy health icon
                if (potentialTargets.Count == 0 && !isDamagingEffect)
                {
                    var enemyHealthIcon = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
                    if (enemyHealthIcon != null && !enemyHealthIcon.IsPlayerIcon)
                    {
                        potentialTargets.Add(enemyHealthIcon);
                    }
                }
            }
            else
            {
                // For negative effects (like damage), target player monsters
                potentialTargets = boardState.PlayerMonsters?
                    .Where(m => m != null && !m.dead && m.placed)
                    .ToList() ?? new List<EntityManager>();

                // If no valid player monsters and this is a damaging effect, try to target player health icon
                if (potentialTargets.Count == 0 && isDamagingEffect)
                {
                    var playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
                    if (playerHealthIcon != null && playerHealthIcon.IsPlayerIcon)
                    {
                        potentialTargets.Add(playerHealthIcon);
                    }
                }
            }

            // Filter targets using AIUtilities
            var validTargets = potentialTargets
                .Where(target => AIUtilities.IsValidTargetForEffect(target, effect, isDamagingEffect))
                .ToList();

            if (validTargets.Count > 0)
            {
                if (isDamagingEffect)
                {
                    // For damaging effects, target highest threat
                    selectedTarget = validTargets.OrderByDescending(m => EvaluateTargetThreat(m, boardState)).FirstOrDefault();
                }
                else
                {
                    // For healing effects, target lowest health %
                    selectedTarget = validTargets.OrderBy(m => m.GetHealth() / m.GetMaxHealth()).FirstOrDefault();
                }
            }

            if (selectedTarget != null)
            {
                Debug.Log($"[EffectEvaluator] Selected target for {effect}: {selectedTarget.name} (placed: {selectedTarget.placed})");

                // If targeting a health icon, add additional logging
                if (selectedTarget is HealthIconManager healthIcon)
                {
                    Debug.Log($"[EffectEvaluator] Selected health icon target: {(healthIcon.IsPlayerIcon ? "Player" : "Enemy")} icon with {healthIcon.CurrentHealth}/{healthIcon.MaxHealth} health");
                }
            }
            else
            {
                Debug.LogWarning($"[EffectEvaluator] Could not find valid target for {effect}");
            }

            return selectedTarget;
        }

        private float EvaluateTargetThreat(EntityManager target, BoardState boardState)
        {
            float threat = 0f;

            threat += target.GetAttackPower() * 1.2f;
            threat += target.GetHealth() * 0.8f;

            return threat;
        }

        private bool HasOngoingEffect(EntityManager target, SpellEffect effect)
        {
            return StackManager.Instance?.HasEffect(target, effect) ?? false;
        }

        public void AddEffectEvaluation(SpellEffect effect, SpellEffectEvaluation evaluation)
        {
            _effectEvaluations[effect] = evaluation;
        }
    }

    public class SpellEffectEvaluation
    {
        public float BaseScore { get; set; }
        public bool IsPositive { get; set; }
        public bool IsStackable { get; set; }
        public bool RequiresTarget { get; set; }
        public bool IsDamaging { get; set; }
    }
} 