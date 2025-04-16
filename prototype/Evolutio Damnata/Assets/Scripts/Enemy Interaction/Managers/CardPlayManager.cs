using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;
using EnemyInteraction.Utilities;

namespace EnemyInteraction.Managers
{
    public class CardPlayManager : MonoBehaviour, ICardPlayManager
    {
        [SerializeField] private SpritePositioning _spritePositioning;
        private ICombatManager _combatManager;
        private CombatStage _combatStage;
        private IKeywordEvaluator _keywordEvaluator;
        private IEffectEvaluator _effectEvaluator;
        private IBoardStateManager _boardStateManager;
        private ISpellEffectApplier _spellEffectApplier;

        // In CardPlayManager.cs
        [SerializeField, Range(0f, 1f), Tooltip("Chance to make intentionally suboptimal plays")]
        private float _suboptimalPlayChance = 0.10f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Variance in card evaluation scores")]
        private float _evaluationVariance = 0.15f;

        [SerializeField, Range(0.2f, 2f), Tooltip("Delay between enemy actions in seconds")]
        private float _actionDelay = 0.5f;

        private Dictionary<GameObject, EntityManager> _entityCache;

        private void Awake()
        {
            _entityCache = new Dictionary<GameObject, EntityManager>();
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Debug.Log("[CardPlayManager] Initializing...");

            yield return InitializeCriticalComponents();
            yield return InitializeOptionalServices();

            BuildEntityCache(); // Fixed method name
            Debug.Log("[CardPlayManager] Initialization complete");
        }

        private IEnumerator InitializeCriticalComponents()
        {
            int attempts = 0;
            const int maxAttempts = 30;

            while (attempts < maxAttempts)
            {
                _combatManager ??= FindObjectOfType<CombatManager>();
                _combatStage ??= FindObjectOfType<CombatStage>();

                if (_combatManager != null && _combatStage != null) break;

                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (_combatStage != null)
            {
                yield return InitializeCombatStageDependencies();
            }
        }

        private IEnumerator InitializeCombatStageDependencies()
        {
            int attempts = 0;
            const int maxAttempts = 30;

            while ((_combatStage.SpritePositioning == null || _combatStage.SpellEffectApplier == null) &&
                   attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            _spritePositioning ??= _combatStage.SpritePositioning as SpritePositioning;
            _spellEffectApplier ??= _combatStage.SpellEffectApplier;
        }

        private IEnumerator InitializeOptionalServices()
        {
            yield return InitializeAIServices();
            InitializeFallbackServices();
        }

        private IEnumerator InitializeAIServices()
        {
            int attempts = 0;
            const int maxAttempts = 30;

            while (AIServices.Instance == null && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (AIServices.Instance != null)
            {
                var services = AIServices.Instance;
                _keywordEvaluator ??= services.KeywordEvaluator;
                _effectEvaluator ??= services.EffectEvaluator;
                _boardStateManager ??= services.BoardStateManager;
            }
        }

        private void InitializeFallbackServices()
        {
            _keywordEvaluator ??= CreateLocalService<KeywordEvaluator>("KeywordEvaluator_Local");
            _effectEvaluator ??= CreateLocalService<EffectEvaluator>("EffectEvaluator_Local");
            _boardStateManager ??= CreateLocalService<BoardStateManager>("BoardStateManager_Local");
        }

        private T CreateLocalService<T>(string name) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            return obj.AddComponent<T>();
        }

        private void BuildEntityCache()
        {
            _entityCache.Clear();
            if (_spritePositioning == null) return;

            foreach (var entity in _spritePositioning.EnemyEntities.Concat(_spritePositioning.PlayerEntities))
            {
                if (entity != null && !_entityCache.ContainsKey(entity))
                {
                    _entityCache[entity] = entity.GetComponent<EntityManager>();
                }
            }
        }

        public IEnumerator PlayCards()
        {
            // Initial delay before starting actions
            yield return new WaitForSeconds(_actionDelay);
            Debug.Log("[CardPlayManager] Starting card play sequence...");

            if (!IsValidPlayState)
            {
                yield return SimulatePlaceholderAction();
                yield break;
            }

            var enemyDeck = _combatManager.EnemyDeck;
            if (enemyDeck == null || enemyDeck.Hand == null || enemyDeck.Hand.Count == 0)
            {
                Debug.Log("[CardPlayManager] No cards in hand to play");
                yield break;
            }

            var playableCards = GetPlayableCards(enemyDeck.Hand);
            if (playableCards.Count == 0)
            {
                Debug.Log("[CardPlayManager] No playable cards found");
                yield break;
            }

            // Add delay before evaluating board state
            yield return new WaitForSeconds(_actionDelay);

            var boardState = GetCurrentBoardState();
            var cardPlayOrder = DetermineCardPlayOrder(playableCards, boardState);

            yield return PlayCardsInOrder(cardPlayOrder, enemyDeck, boardState);

            // Final delay after all cards are played
            yield return new WaitForSeconds(_actionDelay);
            Debug.Log("[CardPlayManager] Completed playing cards");
        }

        private bool IsValidPlayState =>
            _combatManager != null &&
            _spritePositioning != null &&
            (_combatManager.IsEnemyPrepPhase() || _combatManager.IsEnemyCombatPhase());

        private IEnumerator SimulatePlaceholderAction()
        {
            Debug.LogWarning("[CardPlayManager] Using placeholder implementation");
            yield return new WaitForSeconds(_actionDelay);
            Debug.Log("[CardPlayManager] Simulating card play");
            yield return new WaitForSeconds(_actionDelay);
        }

        private List<Card> GetPlayableCards(IEnumerable<Card> hand)
        {
            return hand.Where(card =>
                card != null &&
                card.CardType != null &&
                card.CardType.ManaCost <= _combatManager.EnemyMana &&
                IsCardPlayableInCurrentPhase(card))
                .ToList();
        }

        private bool IsCardPlayableInCurrentPhase(Card card)
        {
            if (card == null || card.CardType == null)
                return false;

            // For monster cards, only allow play during prep phase
            if (card.CardType.IsMonsterCard)
                return _combatManager.IsEnemyPrepPhase();

            // For spell cards, check that they have valid effect types
            if (card.CardType.IsSpellCard)
            {
                bool hasValidEffects = card.CardType.EffectTypes != null &&
                                     card.CardType.EffectTypes.Any();

                // Allow damaging spells in combat phase, utility spells only in prep phase
                if (_combatManager.IsEnemyCombatPhase())
                {
                    // During combat, only allow damaging spells
                    bool hasDamagingEffect = card.CardType.EffectTypes.Any(e =>
                        e == SpellEffect.Damage || e == SpellEffect.Burn);
                    return hasValidEffects && hasDamagingEffect;
                }
                else if (_combatManager.IsEnemyPrepPhase())
                {
                    // During prep, allow any spell with valid effects
                    return hasValidEffects;
                }
            }

            return false;
        }


        private BoardState GetCurrentBoardState()
        {
            return _boardStateManager?.EvaluateBoardState() ?? new BoardState
            {
                EnemyMana = _combatManager.EnemyMana,
                TurnCount = _combatManager.TurnCount,
                EnemyHealth = _combatManager.EnemyHealth,
                PlayerHealth = _combatManager.PlayerHealth
            };
        }

        private List<Card> DetermineCardPlayOrder(List<Card> playableCards, BoardState boardState)
        {
            var scoredCards = playableCards
                .Select(card => new
                {
                    Card = card,
                    Score = ApplyDecisionVariance(EvaluateCardPlay(card, boardState))
                })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Card)
                .ToList();

            Debug.Log($"[CardPlayManager] Highest scored card: {scoredCards.FirstOrDefault()?.CardName}");

            return scoredCards;
        }

        private float ApplyDecisionVariance(float baseScore)
        {
            // Occasionally make suboptimal choices
            if (Random.value < _suboptimalPlayChance)
            {
                baseScore *= Random.Range(0.5f, 0.8f);
                Debug.Log("[AI] Making suboptimal play for variety");
            }

            // Add small random variance to scores
            return baseScore * Random.Range(1f - _evaluationVariance, 1f + _evaluationVariance);
        }

        private IEnumerator PlayCardsInOrder(List<Card> cardsToPlay, Deck enemyDeck, BoardState boardState)
        {
            foreach (var card in cardsToPlay)
            {
                if (_combatManager.EnemyMana < card.CardType.ManaCost)
                {
                    Debug.Log($"[CardPlayManager] Not enough mana for {card.CardName}");
                    continue;
                }

                // Consistent delay before each card play
                yield return new WaitForSeconds(_actionDelay);

                Debug.Log($"[CardPlayManager] Attempting to play {card.CardName}");

                bool playedSuccessfully = false;

                if (card.CardType.IsMonsterCard)
                {
                    playedSuccessfully = PlayMonsterCard(card, boardState);

                    if (playedSuccessfully)
                    {
                        _combatManager.EnemyMana -= card.CardType.ManaCost;
                        enemyDeck.RemoveCard(card);
                    }
                }
                else
                {
                    // For spell cards, check if we can play it
                    playedSuccessfully = CanPlaySpellCard(card);
                    if (playedSuccessfully)
                    {
                        // First remove the card and deduct mana
                        _combatManager.EnemyMana -= card.CardType.ManaCost;
                        enemyDeck.RemoveCard(card);

                        // Then play the spell effect
                        yield return PlaySpellCardWithDelay(card);
                    }
                }

                if (playedSuccessfully)
                {
                    // Update board state after successful play
                    boardState = GetCurrentBoardState();

                    if (_combatManager.EnemyMana < 1) break;
                }

                // Additional delay after the card effect is applied
                yield return new WaitForSeconds(_actionDelay * 0.5f);
            }
        }

        private bool PlayMonsterCard(Card card, BoardState boardState)
        {
            int position = FindOptimalMonsterPosition(card, boardState);
            if (position < 0)
            {
                Debug.Log($"[CardPlayManager] No valid position for {card.CardName}");
                return false;
            }

            bool success = _combatStage.EnemyCardSpawner.SpawnCard(card.CardName, position);
            if (success)
            {
                Debug.Log($"[CardPlayManager] Played {card.CardName} at position {position}");
            }
            return success;
        }

        private bool CanPlaySpellCard(Card card)
        {
            if (_spellEffectApplier == null)
            {
                Debug.LogError("[CardPlayManager] SpellEffectApplier is null");
                return false;
            }

            var target = GetBestSpellTarget(card.CardType);
            if (target == null)
            {
                Debug.Log($"[CardPlayManager] No target for spell {card.CardName}");
                return false;
            }

            return true;
        }

        private IEnumerator PlaySpellCardWithDelay(Card card)
        {
            var target = GetBestSpellTarget(card.CardType);
            Debug.Log($"[CardPlayManager] Preparing to cast {card.CardName} on {target.name}");

            // Small pause before applying the effect
            yield return new WaitForSeconds(_actionDelay * 0.5f);

            try
            {
                _spellEffectApplier.ApplySpellEffectsAI(target, card.CardType, 0);
                Debug.Log($"[CardPlayManager] Cast {card.CardName} on {target.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardPlayManager] Spell error: {e.Message}");
            }
        }


        private int FindOptimalMonsterPosition(Card card, BoardState boardState)
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

        private List<int> GetAvailableMonsterPositions()
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

        private float CalculatePositionScore(int position, Card card, bool hasRanged, bool hasTaunt)
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
                score += (_spritePositioning.EnemyEntities.Count - position) * 5f; // Prefer frontline
            }
            else if (card.CardType.Health >= 5)
            {
                score += (_spritePositioning.EnemyEntities.Count - position) * 3f; // Tanky units frontline
            }
            else if (card.CardType.AttackPower >= 5)
            {
                float midValue = 1 - midDist / (_spritePositioning.EnemyEntities.Count / 2f);
                score += midValue * 15f; // Damage dealers mid
            }

            return score;
        }

        private EntityManager GetBestSpellTarget(CardData cardType)
        {
            if (cardType?.EffectTypes == null || !cardType.EffectTypes.Any())
                return null;

            var effect = cardType.EffectTypes.First();
            bool isDamagingEffect = effect == SpellEffect.Damage || effect == SpellEffect.Burn;

            var potentialTargets = GetAllValidTargets(effect);
            if (potentialTargets.Count == 0)
                return null;

            // Filter targets using AIUtilities to ensure proper targeting
            var validTargets = potentialTargets
                .Where(target => AIUtilities.IsValidTargetForEffect(target, effect, isDamagingEffect))
                .ToList();

            if (validTargets.Count == 0)
            {
                Debug.LogWarning($"[CardPlayManager] Found {potentialTargets.Count} targets but none were valid for {effect}");
                return null;
            }

            return validTargets
                .OrderByDescending(t => CalculateThreatScore(t, cardType))
                .FirstOrDefault();
        }


        private List<EntityManager> GetAllValidTargets(SpellEffect effect)
        {
            var targets = new List<EntityManager>();

            // For damage effects, add player entities and player health icon
            if (effect == SpellEffect.Damage || effect == SpellEffect.Burn)
            {
                // Add player entities
                if (_spritePositioning != null)
                {
                    targets.AddRange(
                        _spritePositioning.PlayerEntities
                            .Where(e => e != null)
                            .Select(e => _entityCache.TryGetValue(e, out var em) ? em : null)
                            .Where(e => e != null && e.placed)
                    );
                }

                // Add player health icon only when no player entities on the field
                if (targets.Count == 0)
                {
                    var playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
                    if (playerHealth != null) targets.Add(playerHealth);
                }
            }
            // For healing effects, add enemy entities and enemy health icon
            else if (effect == SpellEffect.Heal)
            {
                // Add enemy entities
                if (_spritePositioning != null)
                {
                    targets.AddRange(
                        _spritePositioning.EnemyEntities
                            .Where(e => e != null)
                            .Select(e => _entityCache.TryGetValue(e, out var em) ? em : null)
                            .Where(e => e != null && e.placed)
                    );
                }

                // Add enemy health icon as a potential target for healing
                var enemyHealth = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
                if (enemyHealth != null && enemyHealth.GetHealth() < enemyHealth.MaxHealth)
                {
                    targets.Add(enemyHealth);
                }
            }

            return targets;
        }

        private float CalculateThreatScore(EntityManager target, CardData cardType)
        {
            float score = 0f;

            // Base threat value
            if (target is HealthIconManager healthIcon)
            {
                score = healthIcon.GetHealth() * 0.5f; // Prioritize low-health heroes
                if (healthIcon.GetHealth() < 10) score += 100f; // Lethal priority
            }
            else
            {
                score = target.GetAttack() * 1.2f + target.GetHealth() * 0.8f;
            }

            // Keyword modifiers
            if (target.HasKeyword(Keywords.MonsterKeyword.Taunt))
                score += 40f;

            return score;
        }

        private float EvaluateCardPlay(Card card, BoardState boardState)
        {
            if (card?.CardType == null) return 0f;

            float score = 0f;
            float manaRatio = 1 - (card.CardType.ManaCost / (float)Mathf.Max(1, _combatManager.EnemyMana));
            score += manaRatio * 50f;

            if (card.CardType.IsMonsterCard)
            {
                score += EvaluateMonsterCard(card, boardState);
            }
            else
            {
                score += EvaluateSpellCard(card, boardState);
            }

            // Strategic modifiers based on board state
            if (boardState.HealthAdvantage < 0) // Losing
            {
                if (card.CardType.HasKeyword(Keywords.MonsterKeyword.Taunt))
                    score += 30f;
                if (card.CardType.EffectTypes.Contains(SpellEffect.Heal))
                    score += 40f;
            }
            else // Winning or even
            {
                if (card.CardType.AttackPower > 0)
                    score += 20f;
                if (card.CardType.EffectTypes.Contains(SpellEffect.Damage))
                    score += 30f;
            }

            return score;
        }

        private float EvaluateMonsterCard(Card card, BoardState boardState)
        {
            float score = card.CardType.AttackPower * 1.0f + card.CardType.Health * 0.7f;
            float attackHealthRatio = card.CardType.AttackPower / (float)Mathf.Max(1, card.CardType.Health);

            // Prioritize ranged units more
            if (card.CardType.HasKeyword(Keywords.MonsterKeyword.Ranged))
            {
                score += 30f + (attackHealthRatio > 1.5f ? 20f : 0f);
            }
            else
            {
                if (card.CardType.Health >= 5) score += 25f;
                if (card.CardType.Health <= 2 && card.CardType.AttackPower > 3) score -= 10f;
            }

            // Evaluate other keywords with higher weights
            foreach (Keywords.MonsterKeyword keyword in System.Enum.GetValues(typeof(Keywords.MonsterKeyword)))
            {
                if (keyword != Keywords.MonsterKeyword.Ranged && card.CardType.HasKeyword(keyword))
                {
                    score += (_keywordEvaluator?.EvaluateKeyword(keyword, true, boardState) ?? 0f) * 1.2f;
                }
            }

            return score;
        }

        private float EvaluateSpellCard(Card card, BoardState boardState)
        {
            if (card.CardType.EffectTypes == null) return 0f;

            return card.CardType.EffectTypes
                .Sum(effect => _effectEvaluator?.EvaluateEffect(effect, true,
                    _effectEvaluator.GetBestTargetForEffect(effect, true, boardState),
                    boardState) ?? 0f);
        }
    }
}