using UnityEngine;
using System.Linq;
using UnityEngine.UI;

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

        InitializeHandlers();
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
            _spellEffectApplier
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

    public void OnEnemyButtonClick(int index)
    {
        // Validate if any enemy entities are present on the field
        bool enemyEntitiesPresent = false;
        foreach (var entity in _spritePositioning.EnemyEntities)
        {
            var entityManager = entity?.GetComponent<EntityManager>();
            if (entityManager != null && entityManager.placed)
            {
                enemyEntitiesPresent = true;
                break;
            }
        }

        // Handle health icon targeting (index -1 represents health icon click)
        if (index == -1)
        {
            // Enforce targeting rules - health icon can only be targeted when no enemy entities are present
            if (enemyEntitiesPresent)
            {
                Debug.Log("Cannot target enemy health while enemy entities are on the field!");
                return;
            }

            // Attempt to locate and target the enemy health icon
            var enemyHealthIcon = GameObject.FindGameObjectWithTag("Enemy")?.GetComponent<HealthIconManager>();
            
            if (enemyHealthIcon != null)
            {
                // Process spell card targeting if applicable
                if (_cardManager.CurrentSelectedCard != null)
                {
                    var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
                    if (cardUI?.Card?.CardType?.IsSpellCard == true)
                    {
                        // Check if we're in a player phase before allowing spell card play
                        if (!_combatManager.IsPlayerPrepPhase() && !_combatManager.IsPlayerCombatPhase())
                        {
                            Debug.Log("Spell cards can only be played during your turn!");
                            return;
                        }
                        _enemyCardSelectionHandler.HandleEnemyCardSelection(index, enemyHealthIcon);
                        return;
                    }
                }
                HandleHealthIconAttack(enemyHealthIcon);
                return;
            }
            Debug.Log("Could not find enemy health icon to target");
            return;
        }

        // Process entity targeting
        if (!ValidateSelection(index, _spritePositioning.EnemyEntities, out EntityManager targetEntityManager))
            return;

        if (_cardManager.CurrentSelectedCard != null)
        {
            var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
            bool isSpellCard = cardUI?.Card?.CardType?.IsSpellCard == true;

            // Process spell targeting - spells can only be played in player phases
            if (isSpellCard)
            {
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

        entityManager = entities[index]?.GetComponent<EntityManager>();
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

        if (_combatManager.IsPlayerCombatPhase())
        {
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
}
