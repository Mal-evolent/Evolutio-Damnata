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

            if (isOwnCard)
            {
                if (boardState.HealthAdvantage < 0)
                {
                    if (evaluation.IsDefensive) score *= 1.5f;
                    if (evaluation.IsOffensive) score *= 0.8f;
                }
                else
                {
                    if (evaluation.IsOffensive) score *= 1.3f;
                }
            }
            else
            {
                score *= evaluation.IsPositive ? -1 : 1;
            }

            return score;
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