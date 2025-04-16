using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers.Evaluation;
using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Managers.Execution;
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

        [SerializeField, Range(0f, 1f), Tooltip("Chance to make intentionally suboptimal plays")]
        private float _suboptimalPlayChance = 0.10f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Variance in card evaluation scores")]
        private float _evaluationVariance = 0.15f;

        [SerializeField, Range(0.2f, 2f), Tooltip("Delay between enemy actions in seconds")]
        private float _actionDelay = 0.5f;

        private Dictionary<GameObject, EntityManager> _entityCache;

        // New components
        private ICardEvaluator _cardEvaluator;
        private MonsterPositionSelector _monsterPositionSelector;
        private SpellTargetSelector _spellTargetSelector;
        private ICardPlayExecutor _cardPlayExecutor;

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

            InitializeCardPlayComponents();
            BuildEntityCache();
            Debug.Log("[CardPlayManager] Initialization complete");
        }

        private void InitializeCardPlayComponents()
        {
            // Create our specialized components
            _cardEvaluator = new CardEvaluator(
                _combatManager,
                _keywordEvaluator,
                _effectEvaluator,
                _suboptimalPlayChance,
                _evaluationVariance);

            _monsterPositionSelector = new MonsterPositionSelector(
                _spritePositioning,
                _entityCache);

            _spellTargetSelector = new SpellTargetSelector(
                _spritePositioning,
                _entityCache);

            _cardPlayExecutor = new CardPlayExecutor(
                _combatManager,
                _combatStage,
                _spellEffectApplier,
                _monsterPositionSelector,
                _spellTargetSelector,
                _actionDelay);
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

            var playableCards = _cardEvaluator.GetPlayableCards(enemyDeck.Hand);
            if (playableCards.Count == 0)
            {
                Debug.Log("[CardPlayManager] No playable cards found");
                yield break;
            }

            // Add delay before evaluating board state
            yield return new WaitForSeconds(_actionDelay);

            var boardState = GetCurrentBoardState();
            var cardPlayOrder = _cardEvaluator.DetermineCardPlayOrder(playableCards, boardState);

            yield return _cardPlayExecutor.PlayCardsInOrder(cardPlayOrder, enemyDeck, boardState);

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

        private BoardState GetCurrentBoardState()
        {
            // Try to get the board state from the board state manager first
            var boardState = _boardStateManager?.EvaluateBoardState();

            // If that fails, create a minimal but properly initialized board state
            if (boardState == null)
            {
                Debug.LogWarning("[CardPlayManager] Using fallback BoardState creation");
                boardState = new BoardState
                {
                    EnemyMana = _combatManager.EnemyMana,
                    TurnCount = _combatManager.TurnCount,
                    EnemyHealth = _combatManager.EnemyHealth,
                    PlayerHealth = _combatManager.PlayerHealth,
                    EnemyMaxHealth = _combatManager.MaxHealth,
                    PlayerMaxHealth = _combatManager.MaxHealth,
                    EnemyMonsters = new List<EntityManager>(),
                    PlayerMonsters = new List<EntityManager>(),
                    CardAdvantage = 0,
                    EnemyBoardControl = 0,
                    PlayerBoardControl = 0
                };

                // Try to get entity collections if _spritePositioning is available
                if (_spritePositioning != null)
                {
                    // Build minimal entity lists (without full caching logic)
                    boardState.EnemyMonsters = _spritePositioning.EnemyEntities
                        .Where(e => e != null)
                        .Select(e => e.GetComponent<EntityManager>())
                        .Where(e => e != null && e.placed && !e.dead && !e.IsFadingOut)
                        .ToList();

                    boardState.PlayerMonsters = _spritePositioning.PlayerEntities
                        .Where(e => e != null)
                        .Select(e => e.GetComponent<EntityManager>())
                        .Where(e => e != null && e.placed && !e.dead && !e.IsFadingOut)
                        .ToList();
                }
            }

            return boardState;
        }
    }
}
