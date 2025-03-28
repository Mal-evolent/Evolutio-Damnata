using UnityEngine;

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

    public void OnPlayerButtonClick(int index)
    {
        if (!ValidateSelection(index, _spritePositioning.PlayerEntities, out EntityManager entityManager))
            return;

        if (_cardManager.CurrentSelectedCard != null && _combatManager.PlayerTurn)
        {
            _playerCardSelectionHandler.HandlePlayerCardSelection(index, entityManager);
        }
        else if (_cardManager.CurrentSelectedCard == null)
        {
            if (entityManager != null && entityManager.placed)
            {
                _cardManager.CurrentSelectedCard = _spritePositioning.PlayerEntities[index];
            }
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            _cardOutlineManager.RemoveHighlight();
        }
    }

    public void OnEnemyButtonClick(int index)
    {
        if (!ValidateSelection(index, _spritePositioning.EnemyEntities, out EntityManager entityManager))
            return;

        if (_cardManager.CurrentSelectedCard != null && _combatManager.PlayerTurn)
        {
            _enemyCardSelectionHandler.HandleEnemyCardSelection(index, entityManager);
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            _cardOutlineManager.RemoveHighlight();
            return;
        }

        HandlePossibleAttack(entityManager);
    }

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

    private void HandlePossibleAttack(EntityManager enemyEntity)
    {
        if (_cardManager.CurrentSelectedCard == null) return;

        EntityManager playerEntity = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
        if (playerEntity == null || !playerEntity.placed) return;

        if (_combatManager.IsPlayerCombatPhase())
        {
            _combatStage.HandleMonsterAttack(playerEntity, enemyEntity);
            _cardManager.CurrentSelectedCard = null;
        }
        else
        {
            Debug.Log("Attacks are not allowed at this stage!");
        }
    }
}