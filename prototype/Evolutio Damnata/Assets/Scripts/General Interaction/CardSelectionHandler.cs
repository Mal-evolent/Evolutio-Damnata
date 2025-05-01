using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class CardSelectionHandler : MonoBehaviour, ICardSelectionHandler
{
    #region Dependencies
    private ICardManager _cardManager;
    private ICombatManager _combatManager;
    private ICardOutlineManager _cardOutlineManager;
    private ISpritePositioning _spritePositioning;
    private ICombatStage _combatStage;
    private ICardSpawner _playerCardSpawner;
    private IManaChecker _manaChecker;
    private ISpellEffectApplier _spellEffectApplier;
    private Dictionary<GameObject, EntityManager> _entityManagerCache;
    private PlayerCardSelectionHandler _playerCardSelectionHandler;
    private EnemyCardSelectionHandler _enemyCardSelectionHandler;
    #endregion

    #region Initialization
    public void Initialize(
        ICardManager cardManager,
        ICombatManager combatManager,
        ICardOutlineManager cardOutlineManager,
        ISpritePositioning spritePositioning,
        ICombatStage combatStage,
        ICardSpawner playerCardSpawner,
        IManaChecker manaChecker,
        ISpellEffectApplier spellEffectApplier)
    {
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
        _combatManager = combatManager ?? throw new System.ArgumentNullException(nameof(combatManager));
        _cardOutlineManager = cardOutlineManager ?? throw new System.ArgumentNullException(nameof(cardOutlineManager));
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
        _combatStage = combatStage ?? throw new System.ArgumentNullException(nameof(combatStage));
        _playerCardSpawner = playerCardSpawner ?? throw new System.ArgumentNullException(nameof(playerCardSpawner));
        _manaChecker = manaChecker ?? throw new System.ArgumentNullException(nameof(manaChecker));
        _spellEffectApplier = spellEffectApplier ?? throw new System.ArgumentNullException(nameof(spellEffectApplier));

        _entityManagerCache = new Dictionary<GameObject, EntityManager>();
        BuildEntityCache();
        InitializeHandlers();
    }

    private void InitializeHandlers()
    {
        var cardValidator = new CardValidator();
        var cardRemover = new CardRemover(_cardManager);

        _playerCardSelectionHandler = new PlayerCardSelectionHandler(
            _cardManager, _combatManager, cardValidator, cardRemover,
            _cardOutlineManager, _playerCardSpawner, _spellEffectApplier, _spritePositioning);

        _enemyCardSelectionHandler = new EnemyCardSelectionHandler(
            _cardManager, _combatManager, _cardOutlineManager, _spritePositioning,
            _combatStage, _manaChecker, _spellEffectApplier, _entityManagerCache);
    }
    #endregion

    #region Entity Cache Management
    private void BuildEntityCache()
    {
        _entityManagerCache.Clear();

        if (_spritePositioning == null || _spritePositioning.PlayerEntities == null || _spritePositioning.EnemyEntities == null)
        {
            Debug.LogWarning("SpritePositioning or its entities are null during BuildEntityCache - will retry later");
            StartCoroutine(RetryBuildEntityCache());
            return;
        }

        CacheEntities(_spritePositioning.PlayerEntities);
        CacheEntities(_spritePositioning.EnemyEntities);

        Debug.Log($"Entity cache successfully built with {_entityManagerCache.Count} entities");
    }

    private void CacheEntities(List<GameObject> entities)
    {
        foreach (var entity in entities)
        {
            if (entity != null && !_entityManagerCache.ContainsKey(entity))
            {
                var entityManager = entity.GetComponent<EntityManager>();
                if (entityManager != null)
                {
                    _entityManagerCache[entity] = entityManager;
                }
            }
        }
    }

    private IEnumerator RetryBuildEntityCache()
    {
        int attempts = 0;
        const int maxAttempts = 10;
        const float retryDelay = 0.2f;

        while (attempts < maxAttempts &&
               (_spritePositioning == null ||
                _spritePositioning.PlayerEntities == null ||
                _spritePositioning.EnemyEntities == null))
        {
            yield return new WaitForSeconds(retryDelay);
            attempts++;
            Debug.Log($"Retry {attempts}/{maxAttempts} building entity cache...");
        }

        if (_spritePositioning?.PlayerEntities != null && _spritePositioning?.EnemyEntities != null)
        {
            BuildEntityCache();
        }
        else
        {
            Debug.LogError("Failed to build entity cache after multiple attempts - entities may be missing");
        }
    }
    #endregion

    #region State Management
    public void ResetAllMonsterTints()
    {
        SetEntityTints(_spritePositioning.PlayerEntities, Color.white);
        SetEntityTints(_spritePositioning.EnemyEntities, Color.white);
    }

    private void SetEntityTints(List<GameObject> entities, Color color)
    {
        foreach (var entity in entities)
        {
            if (entity != null)
            {
                var image = entity.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }
        }
    }

    private void ResetSelection()
    {
        ResetAllMonsterTints();
        _cardManager.CurrentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();
    }
    #endregion

    #region Player Interaction
    public void OnPlayerButtonClick(int index)
    {
        if (!ValidateSelection(index, _spritePositioning.PlayerEntities, out EntityManager entityManager))
            return;

        GameObject selectedEntity = _spritePositioning.PlayerEntities[index];
        bool isCardFromHand = _cardManager.HandCardObjects.Contains(_cardManager.CurrentSelectedCard);

        if (!isCardFromHand)
        {
            HandlePlayerEntitySelection(selectedEntity, entityManager);
            return;
        }

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        var cardData = cardUI?.Card?.CardType;
        if (cardData == null) return;

        if (cardData.IsSpellCard && entityManager != null && entityManager.placed)
        {
            HandlePlayerSpellTargeting(index, entityManager);
            return;
        }

        HandlePlayerCardPlay(cardData, index, entityManager);
    }

    private void HandlePlayerEntitySelection(GameObject selectedEntity, EntityManager entityManager)
    {
        if (entityManager != null && entityManager.placed)
        {
            // Toggle selection if clicking the same monster
            if (_cardManager.CurrentSelectedCard == selectedEntity)
            {
                ResetSelection();
                return;
            }

            // Update selection
            if (_cardManager.CurrentSelectedCard != null)
                _cardOutlineManager.RemoveHighlight();

            _cardManager.CurrentSelectedCard = selectedEntity;
            return;
        }

        Debug.Log("No card selected or not the player's turn!");
        ResetSelection();
    }

    private void HandlePlayerSpellTargeting(int index, EntityManager entityManager)
    {
        if (_combatManager.IsPlayerPrepPhase() || _combatManager.IsPlayerCombatPhase())
        {
            _playerCardSelectionHandler.HandlePlayerCardSelection(index, entityManager);
        }
        else
        {
            Debug.Log("Spell cards can only be played during your turn!");
            ResetSelection();
        }
    }

    private void HandlePlayerCardPlay(CardData cardData, int index, EntityManager entityManager)
    {
        bool canPlayInCurrentPhase = CanPlayInCurrentPhase(cardData);

        if (canPlayInCurrentPhase)
        {
            if (cardData.IsMonsterCard && entityManager != null && entityManager.placed)
            {
                Debug.Log("Cannot place a monster on an already occupied space!");
                ResetSelection();
                return;
            }

            _playerCardSelectionHandler.HandlePlayerCardSelection(index, entityManager);
        }
        else
        {
            string message = cardData.IsMonsterCard
                ? "Monster cards can only be played during the preparation phase!"
                : "Cannot play this card in the current phase!";

            Debug.Log(message);
            ResetSelection();
        }
    }

    public void OnPlayerHealthIconClick()
    {
        var cardUI = _cardManager.CurrentSelectedCard?.GetComponent<CardUI>();
        bool isSpellCard = cardUI?.Card?.CardType?.IsSpellCard == true;

        if (!isSpellCard || _cardManager.CurrentSelectedCard == null)
        {
            Debug.Log("Only spell cards can target your own health icon!");
            ResetSelection();
            return;
        }

        if (!_combatManager.IsPlayerPrepPhase() && !_combatManager.IsPlayerCombatPhase())
        {
            Debug.Log("Spell cards can only be played during your turn!");
            ResetSelection();
            return;
        }

        var playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
        if (playerHealthIcon == null)
        {
            Debug.LogError("Could not find player health icon to target!");
            ResetSelection();
            return;
        }

        ApplySpellToPlayerHealthIcon(playerHealthIcon);
    }

    private void ApplySpellToPlayerHealthIcon(HealthIconManager playerHealthIcon)
    {
        if (_cardManager.CurrentSelectedCard == null)
        {
            Debug.Log("No spell card selected!");
            ResetSelection();
            return;
        }

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        if (cardUI?.Card?.CardType == null)
        {
            Debug.LogError("Invalid card data!");
            ResetSelection();
            return;
        }

        CardData spellData = cardUI.Card.CardType;

        if (!_manaChecker.HasEnoughPlayerMana(spellData))
        {
            Debug.Log("Not enough mana to cast this spell!");
            ResetSelection();
            return;
        }

        _spellEffectApplier.ApplySpellEffects(playerHealthIcon, spellData, -1);
        DestroyCard(_cardManager.CurrentSelectedCard);
        _manaChecker.DeductPlayerMana(spellData);
        ResetSelection();
    }
    #endregion

    #region Enemy Interaction
    public void OnEnemyButtonClick(int index)
    {
        if (index == -1)
        {
            TryAttackEnemyHealthIcon();
            return;
        }

        if (!ValidateSelection(index, _spritePositioning.EnemyEntities, out EntityManager targetEntityManager))
            return;

        if (_cardManager.CurrentSelectedCard == null)
        {
            Debug.Log("No card selected!");
            ResetSelection();
            return;
        }

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        bool isSpellCard = cardUI?.Card?.CardType?.IsSpellCard == true;

        if (isSpellCard)
        {
            HandleEnemySpellTargeting(index, targetEntityManager);
        }
        else if (_combatManager.IsPlayerCombatPhase())
        {
            var attacker = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
            if (attacker != null && attacker.placed)
            {
                HandlePossibleAttack(targetEntityManager);
            }
            else
            {
                Debug.Log("Selected monster is not valid for attacking!");
                ResetSelection();
            }
        }
        else
        {
            Debug.Log("Not in combat phase or no valid card selected!");
            ResetSelection();
        }
    }

    private void HandleEnemySpellTargeting(int index, EntityManager targetEntityManager)
    {
        if (!_combatManager.IsPlayerPrepPhase() && !_combatManager.IsPlayerCombatPhase())
        {
            Debug.Log("Spell cards can only be played during your turn!");
            ResetSelection();
            return;
        }

        _enemyCardSelectionHandler.HandleEnemyCardSelection(index, targetEntityManager);
    }

    private void TryAttackEnemyHealthIcon()
    {
        var cardUI = _cardManager.CurrentSelectedCard?.GetComponent<CardUI>();
        bool isSpellCard = cardUI?.Card?.CardType?.IsSpellCard == true;

        if (isSpellCard)
        {
            if (_combatManager.IsPlayerPrepPhase() || _combatManager.IsPlayerCombatPhase())
            {
                var spellTargetHealthIcon = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
                if (spellTargetHealthIcon == null)
                {
                    Debug.LogError("Could not find enemy health icon to target!");
                    ResetSelection();
                    return;
                }

                _enemyCardSelectionHandler.HandleEnemyCardSelection(-1, spellTargetHealthIcon);
            }
            else
            {
                Debug.Log("Spell cards can only be played during your turn!");
                ResetSelection();
            }
            return;
        }

        if (!_combatManager.IsPlayerCombatPhase())
        {
            Debug.Log("Attacks are not allowed at this stage!");
            ResetSelection();
            return;
        }

        bool enemyEntitiesPresent = HasEntitiesOnField(false);
        if (enemyEntitiesPresent)
        {
            Debug.Log("Cannot attack enemy health directly while enemy monsters are on the field!");
            ResetSelection();
            return;
        }

        var attacker = _cardManager.CurrentSelectedCard?.GetComponent<EntityManager>();
        if (attacker == null || !attacker.placed)
        {
            Debug.Log("Selected monster is not valid for attacking!");
            ResetSelection();
            return;
        }

        var attackTargetHealthIcon = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
        if (attackTargetHealthIcon == null)
        {
            Debug.LogError("Could not find enemy health icon to attack!");
            ResetSelection();
            return;
        }

        _combatStage.HandleMonsterAttack(attacker, attackTargetHealthIcon);
        ResetSelection();
    }
    #endregion

    #region Combat Logic
    private bool CanPlayInCurrentPhase(CardData cardData)
    {
        if (cardData.IsMonsterCard)
        {
            return _combatManager.IsPlayerPrepPhase();
        }

        if (cardData.IsSpellCard)
        {
            return _combatManager.IsPlayerPrepPhase() || _combatManager.IsPlayerCombatPhase();
        }

        return false;
    }

    private bool HasEntitiesOnField(bool isPlayerSide)
    {
        var entities = isPlayerSide ? _spritePositioning.PlayerEntities : _spritePositioning.EnemyEntities;

        return entities.Any(entity =>
            entity != null &&
            _entityManagerCache.TryGetValue(entity, out var entityManager) &&
            entityManager.placed &&
            !entityManager.dead &&
            !entityManager.IsFadingOut);
    }

    private void HandlePossibleAttack(EntityManager targetEntity)
    {
        // Validate attacker
        if (_cardManager.CurrentSelectedCard == null)
        {
            Debug.LogWarning("Attack failed: No card is currently selected");
            ResetSelection();
            return;
        }

        EntityManager attacker = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
        if (attacker == null || !attacker.placed || attacker.dead || attacker.IsFadingOut)
        {
            Debug.LogWarning("Attack failed: Invalid attacker state");
            ResetSelection();
            return;
        }

        // Validate target
        if (targetEntity == null || targetEntity.dead || targetEntity.IsFadingOut)
        {
            Debug.LogWarning("Attack failed: Invalid target state");
            ResetSelection();
            return;
        }

        // Validate game state
        if (!_combatManager.IsPlayerCombatPhase())
        {
            Debug.LogWarning("Attack failed: Not in player combat phase");
            ResetSelection();
            return;
        }

        // Check attack limits
        AttackLimiter attackLimiter = (_combatStage as CombatStage)?.GetAttackLimiter();
        if (attackLimiter != null && !attackLimiter.CanAttack(attacker))
        {
            Debug.LogWarning($"Attack failed: {attacker.name} has already used its attack this turn");
            ResetSelection();
            return;
        }

        // Check taunt mechanics
        if (CombatRulesEngine.HasTauntUnits(_spritePositioning.EnemyEntities))
        {
            var tauntUnits = CombatRulesEngine.GetAllTauntUnits(_spritePositioning.EnemyEntities);
            if (tauntUnits.Count > 0 && !tauntUnits.Contains(targetEntity))
            {
                string tauntUnitNames = string.Join(", ", tauntUnits.Select(u => u.name));
                Debug.LogWarning($"Attack failed: Must attack taunt units first. Taunt units: {tauntUnitNames}");
                ResetSelection();
                return;
            }
        }

        // Execute attack
        try
        {
            _combatStage.HandleMonsterAttack(attacker, targetEntity);
            Debug.Log($"Attack successful: {attacker.name} attacked {targetEntity.name}");
            ResetSelection();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Attack failed with exception: {ex.Message}\n{ex.StackTrace}");
            ResetSelection();
        }
    }
    #endregion

    #region Utility Methods
    private bool ValidateSelection(int index, List<GameObject> entities, out EntityManager entityManager)
    {
        entityManager = null;

        if (index < 0 || index >= entities.Count)
        {
            Debug.LogError($"Invalid entity index: {index}");
            ResetSelection();
            return false;
        }

        var entity = entities[index];
        if (entity == null)
        {
            Debug.LogError($"Entity at index {index} is null");
            ResetSelection();
            return false;
        }

        if (!_entityManagerCache.TryGetValue(entity, out entityManager))
        {
            entityManager = entity.GetComponent<EntityManager>();
            if (entityManager != null)
            {
                _entityManagerCache[entity] = entityManager;
            }
        }

        if (entityManager == null)
        {
            Debug.LogError($"No EntityManager found at index {index}");
            ResetSelection();
            return false;
        }

        return true;
    }

    private void DestroyCard(GameObject card)
    {
        if (card == null) return;

        if (_cardManager.HandCardObjects.Contains(card))
        {
            _cardManager.HandCardObjects.Remove(card);
        }

        Destroy(card);
    }
    #endregion
}

