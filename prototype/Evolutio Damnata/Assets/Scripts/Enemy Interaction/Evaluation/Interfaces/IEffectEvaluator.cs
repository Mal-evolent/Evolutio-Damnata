using EnemyInteraction.Models;
using UnityEngine;

namespace EnemyInteraction.Evaluation
{
    public interface IEffectEvaluator
    {
        float EvaluateEffect(SpellEffect effect, bool isOwnCard, EntityManager target, BoardState boardState);
        EntityManager GetBestTargetForEffect(SpellEffect effect, bool isOwnCard, BoardState boardState);
        void AddEffectEvaluation(SpellEffect effect, SpellEffectEvaluation evaluation);
    }
} 