using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;
using EnemyInteraction.Utilities;

namespace EnemyInteraction.Managers.AttackStrategy.Strategies
{
    public class HealthIconTargetingStrategy
    {
        private readonly bool _avoidLosingLastMonster;
        private readonly float _lastMonsterIgnoreChance;

        public HealthIconTargetingStrategy(bool avoidLosingLastMonster, float lastMonsterIgnoreChance)
        {
            _avoidLosingLastMonster = avoidLosingLastMonster;
            _lastMonsterIgnoreChance = lastMonsterIgnoreChance;
        }

        public bool ShouldAttackHealthIcon(EntityManager attacker, List<EntityManager> playerEntities,
                                     HealthIconManager playerHealthIcon, BoardState boardState, bool isLastMonster)
        {
            if (attacker == null || playerHealthIcon == null)
                return false;

            // Occasionally make human-like mistake
            if (Random.value < _lastMonsterIgnoreChance)
            {
                Debug.Log("[HealthIconTargetingStrategy] Making human-like mistake - ignoring last monster protection");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            // Last monster protection logic
            if (_avoidLosingLastMonster && isLastMonster &&
                playerHealthIcon.GetHealth() > attacker.GetAttack() * 1.5f)
            {
                // Health icons don't counter-attack, so it's safe
                Debug.Log("[HealthIconTargetingStrategy] Safe direct health attack with our only monster - no counterattack risk");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            // Turn order considerations
            bool playerGoesFirstNextTurn = boardState != null && boardState.IsNextTurnPlayerFirst;

            // Priority for direct attacks when player health is low
            if (playerGoesFirstNextTurn && playerHealthIcon.GetHealth() <= 10)
            {
                Debug.Log("[HealthIconTargetingStrategy] Prioritizing direct health attack for potential lethal before player's turn");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            // Strategic approach when enemy goes next
            if (!playerGoesFirstNextTurn && playerHealthIcon.GetHealth() <= attacker.GetAttack() * 1.5f)
            {
                Debug.Log("[HealthIconTargetingStrategy] Strategic direct health attack to set up lethal on our next turn");
                return AIUtilities.CanTargetHealthIcon(playerEntities);
            }

            return AIUtilities.CanTargetHealthIcon(playerEntities);
        }
    }
}
