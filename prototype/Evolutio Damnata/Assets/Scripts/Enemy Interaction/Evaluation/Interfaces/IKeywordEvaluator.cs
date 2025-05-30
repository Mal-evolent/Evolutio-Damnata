using EnemyInteraction.Models;

namespace EnemyInteraction.Evaluation
{
    public interface IKeywordEvaluator
    {
        float EvaluateKeyword(Keywords.MonsterKeyword keyword, bool isOwnCard, BoardState boardState);
        float EvaluateKeywords(EntityManager attacker, EntityManager target);
        float EvaluateKeywords(EntityManager attacker, EntityManager target, BoardState boardState);
        void AddKeywordEvaluation(Keywords.MonsterKeyword keyword, KeywordEvaluation evaluation);
    }
} 