using EnemyInteraction.Models;
using System.Collections.Generic;

namespace EnemyInteraction.Managers.Targeting
{
    public interface ITargetSelector
    {
        int FindOptimalMonsterPosition(Card card, BoardState boardState);
        List<int> GetAvailableMonsterPositions();
        float CalculatePositionScore(int position, Card card, bool hasRanged, bool hasTaunt);
        EntityManager GetBestSpellTarget(CardData cardType);
        EntityManager GetDummyTarget();
        List<EntityManager> GetAllValidTargets(SpellEffect effect);
        float CalculateThreatScore(EntityManager target, CardData cardType);
    }
}
