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
                    float damageRatio = target.GetHealth() / target.GetMaxHealth();
                    if (damageRatio < 0.5f) score *= 1.3f;
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
                return null;

            var evaluation = _effectEvaluations[effect];
            
            if (evaluation.IsPositive)
            {
                return boardState.EnemyMonsters
                    .Where(m => m != null && !m.dead)
                    .OrderBy(m => m.GetHealth() / m.GetMaxHealth())
                    .FirstOrDefault();
            }
            else
            {
                return boardState.PlayerMonsters
                    .Where(m => m != null && !m.dead)
                    .OrderByDescending(m => EvaluateTargetThreat(m, boardState))
                    .FirstOrDefault();
            }
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