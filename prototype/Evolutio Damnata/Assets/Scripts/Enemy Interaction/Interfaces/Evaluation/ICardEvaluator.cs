using System.Collections.Generic;
using EnemyInteraction.Models;

namespace EnemyInteraction.Managers.Evaluation
{
    public interface ICardEvaluator
    {
        List<Card> DetermineCardPlayOrder(List<Card> playableCards, BoardState boardState);
        float EvaluateCardPlay(Card card, BoardState boardState);
        float ApplyDecisionVariance(float baseScore);
        List<Card> GetPlayableCards(IEnumerable<Card> hand);
        bool IsCardPlayableInCurrentPhase(Card card);
    }
}
