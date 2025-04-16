using EnemyInteraction.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyInteraction.Managers.Targeting
{
    public class MonsterPositionSelector
    {
        private readonly SpritePositioning _spritePositioning;
        private readonly Dictionary<GameObject, EntityManager> _entityCache;

        public MonsterPositionSelector(SpritePositioning spritePositioning, Dictionary<GameObject, EntityManager> entityCache)
        {
            _spritePositioning = spritePositioning;
            _entityCache = entityCache;
        }

        public int FindOptimalMonsterPosition(Card card, BoardState boardState)
        {
            var availablePositions = GetAvailableMonsterPositions();
            if (availablePositions.Count == 0) return -1;
            if (availablePositions.Count == 1) return availablePositions[0];

            bool hasRanged = card.CardType.HasKeyword(Keywords.MonsterKeyword.Ranged);
            bool hasTaunt = card.CardType.HasKeyword(Keywords.MonsterKeyword.Taunt);

            return availablePositions
                .OrderByDescending(pos => CalculatePositionScore(pos, card, hasRanged, hasTaunt))
                .First();
        }

        public List<int> GetAvailableMonsterPositions()
        {
            var positions = new List<int>();
            if (_spritePositioning == null) return positions;

            for (int i = 0; i < _spritePositioning.EnemyEntities.Count; i++)
            {
                if (_spritePositioning.EnemyEntities[i] == null) continue;

                if (!_entityCache.TryGetValue(_spritePositioning.EnemyEntities[i], out var entity)) continue;

                if (entity != null && !entity.placed)
                {
                    positions.Add(i);
                }
            }
            return positions;
        }

        public float CalculatePositionScore(int position, Card card, bool hasRanged, bool hasTaunt)
        {
            float score = 0;
            int middlePos = _spritePositioning.EnemyEntities.Count / 2;
            float midDist = Mathf.Abs(position - middlePos);

            // Base position value (prefer center)
            score += (1 - midDist / middlePos) * 10f;

            // Strategic modifiers
            if (hasRanged)
            {
                score += position * 5f; // Prefer backline
            }
            else if (hasTaunt)
            {
                score += (_spritePositioning.EnemyEntities.Count - position) * 5f;
            }
            else if (card.CardType.Health >= 5)
            {
                score += (_spritePositioning.EnemyEntities.Count - position) * 3f;
            }
            else if (card.CardType.AttackPower >= 5)
            {
                float midValue = 1 - midDist / (_spritePositioning.EnemyEntities.Count / 2f);
                score += midValue * 15f;
            }

            // New position logic for Tough and Overwhelm
            bool hasTough = card.CardType.HasKeyword(Keywords.MonsterKeyword.Tough);
            bool hasOverwhelm = card.CardType.HasKeyword(Keywords.MonsterKeyword.Overwhelm);

            if (hasTough)
            {
                // Tough units are good on the front line to block incoming damage
                score += (_spritePositioning.EnemyEntities.Count - position) * 4f;
            }

            if (hasOverwhelm)
            {
                // Overwhelm units are best in positions where they can attack
                // Prefer middle to front positions for these
                if (position < _spritePositioning.EnemyEntities.Count / 2)
                {
                    score += (_spritePositioning.EnemyEntities.Count / 2 - position) * 3f;
                }
            }

            return score;
        }
    }
}
