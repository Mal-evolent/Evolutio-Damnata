using UnityEngine;
using System.Collections.Generic;
using EnemyInteraction.Models;

namespace EnemyInteraction.Evaluation
{
    public class KeywordEvaluator : MonoBehaviour, IKeywordEvaluator
    {
        private Dictionary<Keywords.MonsterKeyword, KeywordEvaluation> _keywordEvaluations;

        public KeywordEvaluator()
        {
            InitializeEvaluations();
        }

        private void InitializeEvaluations()
        {
            _keywordEvaluations = new Dictionary<Keywords.MonsterKeyword, KeywordEvaluation>
            {
                {
                    Keywords.MonsterKeyword.Taunt,
                    new KeywordEvaluation
                    {
                        BaseScore = 40f,
                        IsPositive = true,
                        IsDefensive = true,
                        IsOffensive = false,
                        RequiresTarget = false
                    }
                },
                {
                    Keywords.MonsterKeyword.Ranged,
                    new KeywordEvaluation
                    {
                        BaseScore = 30f,
                        IsPositive = true,
                        IsDefensive = false,
                        IsOffensive = true,
                        RequiresTarget = false
                    }
                },
                {
                    Keywords.MonsterKeyword.Tough,
                    new KeywordEvaluation
                    {
                        BaseScore = 35f,
                        IsPositive = true,
                        IsDefensive = true,
                        IsOffensive = false,
                        RequiresTarget = false
                    }
                },
                {
                    Keywords.MonsterKeyword.Overwhelm,
                    new KeywordEvaluation
                    {
                        BaseScore = 45f,
                        IsPositive = true,
                        IsDefensive = false,
                        IsOffensive = true,
                        RequiresTarget = true
                    }
                }
            };
        }

        public float EvaluateKeyword(Keywords.MonsterKeyword keyword, bool isOwnCard, BoardState boardState)
        {
            if (!_keywordEvaluations.ContainsKey(keyword))
            {
                Debug.LogWarning($"Unknown keyword: {keyword}. Add it to _keywordEvaluations for proper AI handling.");
                return 0f;
            }

            var evaluation = _keywordEvaluations[keyword];
            float score = evaluation.BaseScore;
            float bonus = 0f;

            if (isOwnCard)
            {
                if (boardState != null && boardState.HealthAdvantage < 0)
                {
                    // Use additive bonuses instead of multipliers
                    if (evaluation.IsDefensive)
                        bonus += score * 0.3f; // Reduced from 1.5x
                    if (evaluation.IsOffensive)
                        bonus -= score * 0.1f; // Less penalty than before
                }
                else
                {
                    // Use additive bonuses for offensive keywords
                    if (evaluation.IsOffensive)
                        bonus += score * 0.2f; // Reduced from 1.3x
                }

                // Apply the bonus to the score
                score += bonus;
            }
            else
            {
                // Evaluating opponent's keywords
                score *= evaluation.IsPositive ? -1 : 1;
            }

            // Add a cap to prevent extreme values
            return Mathf.Clamp(score, -evaluation.BaseScore * 2f, evaluation.BaseScore * 2f);
        }

        public float EvaluateKeywords(EntityManager attacker, EntityManager target)
        {
            if (attacker == null || target == null) return 0f;

            float score = 0f;

            // Evaluate attacker's keywords
            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (attacker.HasKeyword(keyword))
                {
                    score += EvaluateKeyword(keyword, true, null);
                }
            }

            // Evaluate target's keywords
            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (target.HasKeyword(keyword))
                {
                    score -= EvaluateKeyword(keyword, false, null);
                }
            }

            return score;
        }

        public float EvaluateKeywords(EntityManager attacker, EntityManager target, BoardState boardState)
        {
            if (attacker == null || target == null) return 0f;

            float score = 0f;

            // Evaluate attacker's keywords
            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (attacker.HasKeyword(keyword))
                {
                    score += EvaluateKeyword(keyword, true, boardState);
                }
            }

            // Evaluate target's keywords
            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (target.HasKeyword(keyword))
                {
                    score -= EvaluateKeyword(keyword, false, boardState);
                }
            }

            return score;
        }

        public void AddKeywordEvaluation(Keywords.MonsterKeyword keyword, KeywordEvaluation evaluation)
        {
            _keywordEvaluations[keyword] = evaluation;
        }
    }

    public class KeywordEvaluation
    {
        public float BaseScore { get; set; }
        public bool IsPositive { get; set; }
        public bool IsDefensive { get; set; }
        public bool IsOffensive { get; set; }
        public bool RequiresTarget { get; set; }
    }
} 