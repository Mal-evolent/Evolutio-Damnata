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

                // Special handling for Heal effect - including overheal penalty
                if (effect == SpellEffect.Heal && target != null)
                {
                    // Get target's current health information
                    float currentHealth = target.GetHealth();
                    float maxHealth = target.GetMaxHealth();
                    float missingHealth = maxHealth - currentHealth;
                    float healthPercentage = currentHealth / maxHealth;

                    // Calculate heal effectiveness
                    float healAmount = cardData != null ? cardData.EffectValue : 0;
                    float overHealAmount = Mathf.Max(0, healAmount - missingHealth);
                    float healEfficiency = missingHealth > 0 ? Mathf.Min(1.0f, missingHealth / healAmount) : 0;

                    // Apply bonuses for low health targets
                    if (healthPercentage <= 0.3f)
                        additionalScore += score * 0.5f;
                    else if (healthPercentage <= 0.5f)
                        additionalScore += score * 0.3f;

                    // Apply penalty for overhealing
                    if (overHealAmount > 0 && healAmount > 0)
                    {
                        float overHealPercentage = overHealAmount / healAmount;
                        float wastePenalty = score * overHealPercentage * 0.8f;

                        additionalScore -= wastePenalty;
                        Debug.Log($"[EffectEvaluator] Overheal penalty: -{wastePenalty:F1} for wasting {overHealPercentage:P0} of heal on {target.name} ({currentHealth}/{maxHealth})");
                    }

                    // If completely or nearly full health, severely penalize heal
                    if (healthPercentage > 0.95f)
                    {
                        additionalScore -= score * 0.75f;
                        Debug.Log($"[EffectEvaluator] Target {target.name} is nearly at full health ({currentHealth}/{maxHealth}), significantly reducing heal value");
                    }

                    // Bonus if health is critical
                    if (healthPercentage <= 0.2f && healEfficiency >= 0.8f)
                    {
                        additionalScore += score * 0.4f;
                        Debug.Log($"[EffectEvaluator] Critical heal bonus for {target.name} with {currentHealth}/{maxHealth} health");
                    }

                    // Log heal evaluation
                    Debug.Log($"[EffectEvaluator] Heal effect on {target.name}: base={score}, " +
                              $"health={currentHealth}/{maxHealth} ({healthPercentage:P0}), " +
                              $"efficiency={healEfficiency:P0}, " +
                              $"final adjustment={additionalScore:F1}");
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
                else if (effect == SpellEffect.Heal)
                {
                    // For healing effects, use improved targeting that considers heal efficiency
                    selectedTarget = GetOptimalHealTarget(validTargets);
                }
                else
                {
                    // Default to lowest health % for other positive effects
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

        /// <summary>
        /// Selects the optimal healing target by considering both missing health and efficiency
        /// </summary>
        private EntityManager GetOptimalHealTarget(List<EntityManager> targets)
        {
            if (targets == null || targets.Count == 0)
                return null;

            // Each target gets a heal score that combines:
            // 1. How low their health % is (prioritize low health)
            // 2. How much healing they can receive without overhealing
            var scoredTargets = targets.Select(target =>
            {
                float currentHealth = target.GetHealth();
                float maxHealth = target.GetMaxHealth();
                float missingHealth = maxHealth - currentHealth;
                float healthPercentage = currentHealth / maxHealth;

                // Calculate base score from health percentage (lower = better)
                float baseScore = 1.0f - healthPercentage;

                // If nearly full health, severely reduce score
                if (healthPercentage > 0.9f)
                {
                    baseScore *= 0.2f;
                }

                // If critically low, boost score
                if (healthPercentage < 0.3f)
                {
                    baseScore *= 2.0f;
                }

                // Missing health factor - bigger gaps are better for healing
                float missingHealthFactor = missingHealth / maxHealth;

                // Combine factors
                float finalScore = (baseScore * 0.7f) + (missingHealthFactor * 0.3f);

                Debug.Log($"[EffectEvaluator] Heal target score for {target.name}: {finalScore:F2} " +
                          $"(health: {currentHealth}/{maxHealth}, {healthPercentage:P0})");

                return new { Target = target, Score = finalScore };
            })
            .OrderByDescending(x => x.Score)
            .ToList();

            // Get the best target
            var bestTarget = scoredTargets.FirstOrDefault()?.Target;

            if (bestTarget != null)
            {
                float healthPercentage = bestTarget.GetHealth() / bestTarget.GetMaxHealth();
                Debug.Log($"[EffectEvaluator] Selected optimal heal target: {bestTarget.name} " +
                          $"with {healthPercentage:P0} health ({bestTarget.GetHealth()}/{bestTarget.GetMaxHealth()})");
            }

            return bestTarget;
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