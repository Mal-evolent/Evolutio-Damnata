using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Models;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;
using EnemyInteraction.Utilities;

namespace EnemyInteraction.Managers
{
    public class AttackManager : MonoBehaviour, IAttackManager
    {
        [SerializeField] private SpritePositioning _spritePositioning;
        private ICombatManager _combatManager;
        private CombatStage _combatStage;
        private IKeywordEvaluator _keywordEvaluator;
        private IBoardStateManager _boardStateManager;
        private AttackLimiter _attackLimiter;

        // Caching systems
        private Dictionary<GameObject, EntityManager> _entityManagerCache;
        private List<EntityManager> _cachedPlayerEntities;
        private List<EntityManager> _cachedEnemyEntities;

        // In AttackManager.cs
        [SerializeField, Range(0f, 1f), Tooltip("Chance to make suboptimal decisions")]
        private float _decisionVariance = 0.10f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Variance in target evaluation scores")]
        private float _evaluationVariance = 0.15f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Chance to randomize attack order")]
        private float _attackOrderRandomizationChance = 0.10f;

        [SerializeField, Tooltip("Health difference to switch strategies")]
        private float _healthThresholdForAggro = 8f;

        [SerializeField, Tooltip("Turn count to become more aggressive")]
        private int _aggressiveTurnThreshold = 4;

        // New delay control parameters
        [SerializeField, Range(0.2f, 2f), Tooltip("Base delay between attack actions in seconds")]
        private float _baseAttackDelay = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("Random variance in delay timing (percentage)")]
        private float _delayVariance = 0.3f;

        [SerializeField, Range(0.1f, 1.5f), Tooltip("Initial delay before starting attack sequence")]
        private float _initialAttackDelay = 0.8f;

        [SerializeField, Range(0.1f, 1f), Tooltip("Delay after attack evaluations")]
        private float _evaluationDelay = 0.4f;
        
        private void Awake()
        {
            _entityManagerCache = new Dictionary<GameObject, EntityManager>();
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[AttackManager] Starting initialization...");

            int maxAttempts = 30;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                _combatManager = _combatManager ?? FindObjectOfType<CombatManager>();
                _combatStage = _combatStage ?? FindObjectOfType<CombatStage>();

                if (_combatManager != null && _combatStage != null)
                    break;

                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (_combatStage != null)
            {
                attempts = 0;
                while (_combatStage.SpritePositioning == null && attempts < maxAttempts)
                {
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }

                if (_spritePositioning == null && _combatStage.SpritePositioning != null)
                {
                    _spritePositioning = _combatStage.SpritePositioning as SpritePositioning;
                }

                if (_attackLimiter == null)
                {
                    _attackLimiter = _combatStage.GetAttackLimiter();
                }
            }

            if (AIServices.Instance != null)
            {
                var services = AIServices.Instance;
                _keywordEvaluator = _keywordEvaluator ?? services.KeywordEvaluator;
                _boardStateManager = _boardStateManager ?? services.BoardStateManager;
            }

            if (_keywordEvaluator == null)
            {
                var keywordEvaluatorObj = new GameObject("KeywordEvaluator_Local");
                keywordEvaluatorObj.transform.SetParent(transform);
                _keywordEvaluator = keywordEvaluatorObj.AddComponent<KeywordEvaluator>();
            }

            if (_boardStateManager == null)
            {
                var boardStateManagerObj = new GameObject("BoardStateManager_Local");
                boardStateManagerObj.transform.SetParent(transform);
                _boardStateManager = boardStateManagerObj.AddComponent<BoardStateManager>();
            }

            // Initialize caches
            BuildEntityManagerCache();
            RefreshEntityCaches();

            Debug.Log("[AttackManager] Initialization completed");
        }

        private void BuildEntityManagerCache()
        {
            _entityManagerCache.Clear();
            if (_spritePositioning == null) return;

            foreach (var entity in _spritePositioning.EnemyEntities.Concat(_spritePositioning.PlayerEntities))
            {
                if (entity != null && !_entityManagerCache.ContainsKey(entity))
                {
                    _entityManagerCache[entity] = entity.GetComponent<EntityManager>();
                }
            }
        }

        private void RefreshEntityCaches()
        {
            // Take a local snapshot of the entity lists to prevent race conditions
            var enemyEntitiesList = _spritePositioning?.EnemyEntities;
            var playerEntitiesList = _spritePositioning?.PlayerEntities;

            if (enemyEntitiesList != null && playerEntitiesList != null)
            {
                _cachedEnemyEntities = GetValidEntities(enemyEntitiesList, true);
                _cachedPlayerEntities = GetValidEntities(playerEntitiesList, false);

                // Log entities found
                Debug.Log($"[AttackManager] Refreshed entity caches - Found {_cachedEnemyEntities.Count} enemy entities and {_cachedPlayerEntities.Count} player entities");
            }
            else
            {
                Debug.LogWarning("[AttackManager] Could not refresh entity caches - sprite positioning references are null");
            }
        }

        private List<EntityManager> GetValidEntities(IEnumerable<GameObject> source, bool checkAttackLimiter)
        {
            if (source == null) return new List<EntityManager>();

            return source
                .Where(e => e != null && _entityManagerCache.ContainsKey(e))
                .Select(e => _entityManagerCache[e])
                .Where(em => em != null && em.placed && !em.dead && !em.IsFadingOut &&
                      (!checkAttackLimiter || (_attackLimiter?.CanAttack(em) ?? !em.HasAttacked)))
                .ToList();
        }

        public IEnumerator Attack()
        {
            Debug.Log("[AttackManager] Starting Attack");

            // Initial delay before starting attack sequence - gives player time to prepare
            yield return new WaitForSeconds(_initialAttackDelay);

            if (_combatManager == null || _spritePositioning == null)
            {
                yield return SimulatePlaceholderAttack();
                yield break;
            }

            if (!ValidateCombatState())
            {
                yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay * 0.5f));
                yield break;
            }

            // Use cached entities
            RefreshEntityCaches();
            List<EntityManager> enemyEntities = _cachedEnemyEntities;
            List<EntityManager> playerEntities = _cachedPlayerEntities;
            HealthIconManager playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();

            bool setupSuccess = false;
            string errorMessage = null;

            try
            {
                setupSuccess = ValidateAttackScenario(enemyEntities, playerEntities, playerHealthIcon);
            }
            catch (System.Exception e)
            {
                errorMessage = e.Message;
                Debug.LogError($"[AttackManager] Error in Attack: {errorMessage}");
            }

            if (!setupSuccess || errorMessage != null)
            {
                yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay * 0.5f));
                yield break;
            }

            // Delay to simulate "thinking" about attack strategy
            yield return new WaitForSeconds(GetRandomizedDelay(_evaluationDelay));

            BoardState boardState = GetCurrentBoardState();
            var attackOrder = GetAttackOrder(enemyEntities, playerEntities, playerHealthIcon, boardState);

            StrategicMode mode = DetermineStrategicMode(boardState);
            Debug.Log($"[AttackManager] Current strategy: {mode}");

            // Brief pause after determining strategy before first attack
            yield return new WaitForSeconds(GetRandomizedDelay(_evaluationDelay * 0.5f));

            // Process each attack with appropriate delays
            foreach (var attacker in attackOrder)
            {
                // Delay before selecting target - simulates AI "thinking"
                yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay * 0.7f));

                EntityManager targetEntity = SelectTarget(attacker, playerEntities, playerHealthIcon, boardState, mode);

                if (targetEntity != null)
                {
                    // Small delay between target selection and attack execution
                    yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay * 0.3f));

                    yield return ExecuteAttack(attacker, targetEntity);

                    if (targetEntity.dead)
                    {
                        // Add longer pause after killing a unit to emphasize the moment
                        yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay * 1.2f));

                        RefreshEntityCaches();
                        playerEntities = _cachedPlayerEntities;
                        if (playerEntities.Count == 0 && playerHealthIcon == null) break;
                    }
                    else
                    {
                        // Normal post-attack delay
                        yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay));
                    }
                }
                else if (playerHealthIcon != null && ShouldAttackHealthIcon(attacker, playerEntities, playerHealthIcon, boardState))
                {
                    // Dramatic pause before attacking health icon directly
                    yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay * 0.5f));

                    AttackPlayerHealthIcon(attacker, playerHealthIcon);

                    // Longer pause after attacking health icon to emphasize importance
                    yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay * 1.5f));
                }
            }

            // Final delay after attack sequence completes
            yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay));
            Debug.Log("[AttackManager] Attack completed");
        }

        // Helper method to get randomized delay times for more human feeling
        private float GetRandomizedDelay(float baseDelay)
        {
            if (_delayVariance <= 0) return baseDelay;

            float variance = baseDelay * _delayVariance;
            return baseDelay + Random.Range(-variance, variance);
        }

        private List<EntityManager> GetAttackOrder(List<EntityManager> enemies, List<EntityManager> players,
                                                HealthIconManager healthIcon, BoardState boardState)
        {
            var order = enemies.ToList();

            if (Random.value < _attackOrderRandomizationChance && order.Count > 1)
            {
                order = GetPartiallyShuffledAttackers(order);
            }

            if (CanKillPlayerThisTurn(order, players, healthIcon))
            {
                order = OptimizeForLethal(order, players);
            }
            else
            {
                // Prioritize Overwhelm attackers when there are multiple targets to hit with splash
                bool hasMultiplePlayerEntities = players != null && players.Count > 1;

                order = order
                    // Overwhelm is most valuable when there are multiple targets to splash damage
                    .OrderByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && hasMultiplePlayerEntities ? 2 : 0)
                    // Ranged attackers are always valuable
                    .ThenByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Ranged) ? 1 : 0)
                    // Prioritize attackers that can kill targets
                    .ThenByDescending(e => players.Any(p => e.GetAttack() >= p.GetHealth()) ? 1 : 0)
                    // Tough attackers are better for attacking high-attack targets
                    .ThenByDescending(e => e.HasKeyword(Keywords.MonsterKeyword.Tough) && players.Any(p => p.GetAttack() >= 4) ? 1 : 0)
                    .ThenByDescending(e => e.GetAttack())
                    .ThenBy(e => e.GetHealth())
                    .ToList();
            }

            return order;
        }

        private EntityManager SelectTarget(EntityManager attacker, List<EntityManager> playerEntities,
                                         HealthIconManager playerHealthIcon, BoardState boardState, StrategicMode mode)
        {
            if (playerEntities == null || playerEntities.Count == 0)
                return null;

            bool hasTaunts = playerEntities.Any(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt));

            if (hasTaunts)
            {
                var tauntTargets = playerEntities.Where(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt)).ToList();

                // For Overwhelm attackers against taunt, use specialized targeting
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && tauntTargets.Count > 0)
                    return SelectTargetForOverwhelmAttacker(attacker, tauntTargets, boardState);

                return SelectBestTarget(attacker, tauntTargets, boardState, mode);
            }

            // For Overwhelm attackers with multiple targets, use specialized targeting
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm) && playerEntities.Count > 1)
                return SelectTargetForOverwhelmAttacker(attacker, playerEntities, boardState);

            return SelectBestTarget(attacker, playerEntities, boardState, mode);
        }

        private EntityManager SelectTargetForOverwhelmAttacker(EntityManager attacker, List<EntityManager> targets, BoardState boardState)
        {
            if (targets == null || targets.Count <= 1)
                return targets?.FirstOrDefault();

            // Calculate splash damage (50% of attacker's damage)
            float splashDamage = Mathf.Floor(attacker.GetAttack() * 0.5f);

            // Prioritize targets where:
            // 1. We can kill the main target
            // 2. Splash damage can kill or significantly damage other targets

            var scoredTargets = new Dictionary<EntityManager, float>();

            foreach (var target in targets)
            {
                float score = 0;

                // Base score for being able to kill the main target
                if (attacker.GetAttack() >= target.GetHealth())
                    score += 100f;

                // Calculate splash potential against other targets
                var otherTargets = targets.Where(t => t != target).ToList();
                int potentialKills = otherTargets.Count(t => t.GetHealth() <= splashDamage);
                int damagedTargets = otherTargets.Count(t => t.GetHealth() > splashDamage);

                // Add score for potential splash kills and damage
                score += potentialKills * 50f;
                score += damagedTargets * splashDamage * 2f;

                // Prefer targets with high attack if we can kill them
                if (attacker.GetAttack() >= target.GetHealth())
                    score += target.GetAttack() * 5f;

                // Prefer targets surrounded by many other targets to maximize splash
                score += otherTargets.Count * 5f;

                scoredTargets[target] = score;
            }

            // Return target with highest score
            return scoredTargets.OrderByDescending(kvp => kvp.Value).First().Key;
        }


        private IEnumerator ExecuteAttack(EntityManager attacker, EntityManager target)
        {
            _combatStage.HandleMonsterAttack(attacker, target);

            if (_attackLimiter != null)
            {
                _attackLimiter.RegisterAttack(attacker);
            }
            else
            {
                attacker.HasAttacked = true;
            }

            // Brief pause after attack execution to let animation play
            yield return new WaitForSeconds(0.2f);
        }

        private StrategicMode DetermineStrategicMode(BoardState boardState)
        {
            if (boardState == null)
                return StrategicMode.Defensive;

            bool healthAdvantage = boardState.EnemyHealth > boardState.PlayerHealth + _healthThresholdForAggro;
            bool boardAdvantage = boardState.EnemyBoardControl > boardState.PlayerBoardControl * 1.3f;
            bool lateGame = boardState.TurnCount > _aggressiveTurnThreshold;

            // New condition: if player is low on health, go aggressive
            bool playerLowHealth = boardState.PlayerHealth <= 15;

            if (healthAdvantage || boardAdvantage || lateGame || playerLowHealth)
                return StrategicMode.Aggro;

            if (boardState.EnemyHealth < 15 || boardState.PlayerBoardControl > boardState.EnemyBoardControl)
                return StrategicMode.Defensive;

            // Default to aggressive more often
            return Random.value < 0.6f ? StrategicMode.Aggro : StrategicMode.Defensive;
        }

        private EntityManager SelectBestTarget(EntityManager attacker, List<EntityManager> targets,
                                            BoardState boardState, StrategicMode mode)
        {
            if (attacker == null || targets == null || targets.Count == 0)
                return null;

            var validTargets = targets.Where(t => t != null && !t.dead && !t.IsFadingOut).ToList();

            if (validTargets.Count == 0)
                return null;

            if (validTargets.Count == 1)
                return validTargets[0];

            var targetScores = new Dictionary<EntityManager, float>();
            foreach (var target in validTargets)
            {
                try
                {
                    float score = EvaluateTarget(attacker, target, boardState, mode);
                    targetScores[target] = score;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AttackManager] Error evaluating target: {e.Message}");
                }
            }

            var sortedTargets = targetScores.OrderByDescending(kvp => kvp.Value).ToList();

            if (sortedTargets.Count == 0)
                return null;

            if (ShouldMakeSuboptimalDecision() && sortedTargets.Count > 1)
            {
                int randomIndex = Random.Range(1, Mathf.Min(sortedTargets.Count, 3));
                return sortedTargets[randomIndex].Key;
            }

            return sortedTargets[0].Key;
        }

        private float EvaluateTarget(EntityManager attacker, EntityManager target, BoardState boardState, StrategicMode mode)
        {
            if (attacker == null || target == null)
                return float.MinValue;

            float score = 0;

            score += attacker.GetAttack() * 1.2f - target.GetHealth() * 0.8f;

            if (mode == StrategicMode.Aggro)
            {
                score += target.GetAttack() * 0.7f;
                if (target.GetHealth() <= attacker.GetAttack())
                    score += 90f;
            }
            else
            {
                score -= attacker.GetHealth() * 0.2f;
                if (target.HasKeyword(Keywords.MonsterKeyword.Taunt))
                    score += 60f;
            }

            // Add the new keyword interactions evaluation
            score += EvaluateKeywordInteractions(attacker, target, boardState);

            if (_keywordEvaluator != null)
            {
                score += _keywordEvaluator.EvaluateKeywords(attacker, target, boardState) * 1.2f;
            }

            if (!attacker.HasKeyword(Keywords.MonsterKeyword.Ranged))
            {
                if (attacker.GetHealth() <= target.GetAttack())
                {
                    score -= 80f;
                }
                else
                {
                    score -= (target.GetAttack() / attacker.GetHealth()) * 40f;
                }
            }

            score *= Random.Range(1f - _evaluationVariance, 1f + _evaluationVariance);

            return score;
        }

        #region Helper Methods
        private bool ValidateCombatState()
        {
            return _combatManager != null && _combatManager.IsEnemyCombatPhase();
        }

        private bool ValidateAttackScenario(List<EntityManager> enemyEntities, List<EntityManager> playerEntities,
                                          HealthIconManager playerHealthIcon)
        {
            bool hasEnemyEntities = enemyEntities != null && enemyEntities.Count > 0;
            bool hasTargets = (playerEntities != null && playerEntities.Count > 0) || playerHealthIcon != null;

            if (!hasEnemyEntities)
                Debug.Log("[AttackManager] No enemy entities available to attack");

            if (!hasTargets)
                Debug.Log("[AttackManager] No valid targets available");

            return hasEnemyEntities && hasTargets;
        }

        private BoardState GetCurrentBoardState()
        {
            return _boardStateManager?.EvaluateBoardState() ?? new BoardState
            {
                EnemyHealth = _combatManager.EnemyHealth,
                PlayerHealth = _combatManager.PlayerHealth,
                TurnCount = _combatManager.TurnCount
            };
        }

        private IEnumerator SimulatePlaceholderAttack()
        {
            Debug.LogWarning("[AttackManager] Using placeholder attack implementation");
            // More human-like delays for placeholder implementation
            yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay));
            Debug.Log("[AttackManager] Simulating enemy attacks");
            yield return new WaitForSeconds(GetRandomizedDelay(_baseAttackDelay * 1.5f));
        }

        private List<EntityManager> GetPartiallyShuffledAttackers(List<EntityManager> original)
        {
            var shuffled = new List<EntityManager>(original);
            for (int i = 0; i < shuffled.Count - 1; i++)
            {
                if (Random.value < 0.4f)
                {
                    var temp = shuffled[i];
                    shuffled[i] = shuffled[i + 1];
                    shuffled[i + 1] = temp;
                }
            }
            Debug.Log("[AttackManager] Applied partial randomization to attack order");
            return shuffled;
        }

        private bool ShouldMakeSuboptimalDecision()
        {
            return Random.value < _decisionVariance;
        }
        private bool CanKillPlayerThisTurn(List<EntityManager> attackers, List<EntityManager> playerEntities,
                                        HealthIconManager playerHealthIcon)
        {
            if (attackers == null || playerHealthIcon == null)
                return false;

            // If there are taunt monsters, we must attack them first
            bool hasTaunt = playerEntities != null &&
                            playerEntities.Any(e => e != null &&
                                              !e.dead &&
                                              e.placed &&
                                              !e.IsFadingOut &&
                                              e.HasKeyword(Keywords.MonsterKeyword.Taunt));

            if (hasTaunt)
            {
                // Calculate remaining damage after dealing with taunts
                float totalDamage = attackers.Sum(a => a?.GetAttack() ?? 0);
                float remainingDamage = totalDamage;

                // Get all taunt units
                var tauntUnits = playerEntities.Where(e => e != null &&
                                                      !e.dead &&
                                                      e.placed &&
                                                      !e.IsFadingOut &&
                                                      e.HasKeyword(Keywords.MonsterKeyword.Taunt))
                                              .OrderBy(e => e.GetHealth())
                                              .ToList();

                // Calculate damage needed to clear taunts
                foreach (var tauntUnit in tauntUnits)
                {
                    remainingDamage -= tauntUnit.GetHealth();
                }

                // More accurate lethal calculation - if we have exactly enough damage or just a little extra
                float tauntThreshold = playerHealthIcon.GetHealth() - 2;
                return remainingDamage >= tauntThreshold;
            }

            // If no taunt units, check if total damage exceeds player health with a small margin
            float attackDamage = attackers.Sum(a => a?.GetAttack() ?? 0);
            float directThreshold = playerHealthIcon.GetHealth() - 1;
            return attackDamage >= directThreshold;
        }

        // Add this method to your AttackManager.cs class to specifically evaluate 
        // how these keywords affect the attack decisions
        private float EvaluateKeywordInteractions(EntityManager attacker, EntityManager target, BoardState boardState)
        {
            if (attacker == null || target == null)
                return 0f;

            float score = 0f;

            // Evaluate Overwhelm offensive potential
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
            {
                // Calculate potential splash damage
                float splashDamage = Mathf.Floor(attacker.GetAttack() * 0.5f);

                // Get all other entities on target's side
                var targetSideEntities = target.GetMonsterType() == EntityManager.MonsterType.Enemy ?
                    _cachedEnemyEntities : _cachedPlayerEntities;

                // Count how many entities could be damaged by splash
                int splashTargets = targetSideEntities.Count(e => e != target && !e.dead && !e.IsFadingOut);

                // Count how many could potentially die from splash damage
                int potentialSplashKills = targetSideEntities.Count(e =>
                    e != target && !e.dead && !e.IsFadingOut && e.GetHealth() <= splashDamage);

                // Add score based on potential splash damage value
                score += splashDamage * splashTargets * 2.0f;

                // Add significant bonus for potential kills
                score += potentialSplashKills * 40f;

                Debug.Log($"[AttackManager] Evaluating Overwhelm: {splashTargets} splash targets, " +
                          $"{potentialSplashKills} potential splash kills, adding {score} to score");
            }

            // Evaluate attacking against Tough defenders
            if (target.HasKeyword(Keywords.MonsterKeyword.Tough))
            {
                // Tough reduces damage by half, making the target less attractive unless:
                // 1. We have enough damage to still kill it
                // 2. We are using Overwhelm which can bypass Tough with splash damage to other units

                // Reduce target score as it's harder to kill
                score -= 20f;

                // But if we can still kill it with our attack even after reduction, it's a good target
                float damageAfterTough = Mathf.Floor(attacker.GetAttack() / 2f);
                if (damageAfterTough >= target.GetHealth())
                {
                    // We can still kill it despite Tough - high priority target
                    score += 40f;
                    Debug.Log($"[AttackManager] Can kill Tough entity {target.name} with {attacker.name}");
                }

                // If we have Overwhelm, targeting a Tough unit might still be good for splash damage
                if (attacker.HasKeyword(Keywords.MonsterKeyword.Overwhelm))
                {
                    score += 15f;
                    Debug.Log($"[AttackManager] Overwhelm attack against Tough target still valuable for splash");
                }
            }

            // Evaluate attacking with Tough attackers
            if (attacker.HasKeyword(Keywords.MonsterKeyword.Tough))
            {
                // Tough attackers take less counter damage, making them better for attacking
                score += 15f;

                // Extra value when attacking a high-attack target
                if (target.GetAttack() >= 4)
                {
                    score += 20f;
                    Debug.Log($"[AttackManager] Using Tough attacker {attacker.name} against high-attack target {target.name}");
                }

                // If attacker won't die from counter attack due to Tough, it's very valuable
                float counterDamage = Mathf.Floor(target.GetAttack() / 2f);
                if (counterDamage < attacker.GetHealth())
                {
                    score += 30f;
                    Debug.Log($"[AttackManager] Tough attacker {attacker.name} will survive counter attack");
                }
            }

            return score;
        }


        private List<EntityManager> OptimizeForLethal(List<EntityManager> attackers, List<EntityManager> playerEntities)
        {
            Debug.Log("[AttackManager] Optimizing attack order for lethal");

            if (playerEntities != null && playerEntities.Any(e => e != null && e.HasKeyword(Keywords.MonsterKeyword.Taunt)))
            {
                return attackers
                    .OrderBy(e => e.GetHealth())
                    .ThenByDescending(e => e.GetAttack())
                    .ToList();
            }

            return attackers
                .OrderByDescending(e => e.GetAttack())
                .ToList();
        }

        private bool ShouldAttackHealthIcon(EntityManager attacker, List<EntityManager> playerEntities,
                                         HealthIconManager playerHealthIcon, BoardState boardState)
        {
            if (attacker == null || playerHealthIcon == null)
                return false;

            // Use AIUtilities to determine if we can target the health icon
            return AIUtilities.CanTargetHealthIcon(playerEntities);
        }


        private bool HasEntitiesOnField(bool isPlayerSide)
        {
            if (_spritePositioning == null)
                return false;

            var entities = isPlayerSide ? _spritePositioning.PlayerEntities : _spritePositioning.EnemyEntities;

            foreach (var entity in entities)
            {
                if (!_entityManagerCache.TryGetValue(entity, out var entityManager)) continue;

                if (entityManager != null && entityManager.placed && !entityManager.dead && !entityManager.IsFadingOut)
                {
                    return true;
                }
            }

            return false;
        }

        private bool AttackPlayerHealthIcon(EntityManager attacker, HealthIconManager healthIcon)
        {
            if (attacker == null || healthIcon == null || _combatStage == null)
                return false;

            if (HasEntitiesOnField(true))
            {
                Debug.LogWarning("[AttackManager] Cannot attack health icon - player entities present");
                return false;
            }

            try
            {
                _combatStage.HandleMonsterAttack(attacker, healthIcon);

                if (_attackLimiter != null)
                    _attackLimiter.RegisterAttack(attacker);
                else
                    attacker.HasAttacked = true;

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AttackManager] Error attacking health icon: {e.Message}");
                return false;
            }
        }
        #endregion
    }

    public enum StrategicMode
    {
        Aggro,
        Defensive
    }
}