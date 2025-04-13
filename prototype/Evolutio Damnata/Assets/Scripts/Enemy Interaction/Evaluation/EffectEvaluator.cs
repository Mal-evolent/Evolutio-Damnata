using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Extensions;

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
                }
            };
        }

        public float EvaluateEffect(SpellEffect effect, bool isOwnCard, EntityManager target, BoardState boardState)
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
            
            EntityManager selectedTarget = null;
            
            if (evaluation.IsPositive)
            {
                // For positive effects (like healing), target enemy monsters or enemy health icon
                if (boardState.EnemyMonsters != null && boardState.EnemyMonsters.Count > 0)
                {
                    selectedTarget = boardState.EnemyMonsters
                        .Where(m => m != null && !m.dead && m.placed)
                        .OrderBy(m => m.GetHealth() / m.GetMaxHealth())
                        .FirstOrDefault();
                }
                
                // If no valid enemy monsters, try to target enemy health icon for healing
                if (selectedTarget == null && evaluation.IsDamaging == false)
                {
                    var enemyHealthIcon = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
                    if (enemyHealthIcon != null)
                    {
                        Debug.Log("[EffectEvaluator] No enemy monsters available, targeting enemy health icon with positive effect");
                        selectedTarget = enemyHealthIcon;
                    }
                }
            }
            else
            {
                // For negative effects (like damage), target player monsters or player health icon
                bool playerMonstersExist = boardState.PlayerMonsters != null && 
                                         boardState.PlayerMonsters.Count > 0 &&
                                         boardState.PlayerMonsters.Any(m => m != null && !m.dead && m.placed);
                
                if (playerMonstersExist)
                {
                    selectedTarget = boardState.PlayerMonsters
                        .Where(m => m != null && !m.dead && m.placed)
                        .OrderByDescending(m => EvaluateTargetThreat(m, boardState))
                        .FirstOrDefault();
                    
                    // Never target player health icon when monsters are on the field
                    // selectedTarget will be a monster entity at this point
                }
                else
                {
                    // Only if no valid player monsters exist, try to target player health icon with damaging spells
                    if (evaluation.IsDamaging)
                    {
                        var playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
                        if (playerHealthIcon != null)
                        {
                            Debug.Log("[EffectEvaluator] No player monsters available, targeting player health icon with damaging effect");
                            selectedTarget = playerHealthIcon;
                        }
                    }
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