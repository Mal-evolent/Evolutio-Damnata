using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class CardSelectionHandler : MonoBehaviour, ICardSelectionHandler
{
    private ICardManager _cardManager;
    private ICombatManager _combatManager;
    private ICardOutlineManager _cardOutlineManager;
    private ISpritePositioning _spritePositioning;
    private ICombatStage _combatStage;
    private ICardSpawner _playerCardSpawner;
    private IManaChecker _manaChecker;
    private ISpellEffectApplier _spellEffectApplier;

    // Add the entity manager cache
    private Dictionary<GameObject, EntityManager> _entityManagerCache;

    private PlayerCardSelectionHandler _playerCardSelectionHandler;
    private EnemyCardSelectionHandler _enemyCardSelectionHandler;

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

        // Initialize the entity manager cache
        _entityManagerCache = new Dictionary<GameObject, EntityManager>();
        BuildEntityCache();

        InitializeHandlers();
    }

    private void BuildEntityCache()
    {
        _entityManagerCache.Clear();

        // Check if _spritePositioning or its properties are null
        if (_spritePositioning == null || _spritePositioning.PlayerEntities == null || _spritePositioning.EnemyEntities == null)
        {
            Debug.LogWarning("SpritePositioning or its entities are null during BuildEntityCache - will retry later");
            StartCoroutine(RetryBuildEntityCache());
            return;
        }

        // Cache all player entities
        foreach (var entity in _spritePositioning.PlayerEntities)
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

        // Cache all enemy entities
        foreach (var entity in _spritePositioning.EnemyEntities)
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

        Debug.Log("Entity cache successfully built with " + _entityManagerCache.Count + " entities");
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

        if (_spritePositioning != null &&
            _spritePositioning.PlayerEntities != null &&
            _spritePositioning.EnemyEntities != null)
        {
            BuildEntityCache();
        }
        else
        {
            Debug.LogError("Failed to build entity cache after multiple attempts - entities may be missing");
        }
    }


    private void InitializeHandlers()
    {
        var cardValidator = new CardValidator();
        var cardRemover = new CardRemover(_cardManager);

        _playerCardSelectionHandler = new PlayerCardSelectionHandler(
            _cardManager,
            _combatManager,
            cardValidator,
            cardRemover,
            _cardOutlineManager,
            _playerCardSpawner,
            _spellEffectApplier
        );

        _enemyCardSelectionHandler = new EnemyCardSelectionHandler(
            _cardManager,
            _combatManager,
            _cardOutlineManager,
            _spritePositioning,
            _combatStage,
            _manaChecker,
            _spellEffectApplier,
            _entityManagerCache // Pass the entity manager cache
        );
    }

    public void ResetAllMonsterTints()
    {
        foreach (var entity in _spritePositioning.PlayerEntities)
        {
            if (entity != null)
            {
                var image = entity.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Color.white;
                }
            }
        }
        foreach (var entity in _spritePositioning.EnemyEntities)
        {
            if (entity != null)
            {
                var image = entity.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Color.white;
                }
            }
        }
    }

    public void OnPlayerButtonClick(int index)
    {
        if (!ValidateSelection(index, _spritePositioning.PlayerEntities, out EntityManager entityManager))
            return;

        // If we have a card selected and it's our turn, try to play it
        if (_cardManager.HandCardObjects.Contains(_cardManager.CurrentSelectedCard) && _combatManager.IsPlayerPrepPhase())
        {
            var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
            var cardData = cardUI?.Card?.CardType;

            // Only check for occupied space if it's a monster card
            if (cardData != null && cardData.IsMonsterCard && entityManager != null && entityManager.placed)
            {
                Debug.Log("Cannot place a monster on an already occupied space!");
                return;
            }

            _playerCardSelectionHandler.HandlePlayerCardSelection(index, entityManager);
            return;
        }

        // If it's a placed monster we're selecting
        if (entityManager != null && entityManager.placed)
        {
            // If clicking the same monster, toggle its selection off
            if (_cardManager.CurrentSelectedCard == _spritePositioning.PlayerEntities[index])
            {
                _cardManager.CurrentSelectedCard = null;
                ResetAllMonsterTints();
                return;
            }

            // If we have any card selected, deselect it first
            if (_cardManager.CurrentSelectedCard != null)
            {
                _cardOutlineManager.RemoveHighlight();
            }

            // Update monster selection
            _cardManager.CurrentSelectedCard = _spritePositioning.PlayerEntities[index];
            return;
        }

        Debug.Log("No card selected or not the players turn!");
        _cardManager.CurrentSelectedCard = null;
        ResetAllMonsterTints();
    }

    /// <summary>
    /// Checks if there are any active entities on the specified side
    /// </summary>
    /// <param name="isPlayerSide">True to check player side, false to check enemy side</param>
    /// <returns>True if entities are present on the field</returns>
    private bool HasEntitiesOnField(bool isPlayerSide)
    {
        var entities = isPlayerSide ? _spritePositioning.PlayerEntities : _spritePositioning.EnemyEntities;
        
        foreach (var entity in entities)
        {
            if (entity != null && _entityManagerCache.TryGetValue(entity, out var entityManager) &&
                entityManager.placed && !entityManager.dead && !entityManager.IsFadingOut)
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Handle a player's attempt to attack the enemy health icon
    /// Ensures the attack only occurs if valid (no enemy entities on field)
    /// </summary>
    private void TryAttackEnemyHealthIcon()
    {
        // Get the card to check if it's a spell or monster
        var cardUI = _cardManager.CurrentSelectedCard?.GetComponent<CardUI>();
        bool isSpellCard = cardUI?.Card?.CardType?.IsSpellCard == true;

        // If it's a spell card, check if it's any player phase (prep or combat)
        if (isSpellCard)
        {
            if (_combatManager.IsPlayerPrepPhase() || _combatManager.IsPlayerCombatPhase())
            {
                // Get the enemy health icon
                var spellTargetHealthIcon = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
                if (spellTargetHealthIcon == null)
                {
                    Debug.LogError("Could not find enemy health icon to target!");
                    return;
                }

                // Spell targeting on health icon
                _enemyCardSelectionHandler.HandleEnemyCardSelection(-1, spellTargetHealthIcon);
                return;
            }
            else
            {
                Debug.Log("Spell cards can only be played during your turn!");
                return;
            }
        }
        
        // For monster attacks, must be in combat phase
        if (!_combatManager.IsPlayerCombatPhase())
        {
            Debug.Log("Attacks are not allowed at this stage!");
            return;
        }

        // Check if enemy entities are present on the field
        bool enemyEntitiesPresent = HasEntitiesOnField(false);

        // Prevent attacking health icon if enemy entities are present
        if (enemyEntitiesPresent)
        {
            Debug.Log("Cannot attack enemy health directly while enemy monsters are on the field!");
            return;
        }

        var attacker = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
        if (attacker == null || !attacker.placed)
        {
            Debug.Log("Selected monster is not valid for attacking!");
            return;
        }

        // Get the enemy health icon
        var attackTargetHealthIcon = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
        if (attackTargetHealthIcon == null)
        {
            Debug.LogError("Could not find enemy health icon to attack!");
            return;
        }

        // Process the attack and reset selection state
        _combatStage.HandleMonsterAttack(attacker, attackTargetHealthIcon);
        _cardManager.CurrentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();
        ResetAllMonsterTints();
    }

    public void OnEnemyButtonClick(int index)
    {
        // Handle health icon targeting (index -1 represents health icon click)
        if (index == -1)
        {
            TryAttackEnemyHealthIcon();
            return;
        }

        // Process entity targeting
        if (!ValidateSelection(index, _spritePositioning.EnemyEntities, out EntityManager targetEntityManager))
            return;

        if (_cardManager.CurrentSelectedCard != null)
        {
            var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
            bool isSpellCard = cardUI?.Card?.CardType?.IsSpellCard == true;

            // Process spell targeting - spells can be played in any player phase
            if (isSpellCard)
            {
                // Check if it's player's turn (either prep or combat phase)
                if (!_combatManager.IsPlayerPrepPhase() && !_combatManager.IsPlayerCombatPhase())
                {
                    Debug.Log("Spell cards can only be played during your turn!");
                    return;
                }
                _enemyCardSelectionHandler.HandleEnemyCardSelection(index, targetEntityManager);
                return;
            }

            // Process attack targeting - restricted to combat phase
            if (_combatManager.IsPlayerCombatPhase())
            {
                var attacker = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
                if (attacker != null && attacker.placed)
                {
                    HandlePossibleAttack(targetEntityManager);
                }
                else
                {
                    Debug.Log("Selected monster is not valid for attacking!");
                    _cardManager.CurrentSelectedCard = null;
                    ResetAllMonsterTints();
                }
            }
        }
        else
        {
            Debug.Log("No card selected!");
            _cardOutlineManager.RemoveHighlight();
            ResetAllMonsterTints();
        }
    }

    /// <summary>
    /// Processes an attack against a health icon if valid combat conditions are met.
    /// </summary>
    /// <param name="healthIcon">The health icon to attack</param>
    private void HandleHealthIconAttack(HealthIconManager healthIcon)
    {
        if (_cardManager.CurrentSelectedCard == null)
        {
            Debug.Log("No card selected!");
            return;
        }

        if (!_combatManager.IsPlayerCombatPhase())
        {
            Debug.Log("Attacks are not allowed at this stage!");
            return;
        }

        // Check if enemy entities are present on the field
        bool enemyEntitiesPresent = HasEntitiesOnField(false);

        // Prevent attacking health icon if enemy entities are present
        if (enemyEntitiesPresent)
        {
            Debug.Log("Cannot attack enemy health directly while enemy monsters are on the field!");
            return;
        }

        var attacker = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
        if (attacker == null || !attacker.placed)
        {
            Debug.Log("Selected monster is not valid for attacking!");
            return;
        }

        // Process the attack and reset selection state
        _combatStage.HandleMonsterAttack(attacker, healthIcon);
        _cardManager.CurrentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();
        ResetAllMonsterTints();
    }

    /// <summary>
    /// Validates entity selection at the specified index.
    /// </summary>
    /// <param name="index">Index of the entity to validate</param>
    /// <param name="entities">List of entities to check against</param>
    /// <param name="entityManager">Output parameter for the validated EntityManager</param>
    /// <returns>True if selection is valid, false otherwise</returns>
    private bool ValidateSelection(int index, System.Collections.Generic.List<GameObject> entities, out EntityManager entityManager)
    {
        entityManager = null;

        if (index < 0 || index >= entities.Count)
        {
            Debug.LogError($"Invalid entity index: {index}");
            return false;
        }

        var entity = entities[index];
        if (entity == null)
        {
            Debug.LogError($"Entity at index {index} is null");
            return false;
        }

        // Try to get from cache first
        if (!_entityManagerCache.TryGetValue(entity, out entityManager))
        {
            // Fall back to GetComponent if not in cache
            entityManager = entity.GetComponent<EntityManager>();
            
            // Update cache if found
            if (entityManager != null)
            {
                _entityManagerCache[entity] = entityManager;
            }
        }

        if (entityManager == null)
        {
            Debug.LogError($"No EntityManager found at index {index}");
            return false;
        }

        return true;
    }

    private void HandlePossibleAttack(EntityManager targetEntity)
    {
        if (_cardManager.CurrentSelectedCard == null) return;

        EntityManager playerEntity = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
        if (playerEntity == null || !playerEntity.placed) return;

        // Check for dead or fading entities
        if (playerEntity.dead || playerEntity.IsFadingOut)
        {
            Debug.Log($"Cannot attack with {playerEntity.name}: entity is dead or fading out");
            return;
        }
        
        if (targetEntity.dead || targetEntity.IsFadingOut)
        {
            Debug.Log($"Cannot attack {targetEntity.name}: entity is dead or fading out");
            return;
        }

        if (_combatManager.IsPlayerCombatPhase())
        {
            // Check if there are any taunt units on the enemy side
            if (CombatRulesEngine.HasTauntUnits(_spritePositioning.EnemyEntities))
            {
                var tauntUnits = CombatRulesEngine.GetAllTauntUnits(_spritePositioning.EnemyEntities);
                if (tauntUnits.Count > 0 && !tauntUnits.Contains(targetEntity))
                {
                    Debug.Log($"Cannot attack {targetEntity.name} while there are taunt units on the field!");
                    return;
                }
            }

            _combatStage.HandleMonsterAttack(playerEntity, targetEntity);
            // Deselect everything after attack
            _cardManager.CurrentSelectedCard = null;
            _cardOutlineManager.RemoveHighlight();
            ResetAllMonsterTints();
        }
        else
        {
            Debug.Log("Attacks are not allowed at this stage!");
        }
    }
    // Add this method to CardSelectionHandler.cs
    public void OnPlayerHealthIconClick()
    {
        // Get the card to check if it's a spell
        var cardUI = _cardManager.CurrentSelectedCard?.GetComponent<CardUI>();
        bool isSpellCard = cardUI?.Card?.CardType?.IsSpellCard == true;

        if (!isSpellCard || _cardManager.CurrentSelectedCard == null)
        {
            Debug.Log("Only spell cards can target your own health icon!");
            return;
        }

        // Check if it's the player's turn (either prep or combat phase)
        if (!_combatManager.IsPlayerPrepPhase() && !_combatManager.IsPlayerCombatPhase())
        {
            Debug.Log("Spell cards can only be played during your turn!");
            return;
        }

        // Get the player health icon
        var playerHealthIcon = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthIconManager>();
        if (playerHealthIcon == null)
        {
            Debug.LogError("Could not find player health icon to target!");
            return;
        }

        // Apply spell to player's health icon
        ApplySpellToPlayerHealthIcon(playerHealthIcon);
    }

    private void ApplySpellToPlayerHealthIcon(HealthIconManager playerHealthIcon)
    {
        if (_cardManager.CurrentSelectedCard == null)
        {
            Debug.Log("No spell card selected!");
            return;
        }

        var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
        if (cardUI == null || cardUI.Card == null || cardUI.Card.CardType == null)
        {
            Debug.LogError("Invalid card data!");
            return;
        }

        CardData spellData = cardUI.Card.CardType;

        // Check if player has enough mana - FIXED: pass CardData instead of int
        if (!_manaChecker.HasEnoughPlayerMana(spellData))
        {
            Debug.Log("Not enough mana to cast this spell!");
            return;
        }

        // Apply spell effect to player health icon
        _spellEffectApplier.ApplySpellEffects(playerHealthIcon, spellData, -1);

        // Remove the card from hand
        DestroyCard(_cardManager.CurrentSelectedCard);
        _cardManager.CurrentSelectedCard = null;
        _cardOutlineManager.RemoveHighlight();

        // Spend mana - FIXED: pass CardData instead of int
        _manaChecker.DeductPlayerMana(spellData);
    }

    private void DestroyCard(GameObject card)
    {
        if (card == null) return;

        // Remove from hand card objects list
        if (_cardManager.HandCardObjects.Contains(card))
        {
            _cardManager.HandCardObjects.Remove(card);
        }

        // Destroy the card object
        Destroy(card);
    }
}

