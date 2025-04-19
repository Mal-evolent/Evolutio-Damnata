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

            if (isOwnCard)
            {
                if (evaluation.IsStackable && HasOngoingEffect(target, effect))
                {
                    score *= 1.4f;
                }

                if (evaluation.IsDamaging)
                {
                    // Check if target is a health icon
                    if (target is HealthIconManager healthIcon)
                    {
                        // Player health icon - prioritize if player health is low
                        if (healthIcon.IsPlayerIcon)
                        {
                            // Significantly higher score for targeting player health icon when it's below 30%
                            float healthRatio = healthIcon.CurrentHealth / healthIcon.MaxHealth;
                            if (healthRatio <= 0.3f)
                                score *= 2.5f;
                            else if (healthRatio <= 0.5f)
                                score *= 1.8f;

                            // Extra bonus in late game (turn 10+)
                            if (boardState.TurnCount >= 10)
                                score *= 1.5f;
                        }
                        // Own (enemy) health icon - don't target with damage
                        else
                        {
                            score = 0f; // Don't damage our own health icon
                        }
                    }
                    else
                    {
                        // Normal entity damage evaluation
                        float damageRatio = target.GetHealth() / target.GetMaxHealth();
                        if (damageRatio < 0.5f) score *= 1.3f;
                    }
                }

                if (boardState.HealthAdvantage < 0)
                {
                    if (!evaluation.IsDamaging) score *= 1.2f;
                }

                // Special handling for Draw effect
                if (effect == SpellEffect.Draw)
                {
                    // Higher value when hand is nearly empty
                    int handSize = boardState.EnemyHandSize;
                    if (handSize <= 1)
                        score *= 2.0f;
                    else if (handSize <= 2)
                        score *= 1.5f;

                    // Lower value when hand is already full
                    if (handSize >= 7)
                        score *= 0.5f;

                    // Higher value in early game to build card advantage
                    if (boardState.TurnCount < 3)
                        score *= 1.3f;

                    // Consider card advantage - more valuable when behind on cards
                    if (boardState.CardAdvantage < 0)
                        score *= 1.0f + Math.Min(Math.Abs(boardState.CardAdvantage) * 0.1f, 0.5f);

                    // Consider mana availability - more valuable if we have mana to play drawn cards
                    if (boardState.EnemyMana >= 3)
                        score *= 1.2f;

                    // Consider board state - more valuable when behind on board
                    if (boardState.BoardControlDifference < 0)
                        score *= 1.2f;

                    // NEW: Check if we would overflow our hand with this card
                    if (cardData != null && cardData.DrawValue > 0)
                    {
                        // Get available hand slots
                        int maxHandSize = boardState.EnemyHandSize > 0 ? boardState.EnemyHandSize : boardState.EnemyHandSize;
                        int availableSlots = maxHandSize - boardState.EnemyHandSize;

                        // If draw value exceeds available slots, apply a severe penalty
                        if (cardData.DrawValue > availableSlots)
                        {
                            score *= 0.2f; // Severe penalty
                            Debug.Log($"[EffectEvaluator] Applied severe penalty to Draw card that would draw {cardData.DrawValue} cards with only {availableSlots} slots available");
                        }
                    }

                    Debug.Log($"[EffectEvaluator] Draw effect score: {score} (hand size: {handSize}, card advantage: {boardState.CardAdvantage}, turn: {boardState.TurnCount})");
                }

                // Special handling for Blood Price effect
                else if (effect == SpellEffect.Bloodprice)
                {
                    // Blood Price is usually part of a card with other positive effects
                    // So we need to evaluate it in context of enemy's health

                    // Value Blood Price less negatively when enemy has high health
                    float healthPercentage = boardState.EnemyHealth / boardState.EnemyMaxHealth;

                    // If enemy health is high, the penalty is reduced
                    if (healthPercentage > 0.7f)
                        score *= 0.5f;  // Less negative (only half as bad)
                    else if (healthPercentage > 0.4f)
                        score *= 0.8f;  // Somewhat less negative
                    else if (healthPercentage < 0.2f)
                        score *= 2.0f;  // Much more negative when at low health

                    // Consider the game state - Blood Price is more acceptable when winning
                    if (boardState.HealthAdvantage > 10)
                        score *= 0.7f;  // Less negative if we have health advantage

                    // Blood Price is more risky in late game
                    if (boardState.TurnCount > 8)
                        score *= 1.2f;  // More negative in late game

                    Debug.Log($"[EffectEvaluator] Blood Price effect score: {score} (health: {boardState.EnemyHealth}, health advantage: {boardState.HealthAdvantage})");
                }
            }
            else
            {
                score *= evaluation.IsPositive ? -1 : 1;
            }

            return score;
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