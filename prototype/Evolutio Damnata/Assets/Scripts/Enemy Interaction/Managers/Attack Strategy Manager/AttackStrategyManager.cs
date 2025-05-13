using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Models;
using EnemyInteraction.Utilities;
using EnemyInteraction.Managers.AttackStrategy.Evaluators;
using EnemyInteraction.Managers.AttackStrategy.Strategies;

namespace EnemyInteraction.Managers
{
    public class AttackStrategyManager : MonoBehaviour, IAttackStrategyManager
    {
        #region Configuration
        [SerializeField, Range(0f, 1f), Tooltip("Chance to make suboptimal decisions")]
        private float _decisionVariance = 0.10f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Chance to randomize attack order")]
        private float _attackOrderRandomizationChance = 0.10f;

        [SerializeField, Tooltip("Health difference to switch strategies")]
        private float _healthThresholdForAggro = 8f;

        [SerializeField, Tooltip("Turn count to become more aggressive")]
        private int _aggressiveTurnThreshold = 4;

        [SerializeField, Tooltip("Whether to avoid losing the last monster")]
        private bool _avoidLosingLastMonster = true;

        [SerializeField, Range(0f, 0.3f), Tooltip("Chance to ignore last monster protection")]
        private float _lastMonsterIgnoreChance = 0.15f;

        [SerializeField, Range(1f, 10f), Tooltip("Min value ratio for beneficial last monster trade")]
        private float _valuableTradeRatio = 2.0f;
        #endregion

        private ITargetEvaluator _targetEvaluator;
        private IEntityCacheManager _entityCacheManager;
        private AttackOrderStrategy _attackOrderStrategy;
        private TargetSelectionStrategy _targetSelectionStrategy;
        private HealthIconTargetingStrategy _healthIconTargetingStrategy;
        private TradeEvaluator _tradeEvaluator;

        public void Initialize(ITargetEvaluator targetEvaluator, IEntityCacheManager entityCacheManager)
        {
            _targetEvaluator = targetEvaluator;
            _entityCacheManager = entityCacheManager;

            // Initialize strategy components
            _attackOrderStrategy = new AttackOrderStrategy(_decisionVariance, _attackOrderRandomizationChance);
            _targetSelectionStrategy = new TargetSelectionStrategy(_targetEvaluator, _decisionVariance);
            _healthIconTargetingStrategy = new HealthIconTargetingStrategy(
                _avoidLosingLastMonster, _lastMonsterIgnoreChance);
            _tradeEvaluator = new TradeEvaluator(_valuableTradeRatio);
        }

        #region Public Interface Methods
        public List<EntityManager> GetAttackOrder(List<EntityManager> enemies, List<EntityManager> players,
                                             HealthIconManager healthIcon, BoardState boardState)
        {
            // Filter out valid attackers (attack > 0)
            var validAttackers = enemies.Where(e => e != null && e.GetAttack() > 0).ToList();
            LogDefensiveUnits(enemies.Where(e => e != null && e.GetAttack() == 0).ToList());

            if (validAttackers.Count == 0)
            {
                Debug.Log("[AttackStrategyManager] No valid attackers with attack > 0 available");
                return new List<EntityManager>();
            }

            bool isLethalPossible = CanKillPlayerThisTurn(validAttackers, players, healthIcon);

            return _attackOrderStrategy.DetermineAttackOrder(
                validAttackers,
                players,
                boardState,
                isLethalPossible,
                IsLastMonster);
        }

        public EntityManager SelectTarget(EntityManager attacker, List<EntityManager> playerEntities,
                                     HealthIconManager playerHealthIcon, BoardState boardState, StrategicMode mode)
        {
            if (playerEntities == null || playerEntities.Count == 0)
                return null;

            bool isLastMonster = _avoidLosingLastMonster && IsLastMonster(attacker);

            return _targetSelectionStrategy.SelectTarget(
                attacker,
                playerEntities,
                boardState,
                mode,
                isLastMonster,
                _lastMonsterIgnoreChance);
        }

        public StrategicMode DetermineStrategicMode(BoardState boardState)
        {
            if (boardState == null)
                return StrategicMode.Defensive;

            // Calculate strategic factors
            bool healthAdvantage = boardState.EnemyHealth > boardState.PlayerHealth + _healthThresholdForAggro;
            bool boardAdvantage = boardState.EnemyBoardControl > boardState.PlayerBoardControl * 1.3f;
            bool lateGame = boardState.TurnCount > _aggressiveTurnThreshold;
            bool playerLowHealth = boardState.PlayerHealth <= 15;
            bool enemyNextTurn = !boardState.IsNextTurnPlayerFirst;
            bool hasOnlyOneMonster = CountActiveEnemyMonsters() <= 1;

            // Single monster defensive strategy
            if (hasOnlyOneMonster && Random.value > 0.25f)
            {
                Debug.Log("[AttackStrategyManager] Only one monster on field - adopting defensive strategy");
                return StrategicMode.Defensive;
            }

            // Turn order considerations
            if (enemyNextTurn)
            {
                Debug.Log("[AttackStrategyManager] Enemy goes first next turn - adopting more aggressive strategy");
                return StrategicMode.Aggro;
            }

            if (boardState.IsNextTurnPlayerFirst && !healthAdvantage && !boardAdvantage && !playerLowHealth)
            {
                Debug.Log("[AttackStrategyManager] Player goes first next turn - adopting more defensive strategy");
                return StrategicMode.Defensive;
            }

            // Situation-based strategy
            if (healthAdvantage || boardAdvantage || lateGame || playerLowHealth)
                return StrategicMode.Aggro;

            if (boardState.EnemyHealth < 15 || boardState.PlayerBoardControl > boardState.EnemyBoardControl)
                return StrategicMode.Defensive;

            // Default slightly favors aggression
            return Random.value < 0.6f ? StrategicMode.Aggro : StrategicMode.Defensive;
        }

        public bool ShouldAttackHealthIcon(EntityManager attacker, List<EntityManager> playerEntities,
                                     HealthIconManager playerHealthIcon, BoardState boardState)
        {
            return _healthIconTargetingStrategy.ShouldAttackHealthIcon(
                attacker,
                playerEntities,
                playerHealthIcon,
                boardState,
                IsLastMonster(attacker));
        }
        #endregion

        #region Utility Methods
        private bool IsLastMonster(EntityManager monster) => CountActiveEnemyMonsters() <= 1;

        private int CountActiveEnemyMonsters()
        {
            if (_entityCacheManager == null) return 0;

            _entityCacheManager.RefreshEntityCaches();
            var enemies = _entityCacheManager.CachedEnemyEntities;

            if (enemies == null) return 0;

            return enemies.Count(e => e != null && !e.dead && e.placed && !e.IsFadingOut);
        }

        private bool CanKillPlayerThisTurn(List<EntityManager> attackers, List<EntityManager> playerEntities,
                                HealthIconManager playerHealthIcon)
        {
            if (attackers == null || playerHealthIcon == null) return false;

            // Check for taunt units
            bool hasTaunt = playerEntities?.Any(e => e != null && !e.dead && e.placed &&
                                            !e.IsFadingOut && e.HasKeyword(Keywords.MonsterKeyword.Taunt)) == true;

            if (hasTaunt)
            {
                float totalDamage = attackers.Sum(a => a?.GetAttack() ?? 0);
                float remainingDamage = totalDamage;

                // Calculate damage needed to clear taunts
                var tauntUnits = playerEntities.Where(e => e != null && !e.dead && e.placed &&
                                                    !e.IsFadingOut && e.HasKeyword(Keywords.MonsterKeyword.Taunt))
                                            .OrderBy(e => e.GetHealth());

                foreach (var tauntUnit in tauntUnits)
                {
                    remainingDamage -= tauntUnit.GetHealth();
                }

                return remainingDamage >= (playerHealthIcon.GetHealth() - 2);
            }

            // Direct damage calculation
            float attackDamage = attackers.Sum(a => a?.GetAttack() ?? 0);
            return attackDamage >= (playerHealthIcon.GetHealth() - 1);
        }

        private void LogDefensiveUnits(List<EntityManager> defensiveUnits)
        {
            if (!defensiveUnits.Any()) return;

            Debug.Log($"[AttackStrategyManager] Excluding {defensiveUnits.Count} defensive units with 0 attack from attack order");
            foreach (var unit in defensiveUnits)
            {
                Debug.Log($"[AttackStrategyManager] - {unit.name} is a defensive unit (0 attack)");
            }
        }
        #endregion
    }
}
